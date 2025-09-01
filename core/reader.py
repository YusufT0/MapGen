"""
reader.py

This file contains all of the loading operations.
Configs, maps etc. loading to the system here.
"""

from pathlib import Path
import trimesh
import yaml
from models.domain import MainConfig, MapConfig

UPLOAD_DIR = Path("./uploads")
CONFIG_DIR = Path("./configs")

def _safe_upload_path(filename: str) -> Path:
    return UPLOAD_DIR / Path(filename).name

def _safe_config_path(filename: str) -> Path:
    return CONFIG_DIR / Path(filename).name

# Reads the main config file from configs folder (not uploads)
def load_config(filename: str) -> MainConfig:
    """
    Loading the main configuration file from configs folder.
    """
    yaml_path = _safe_config_path(filename)
    if not yaml_path.exists():
        raise FileNotFoundError(f"YAML file not found: {yaml_path}")

    with open(yaml_path, 'r', encoding="utf-8") as file:
        data = yaml.safe_load(file)

    return MainConfig(**data)

def load_uploaded_config(filename: str) -> MainConfig:
    """
    Load main config file from uploads folder (used right after upload).
    """
    yaml_path = _safe_upload_path(filename)
    if not yaml_path.exists():
        raise FileNotFoundError(f"YAML file not found in uploads: {yaml_path}")

    with open(yaml_path, 'r', encoding="utf-8") as file:
        data = yaml.safe_load(file)

    return MainConfig(**data)

# Reads the map config file from configs folder (not uploads)
def load_map_config(filename: str) -> MapConfig:
    yaml_path = _safe_config_path(filename)
    if not yaml_path.exists():
        raise FileNotFoundError(f"YAML file not found: {yaml_path}")

    with open(yaml_path, 'r', encoding="utf-8") as file:
        data = yaml.safe_load(file)

    return MapConfig(**data)


# Loads the trimesh scene from uploads folder (base map or models)
def load_scene(filename: str) -> trimesh.Scene:
    """
    Loading the scene from an OBJ or mesh file in uploads folder.
    """
    mesh_path = _safe_upload_path(filename)
    if not mesh_path.exists():
        raise FileNotFoundError(f"Mesh file not found: {mesh_path}")

    mesh = trimesh.load(mesh_path)
    if isinstance(mesh, trimesh.Trimesh):
        scene = trimesh.Scene()
        scene.add_geometry(mesh)
        return scene

    if isinstance(mesh, trimesh.Scene):
        return mesh

    # Handle iterable meshes
    scene = trimesh.Scene()
    if hasattr(mesh, '__iter__'):
        for m in mesh:
            scene.add_geometry(m)
    else:
        scene.add_geometry(mesh)

    return scene
