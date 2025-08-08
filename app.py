from fastapi import FastAPI
from core.augment_tool import augment
from core.reader import load_config, load_scene
from core.augment_writer import run_scene_builder
from models.api import ConfigInput
from models.domain import MapConfig
from core.writer import write_config
from validation.schema_test import validate_config 
import trimesh
import os
import uuid

app = FastAPI()


folder_path = "./maps"
os.makedirs(folder_path, exist_ok=True)
folder_path = "./configs"
os.makedirs(folder_path, exist_ok=True)

def generate_map_name():
    return f"map_{uuid.uuid4().hex[:8]}"

def build_map_config(map_name: str, filetype: str, config, map_bounds, base_mesh) -> MapConfig:
    map_config = MapConfig(
        map=f"{map_name}.{filetype}",
        objects=[],
        landscapes=[]
    )
    for aug in config.augmentations:
        augmentations = augment(map_bounds, aug, base_mesh)
        map_config.objects.extend(augmentations)
    return map_config


 
@app.post("/create_configs")
async def create_configs(data: ConfigInput):
    if not validate_config("config.yaml"):
        return {"data" : "Please provide config file in correct format."}
    scene = load_scene(data.obj_path)
    config = load_config(data.config_path)
    base_mesh = trimesh.util.concatenate(scene.dump())
    map_bounds = scene.bounds
    
    for _ in range(config.map_count):
        map_name = generate_map_name()
        map_config = build_map_config(map_name, config.output_type, config, map_bounds, base_mesh)
        write_config(map_name, map_config)
    print(map_config.objects[0].color)
        
    return {"data": "positive"}

@app.post("/create_maps")
async def create_maps():
    run_scene_builder()
@app.get("/get_map_path")
async def get_map_path():
    return {"path": os.path.abspath("./maps")}
@app.get("/get_config_path")
async def get_config_path():
    return {"path": os.path.abspath("./configs")}

