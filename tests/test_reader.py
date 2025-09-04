import sys
import os

# Add the parent directory (MapGen) to PYTHONPATH
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '..')))

import pytest
import yaml
import trimesh
import core.reader as reader
from models.domain import MainConfig, AddModelConfig, LandscapeConfig, MapConfig

@pytest.fixture(autouse=True)
def setup_dirs(tmp_path):
    """
    Fixture to create temporary configs and uploads directories,
    then patch the reader module paths to use these temporary dirs
    so that tests do not interfere with real data folders.
    """
    configs_dir = tmp_path / "configs"
    uploads_dir = tmp_path / "uploads"
    configs_dir.mkdir()
    uploads_dir.mkdir()

    # Patch reader.py paths
    reader.CONFIG_DIR = configs_dir
    reader.UPLOAD_DIR = uploads_dir

    yield
    # Cleanup handled by tmp_path automatically


def test_load_config():
    """
    Test loading a valid main config from the patched configs directory.
    """
    dummy_config = MainConfig(
        map_count=2,
        output_type="glb",
        augmentations=[
            AddModelConfig(
                type="add_model",
                model="custom",
                custom_path="./Lowpoly_tree_sample.obj",
                scale=0.3,
                count=10,
                position="random"
            ),
            LandscapeConfig(
                type="landscape",
                position="random",
                smoothness="high",
                count=3,
                radius="random"
            )
        ]
    )

    yaml_file = reader.CONFIG_DIR / "config.yaml"
    yaml_file.write_text(
        yaml.dump(dummy_config.model_dump()),
        encoding="utf-8"
    )

    # Call with filename only, since reader prepends CONFIG_DIR internally
    result = reader.load_config("config.yaml")

    assert isinstance(result, MainConfig)
    assert result.map_count == 2
    assert result.output_type == "glb"
    assert isinstance(result.augmentations[0], AddModelConfig)
    assert isinstance(result.augmentations[1], LandscapeConfig)
    assert result.augmentations[0].model == "custom"
    assert result.augmentations[1].smoothness == "high"


def test_load_config_file_not_found():
    """
    Ensure FileNotFoundError is raised when config file doesn't exist in configs folder.
    """
    with pytest.raises(FileNotFoundError):
        reader.load_config("nonexistent.yaml")


def test_load_map_config():
    """
    Test loading a valid map config from the patched configs directory.
    """
    dummy_config = MapConfig(
        map="test_map",
        size=50,
        objects=[],
        landscapes=[]
    )

    yaml_file = reader.CONFIG_DIR / "map.yaml"
    yaml_file.write_text(
        yaml.dump(dummy_config.model_dump()),
        encoding="utf-8"
    )

    # Call with filename only, since reader prepends CONFIG_DIR internally
    result = reader.load_map_config("map.yaml")
    assert isinstance(result, MapConfig)
    assert result.map == "test_map"
    assert result.objects == []
    assert result.landscapes == []


def test_load_scene_with_trimesh_object():
    """
    Test loading a trimesh mesh file from the patched uploads directory.
    """
    mesh = trimesh.creation.box()
    file_path = reader.UPLOAD_DIR / "cube.obj"
    mesh.export(file_path)

    # Call with filename only, since reader prepends UPLOAD_DIR internally
    scene = reader.load_scene("cube.obj")

    assert isinstance(scene, trimesh.Scene)
    assert len(scene.geometry) > 0


def test_load_scene_with_scene_object():
    """
    Test loading a trimesh Scene object file from the patched uploads directory.
    """
    mesh1 = trimesh.creation.box()
    mesh2 = trimesh.creation.icosphere()
    scene_obj = trimesh.Scene([mesh1, mesh2])

    file_path = reader.UPLOAD_DIR / "scene.glb"
    scene_obj.export(file_path)

    # Call with filename only, since reader prepends UPLOAD_DIR internally
    scene = reader.load_scene("scene.glb")

    assert isinstance(scene, trimesh.Scene)
    assert len(scene.geometry) == 2


def test_load_scene_file_not_found():
    """
    If the mesh file does not exist in uploads folder,
    FileNotFoundError is expected.
    """
    with pytest.raises(FileNotFoundError):
        reader.load_scene("nofile.obj")
