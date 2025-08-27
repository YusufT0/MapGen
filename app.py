from fastapi import FastAPI , WebSocket , HTTPException
from core.augment_tool import augment
from core.reader import load_config, load_scene
from core.augment_writer import run_scene_builder
from models.api import ConfigInput, CreatorInput
from models.domain import MapConfig
from core.writer import write_config
from validation.schema_test import validate_config 
import trimesh
import os
import uuid
import asyncio

app = FastAPI()

progress_store = {}  # task_id: progress

folder_path = "./maps"
os.makedirs(folder_path, exist_ok=True)
folder_path = "./configs"
os.makedirs(folder_path, exist_ok=True)

def generate_map_name():
    return f"map_{uuid.uuid4().hex[:8]}"


def build_map_config(map_name: str, filetype: str, config, map_bounds, base_mesh) -> MapConfig:
    objects = []
    landscapes = []
    
    for aug in config.augmentations:
        augment_results = augment(map_bounds, aug, base_mesh)
        
        if aug.type == "landscape":
            landscapes.extend(augment_results)
        else:  # aug.type == "add_model"
            objects.extend(augment_results)
    
    return MapConfig(
        map=f"{map_name}.{filetype}",
        objects=objects,
        landscapes=landscapes
    )

def generate_and_write_maps(config, scene, base_mesh, map_bounds, writer=write_config):
    for _ in range(config.map_count):
        map_name = generate_map_name()
        map_config = build_map_config(map_name, config.output_type, config, map_bounds, base_mesh)
        writer(map_name, map_config)
    return map_config
 
@app.post("/create_configs")
async def create_configs(data: ConfigInput):
    if not validate_config("config.yaml"):
        return {"data": "Please provide config file in correct format."}

    scene = load_scene(data.obj_path)
    config = load_config(data.config_path)
    base_mesh = trimesh.util.concatenate(scene.dump())
    map_bounds = scene.bounds

    last_map_config = generate_and_write_maps(config, scene, base_mesh, map_bounds)

    return {
    "status": "success",
    "message": f"Generated {config.map_count} map configurations successfully.",
    "last_map": last_map_config.map
    }

@app.post("/create_maps")
async def create_maps(data: CreatorInput):
    run_scene_builder(
        base_map=data.base_map or "./map.obj"
        )
    return {"status": "success", "message": "Maps created successfully."}


@app.get("/get_map_path")
async def get_map_path():
    return {"path": os.path.abspath("./maps")}
@app.get("/get_config_path")
async def get_config_path():
    return {"path": os.path.abspath("./configs")}

@app.get("/start_progress/{task_id}")
async def start_progress(task_id: str):
    if task_id in progress_store:
        raise HTTPException(status_code=400, detail="Task already running")
    progress_store[task_id] = 0.0
    asyncio.create_task(simulate_progress(task_id))
    return {"message": "Progress started", "task_id": task_id}

async def simulate_progress(task_id: str):
    try:
        for i in range(20):
            await asyncio.sleep(0.25)
            progress_store[task_id] = (i + 1) / 20.0
        progress_store[task_id] = 1.0
    except Exception:
        progress_store[task_id] = -1.0

@app.websocket("/ws/progress/{task_id}")
async def websocket_progress(websocket: WebSocket, task_id: str):
    await websocket.accept()
    try:
        while True:
            progress = progress_store.get(task_id, 0.0)
            await websocket.send_json({"progress": progress})
            if progress >= 1.0 or progress == -1.0:
                break
            await asyncio.sleep(0.2)
    except Exception as e:
        print("WebSocket Error:", e)
    finally:
        await websocket.close()
        progress_store.pop(task_id, None)