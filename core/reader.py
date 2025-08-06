from pathlib import Path
from schemas.domain import MainConfig
import trimesh
import yaml


# Reads the config file.
def load_config(path: str) -> MainConfig:
    yaml_path = Path(path)
    if not yaml_path.exists():
        raise FileNotFoundError("YAML file not found.")

    with open(yaml_path, 'r') as file:
        data = yaml.safe_load(file)

    return MainConfig(**data)

# Loads the trimesh scene
def load_scene(map_path: str) -> trimesh.Scene:
    map_path = Path(map_path)
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

