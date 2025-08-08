import pytest
from unittest.mock import MagicMock, patch
import trimesh
from pathlib import Path
from core.reader import load_scene
from core.augment_writer import (  # change this to your actual module path
    CubeCreator, SphereCreator, CylinderCreator, ConeCreator, CustomModelCreator,
    ShapeFactory, ConfigProcessor,build_scene, export_scene,run_scene_builder, SceneManager
)

class DummyConf:
    def __init__(self, model, color=[1,1,1,255], scale=1.0, position=[0,0,0], model_path=None):
        self.model = model
        self.color = color
        self.scale = scale
        self.position = position
        self.model_path = model_path


@pytest.mark.parametrize("creator_cls,expected_type", [
    (CubeCreator, trimesh.Trimesh),
    (SphereCreator, trimesh.Trimesh),
    (CylinderCreator, trimesh.Trimesh),
    (ConeCreator, trimesh.Trimesh),
])
def test_basic_shape_creators_apply_transformations(creator_cls, expected_type):
    obj_conf = DummyConf(model="cube")
    creator = creator_cls()
    mesh = creator.create_shape(obj_conf)
    assert isinstance(mesh, expected_type)
    assert hasattr(mesh, "apply_transform")

def test_custom_model_creator_loads_file(monkeypatch):
    fake_mesh = MagicMock(spec=trimesh.Trimesh)
    monkeypatch.setattr(trimesh, "load", lambda path: fake_mesh)

    obj_conf = DummyConf(model="custom", model_path="fake.obj")
    creator = CustomModelCreator()
    mesh = creator.create_shape(obj_conf)
    assert mesh is fake_mesh
    assert fake_mesh.apply_transform.call_count == 2

# ---- ShapeFactory Tests ----
def test_shape_factory_create_shape():
    factory = ShapeFactory()
    conf = DummyConf(model="sphere")
    shape = factory.create_shape(conf)
    assert isinstance(shape, trimesh.Trimesh)


def test_shape_factory_register_creator():
    factory = ShapeFactory()
    fake_creator = MagicMock()
    factory.register_creator("weird", fake_creator)
    conf = DummyConf(model="weird")
    factory.create_shape(conf)
    
    fake_creator.create_shape.assert_called_once_with(conf)
    
def test_shape_factory_unsupported_type():
    factory = ShapeFactory()
    with pytest.raises(ValueError):
        factory.create_shape(DummyConf(model="idontexist"))


def test_export_scene_creates_folder_and_exports():
    scene_mock = MagicMock()

    with patch.object(Path, "mkdir") as mkdir_mock, \
         patch.object(scene_mock, "export") as export_mock:

        export_scene(scene_mock, "myfile", output_folder="myfolder")

        mkdir_mock.assert_called_once_with(exist_ok=True)
        export_mock.assert_called_once_with("myfolder/myfile.obj")    


def test_build_scene_adds_all_objects():
    # Dummy config with 2 objects
    obj1 = MagicMock()
    obj2 = MagicMock()
    config = MagicMock(objects=[obj1, obj2])

    shape1 = MagicMock()
    shape2 = MagicMock()
    shape_factory = MagicMock()
    shape_factory.create_shape.side_effect = [shape1, shape2]

    # Base scene mock
    base_scene = MagicMock()

    # Call function
    result = build_scene(config, shape_factory, base_scene)

    # Assertions
    assert result is base_scene  
    shape_factory.create_shape.assert_any_call(obj1)
    shape_factory.create_shape.assert_any_call(obj2)
    base_scene.add_geometry.assert_any_call(shape1)
    base_scene.add_geometry.assert_any_call(shape2)

# ---- ConfigProcessor Tests ----
def test_config_processor_get_files(tmp_path):
    yaml_file = tmp_path / "config.yaml"
    yaml_file.write_text("fake: config")
    processor = ConfigProcessor(tmp_path)
    files = processor.get_config_files()
    assert yaml_file in files


def test_scene_manager_calls_dependencies():
    # Patch bağımlılıkları - load_scene'i doğru yerde patch ediyoruz
    with patch("core.augment_writer.ConfigProcessor") as ConfigProcessorMock, \
         patch("core.augment_writer.ShapeFactory") as ShapeFactoryMock, \
         patch("core.augment_writer.load_scene") as load_scene_mock, \
         patch("core.augment_writer.build_scene") as build_scene_mock, \
         patch("core.augment_writer.export_scene") as export_scene_mock:

        # ConfigProcessor mock setup
        config_processor_instance = ConfigProcessorMock.return_value
        dummy_file = Path("file1.yaml")
        config_processor_instance.get_config_files.return_value = [dummy_file]
        dummy_config = MagicMock()
        config_processor_instance.load_config.return_value = dummy_config

        # load_scene ve build_scene return
        dummy_scene = MagicMock()
        load_scene_mock.return_value = MagicMock()  # base_scene
        build_scene_mock.return_value = dummy_scene

        # Test
        manager = SceneManager("conf_folder", "base_map", "out_folder")
        manager.process_all_scenes()

        # Assertions
        config_processor_instance.get_config_files.assert_called_once()
        config_processor_instance.load_config.assert_called_once_with(dummy_file)
        load_scene_mock.assert_called_once_with("base_map")
        build_scene_mock.assert_called_once_with(dummy_config, ShapeFactoryMock.return_value, load_scene_mock.return_value)
        export_scene_mock.assert_called_once_with(dummy_scene, dummy_file.stem, "out_folder")


def test_run_scene_builder_creates_manager_and_calls_process():
    with patch("core.augment_writer.SceneManager") as SceneManagerMock:
        instance = SceneManagerMock.return_value

        run_scene_builder("c_folder", "b_map", "o_folder")

        SceneManagerMock.assert_called_once_with("c_folder", "b_map", "o_folder")
        instance.process_all_scenes.assert_called_once()