from fastapi import FastAPI, WebSocket, WebSocketDisconnect, HTTPException, UploadFile, File, BackgroundTasks
from fastapi.responses import JSONResponse, FileResponse
from core.augment_tool import augment
from core.reader import load_config, load_scene, load_uploaded_config
from core.augment_writer import run_scene_builder, progress_store, progress_lock
from models.api import ConfigInput, CreatorInput
from models.domain import MapConfig
from core.writer import write_config
from validation.schema_test import validate_config
from typing import List
import trimesh
import os
import uuid
import asyncio

app = FastAPI()

# Define directory paths for maps, configs, and uploads
MAPS_DIR = os.path.abspath("./maps")
CONFIGS_DIR = os.path.abspath("./configs")
UPLOAD_DIR = os.path.abspath("./uploads")

# Ensure the directories exist
os.makedirs(UPLOAD_DIR, exist_ok=True)
os.makedirs(MAPS_DIR, exist_ok=True)
os.makedirs(CONFIGS_DIR, exist_ok=True)


def generate_map_name():
    """Generate a unique map name using UUID."""
    return f"map_{uuid.uuid4().hex[:8]}"


def build_map_config(map_name: str, filetype: str, config, map_bounds, base_mesh) -> MapConfig:
    """
    Build a MapConfig object by applying augmentations to the base mesh.
    Separates landscape augmentations and model additions.
    """
    objects = []
    landscapes = []

    for aug in config.augmentations:
        augment_results = augment(map_bounds, aug, base_mesh)

        if aug.type == "landscape":
            landscapes.extend(augment_results)
        else:  # If augmentation type is add_model
            objects.extend(augment_results)

    return MapConfig(
        map=f"{map_name}.{filetype}",
        objects=objects,
        landscapes=landscapes
    )


def generate_and_write_maps(config, scene, base_mesh, map_bounds, writer=write_config):
    """
    Generate multiple map configurations based on config.map_count,
    write them to disk using the provided writer function,
    and return the last generated MapConfig.
    """
    for _ in range(config.map_count):
        map_name = generate_map_name()
        map_config = build_map_config(map_name, config.output_type, config, map_bounds, base_mesh)
        writer(map_name, map_config)
    return map_config


from os.path import basename


@app.post("/create_configs")
async def create_configs(data: ConfigInput):
    """
    Create map configurations based on uploaded object and config files.
    Loads the scene and config, generates maps, and returns success message.
    """
    obj_filename = basename(data.obj_path)
    config_filename = basename(data.config_path)

    scene = load_scene(obj_filename)
    config = load_uploaded_config(config_filename)

    base_mesh = trimesh.util.concatenate(scene.dump())
    map_bounds = scene.bounds

    last_map_config = generate_and_write_maps(config, scene, base_mesh, map_bounds)

    return {
        "status": "success",
        "message": f"Generated {config.map_count} map configurations successfully.",
        "last_map": last_map_config.map
    }


@app.post("/upload_model_config")
async def upload_model_config(
    obj_file: UploadFile = File(...),
    config_file: UploadFile = File(...),
    mtl_file: UploadFile = File(None),
    texture_files: List[UploadFile] = File([])  # Texture files such as PNG, JPG, TGA, etc.
):
    """
    Endpoint to upload OBJ model, configuration, optional MTL, and textures.
    Saves files to upload directory and validates the config file.
    Deletes all uploaded files if validation fails.
    """
    # Save OBJ file
    obj_path = os.path.join(UPLOAD_DIR, obj_file.filename)
    with open(obj_path, "wb") as f:
        f.write(await obj_file.read())

    # Save config file
    config_path = os.path.join(UPLOAD_DIR, config_file.filename)
    with open(config_path, "wb") as f:
        f.write(await config_file.read())

    # Save MTL file if provided
    if mtl_file:
        mtl_path = os.path.join(UPLOAD_DIR, mtl_file.filename)
        with open(mtl_path, "wb") as f:
            f.write(await mtl_file.read())

    # Save texture files (accept specific image extensions only)
    for texture_file in texture_files:
        if texture_file.filename:  # Ensure file is not empty
            texture_ext = os.path.splitext(texture_file.filename)[1].lower()
            if texture_ext in ['.png', '.jpg', '.jpeg', '.tga', '.bmp', '.tif']:
                texture_path = os.path.join(UPLOAD_DIR, texture_file.filename)
                with open(texture_path, "wb") as f:
                    f.write(await texture_file.read())
                print(f"Texture uploaded: {texture_file.filename}")

    # Validate the config file
    is_valid = validate_config(config_path=config_path)
    if not is_valid:
        # On validation failure, clean up all uploaded files
        try:
            os.remove(obj_path)
            os.remove(config_path)
            if mtl_file:
                os.remove(mtl_path)
            for texture_file in texture_files:
                texture_path = os.path.join(UPLOAD_DIR, texture_file.filename)
                if os.path.exists(texture_path):
                    os.remove(texture_path)
        except Exception as e:
            print(f"Error cleaning up files: {e}")
        
        return {"status": "error", "message": "Config validation failed."}

    return {"status": "success", "message": "Files uploaded and config validated successfully."}


