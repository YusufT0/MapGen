"""
reader.py

This file contains all of the loading operations.
Configs, maps etc. loading to the system here.

"""
from pathlib import Path
import trimesh
import yaml
from models.domain import MainConfig, MapConfig


# Reads the config file.
def load_config(path: str) -> MainConfig:
    """
    Loading the main configuration file for operations.
    """
    yaml_path = Path(path)
    if not yaml_path.exists():
        raise FileNotFoundError("YAML file not found.")

    with open(yaml_path, 'r', encoding="utf-8") as file:
        data = yaml.safe_load(file)

    return MainConfig(**data)


def load_map_config(path: str) -> MapConfig:
    yaml_path = Path(path)
    if not yaml_path.exists():
        raise FileNotFoundError("YAML file not found.")

    with open(yaml_path, 'r', encoding="utf-8") as file:
        data = yaml.safe_load(file)

    return MapConfig(**data)


# Loads the trimesh scene
def load_scene(map_path: str) -> trimesh.Scene:
    """
    Loading the scene that contains the map file.
    """
    map_path = Path(map_path)
    if map_path.exists():
        mesh = trimesh.load(map_path)
        if isinstance(mesh, trimesh.Trimesh):
            scene = trimesh.Scene()
            scene.add_geometry(mesh)
            return scene
        if isinstance(mesh, trimesh.Scene):
            return mesh
        # Handle other types or multiple meshes
        scene = trimesh.Scene()
        if hasattr(mesh, '__iter__'):
            for m in mesh:
                scene.add_geometry(m)
        else:
            scene.add_geometry(mesh)
        return scene
    return trimesh.Scene()
