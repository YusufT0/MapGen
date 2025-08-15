import pytest
import yaml
import trimesh
import core.reader as reader
from models.domain import MainConfig, AddModelConfig, LandscapeConfig, MapConfig

def test_load_config(tmp_path):
    dummy_config = MainConfig(
        map_count=2,
        output_type="glb",
        augmentations=[
            AddModelConfig(
                type="add_model",
                model="custom",
                custom_path="./rock.glb",
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

    yaml_file = tmp_path / "config.yaml"
    yaml_file.write_text(
        yaml.dump(dummy_config.model_dump()),
        encoding="utf-8"
    )

    result = reader.load_config(str(yaml_file))

    assert isinstance(result, MainConfig)
    assert result.map_count == 2
    assert result.output_type == "glb"

    assert isinstance(result.augmentations[0], AddModelConfig)
    assert isinstance(result.augmentations[1], LandscapeConfig)
    assert result.augmentations[0].model == "custom"
    assert result.augmentations[1].smoothness == "high"
    


def test_load_config_file_not_found():
    with pytest.raises(FileNotFoundError):
        reader.load_config("nonexistent.yaml")


def test_load_map_config(tmp_path):
    dummy_config = MapConfig(
        map="test_map",
        size=50,
        objects=[],
        landscapes=[]
    )

    yaml_file = tmp_path / "map.yaml"
    yaml_file.write_text(
        yaml.dump(dummy_config.model_dump()),
        encoding="utf-8"
    )

    result = reader.load_map_config(str(yaml_file))
    assert isinstance(result, MapConfig)
    assert result.map == "test_map"
    assert result.objects == []      
    assert result.landscapes == []   


def test_load_scene_with_trimesh_object(tmp_path):
    mesh = trimesh.creation.box()
    file_path = tmp_path / "cube.obj"
    mesh.export(file_path)

    scene = reader.load_scene(str(file_path))
    assert isinstance(scene, trimesh.Scene)
    assert len(scene.geometry) > 0


def test_load_scene_with_scene_object(tmp_path):
    mesh1 = trimesh.creation.box()
    mesh2 = trimesh.creation.icosphere()
    scene_obj = trimesh.Scene([mesh1, mesh2])

    file_path = tmp_path / "scene.glb"
    scene_obj.export(file_path)

    scene = reader.load_scene(str(file_path))
    assert isinstance(scene, trimesh.Scene)
    assert len(scene.geometry) == 2


def test_load_scene_file_not_found(tmp_path):
    # Olmayan dosya → boş Scene dönmeli
    file_path = tmp_path / "nofile.obj"
    scene = reader.load_scene(str(file_path))
    assert isinstance(scene, trimesh.Scene)
    assert len(scene.geometry) == 0