def update_progress(task_id: str, progress: float):
    """
    Thread-safe update of task progress in shared progress_store dictionary.
    """
    with progress_lock:
        progress_store[task_id] = progress


@app.post("/create_maps")
async def create_maps(data: CreatorInput):
    """
    Start the map creation process asynchronously in a background thread.
    Initializes progress for the task and returns a task ID for progress tracking.
    """
    task_id = str(uuid.uuid4())
    with progress_lock:
        progress_store[task_id] = 0.0

    loop = asyncio.get_running_loop()
    loop.run_in_executor(
        None,
        lambda: run_scene_builder(
            task_id=task_id,
            config_folder="./configs",
            base_map=data.base_map or "./map.obj",
            output_folder="./maps",
            progress_callback=lambda p: update_progress(task_id, p)
        )
    )

    return {
        "status": "success",
        "message": "Map creation started.",
        "task_id": task_id
    }


@app.get("/configs/list")
def list_configs():
    """
    List all configuration files in the configs directory,
    excluding any named 'info.txt'.
    """
    files = [
        f
        for f in os.listdir(CONFIGS_DIR)
        if os.path.isfile(os.path.join(CONFIGS_DIR, f)) and f != "info.txt"
    ]
    return JSONResponse(content={"files": files})


@app.get("/configs/file/{filename}")
def get_config_file(filename: str):
    """
    Retrieve a specific config file by filename.
    Returns 404 if file does not exist.
    """
    file_path = os.path.join(CONFIGS_DIR, filename)
    if not os.path.exists(file_path):
        raise HTTPException(status_code=404, detail="File not found")
    return FileResponse(file_path, filename=filename)


@app.get("/maps/list")
def list_maps():
    """
    List all map files in the maps directory,
    excluding any named 'info.txt'.
    """
    files = [
        f
        for f in os.listdir(MAPS_DIR)
        if os.path.isfile(os.path.join(MAPS_DIR, f)) and f != "info.txt"
    ]
    return JSONResponse(content={"files": files})


@app.get("/maps/file/{filename}")
def get_map_file(filename: str):
    """
    Retrieve a specific map file by filename.
    Returns 404 if file does not exist.
    """
    file_path = os.path.join(MAPS_DIR, filename)
    if not os.path.exists(file_path):
        raise HTTPException(status_code=404, detail="File not found")
    return FileResponse(file_path, filename=filename)


@app.get("/start_progress/{task_id}")
async def start_progress(task_id: str, background_tasks: BackgroundTasks):
    """
    Check if a given task_id exists in progress store,
    indicating that the task has started.
    """
    with progress_lock:
        if task_id not in progress_store:
            return {"status": "error", "message": "Task ID not found or not started."}
        else:
            return {"status": "success", "message": "Task already started.", "task_id": task_id}


@app.websocket("/ws/progress/{task_id}")
async def websocket_endpoint(websocket: WebSocket, task_id: str):
    """
    WebSocket endpoint to stream progress updates for a given task_id.
    Sends progress updates every second until progress reaches 100%.
    Cleans up progress store on disconnect or completion.
    """
    await websocket.accept()
    try:
        while True:
            await asyncio.sleep(1)  

            with progress_lock:
                progress = progress_store.get(task_id, 0.0)

            await websocket.send_json({"progress": progress})

            if progress >= 1.0:
                break

    except WebSocketDisconnect:
        print(f"WebSocket disconnected: {task_id}")

    except Exception as e:
        print(f"WebSocket error ({task_id}): {e}")

    finally:
        await websocket.close()
        with progress_lock:
            progress_store.pop(task_id, None)


def clear_directory_but_keep_info_txt(directory_path):
    """
    Utility function to clear all files in a directory except 'info.txt'.
    Used for clearing configs, maps, or uploads directories safely.
    """
    for filename in os.listdir(directory_path):
        file_path = os.path.join(directory_path, filename)
        if os.path.isfile(file_path) and filename != "info.txt":
            try:
                os.remove(file_path)
                print(f"Deleted file: {filename}")
            except Exception as e:
                print(f"Error deleting {filename}: {e}")


@app.delete("/clear_configs")
def clear_configs():
    """
    Endpoint to delete all config files except 'info.txt'.
    """
    clear_directory_but_keep_info_txt(CONFIGS_DIR)
    return {"status": "configs content cleared"}


@app.delete("/clear_maps")
def clear_maps():
    """
    Endpoint to delete all map files except 'info.txt'.
    """
    clear_directory_but_keep_info_txt(MAPS_DIR)
    return {"status": "maps content cleared"}


@app.delete("/clear_uploads")
def clear_uploads():
    """
    Endpoint to delete all uploaded files except 'info.txt'.
    """
    clear_directory_but_keep_info_txt(UPLOAD_DIR)
    return {"status": "uploads content cleared"}
