import yaml
import trimesh
import open3d as o3d
import numpy as np
from pathlib import Path
from schema_test import validate_config
import uuid
import os
from shape_adder import create_model

# print(trimesh.__version__)


def load_config(path) -> dict:
    yaml_path = Path(path)
    with open(yaml_path, 'r') as file:
        config = yaml.safe_load(file)
    return config

def load_scene(map_path: Path) -> trimesh.Scene:
    if map_path.exists():
        mesh = trimesh.load(map_path)
        
        if isinstance(mesh, trimesh.Trimesh):
            scene = trimesh.Scene()
            scene.add_geometry(mesh)
            return scene
        elif isinstance(mesh, trimesh.Scene):
            return mesh
        else:
            # Handle other types or multiple meshes
            scene = trimesh.Scene()
            if hasattr(mesh, '__iter__'):
                for m in mesh:
                    scene.add_geometry(m)
            else:
                scene.add_geometry(mesh)
            return scene
    return trimesh.Scene()

# TODO: add custom model importation


def apply_augmentations(scene: trimesh.Scene, augmentations: list):
    map_bounds = scene.bounds
    map_count = config.get('map_count', 1)  # Get map_count from top level
    filetype = config.get('output_type', 'glb') 
    if (not (os.path.exists("output_maps"))):
        os.mkdir('output_maps')
    base_mesh = trimesh.util.concatenate(scene.dump())  # only terrain models here
    for i in range(map_count):
        scene_copy = scene.copy()  # Create a copy for each map
        for aug in augmentations:
            if aug['type'] == 'add_model':
                models = create_model(map_bounds, base_mesh, aug)
                for model in models:
                    scene_copy.add_geometry(model)
            

        
        scene_copy.show()
        base = f"./output_maps/map_{uuid.uuid4()}"
        if (filetype == 'glb'):
            filename = f"{base}.glb"
        elif (filetype == 'obj'):
            filename = f"{base}.obj"
        elif (filetype == 'stl'):
            filename = f"{base}.stl"
        else:
            filename = f"{base}.glb"    
        
        
        export_scene(scene_copy, filename)
        

# TODO: Colored export with .obj
# TODO: Colored export with .stl

def export_scene(scene: trimesh.Scene, output_path: str):
    scene.export(output_path)
                                                                            
if __name__ == "__main__":
    path = "./config.yaml"
    if not (validate_config(path)):
        exit(1)
    config = load_config(path)
    scene = load_scene(Path(config['map']))
    apply_augmentations(scene, config.get('augmentations', []))
    
    # export_scene(scene, "updated_map.obj")