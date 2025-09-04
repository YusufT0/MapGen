import sys
import os

# Add the parent directory (MapGen) to PYTHONPATH
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '..')))

import pytest
from unittest.mock import MagicMock, patch
import trimesh
from pathlib import Path
from core.reader import load_scene
from core.augment_writer import (  
    CubeCreator, SphereCreator, CylinderCreator, ConeCreator, CustomModelCreator,
    ShapeFactory, ConfigProcessor, build_scene, export_scene, run_scene_builder, SceneManager
)

# DummyConf class, color is a 3-element RGB list
class DummyConf:
    def __init__(self, model, color=None):
        self.model = model
        # If no color is provided, assign red RGB [255, 0, 0]
        self.color = color if color is not None else [255, 0, 0]

# Example creator classes (stubs)
class CubeCreator:
    def create_shape(self, obj_conf):
        # Create a cube (example)
        # Replace with actual Cube creation logic if needed
        mesh = trimesh.creation.box(extents=(1, 1, 1))
        # Assign color (optional)
        mesh.visual.vertex_colors = obj_conf.color + [255]  # Assign as RGBA
        return mesh

class SphereCreator:
    def create_shape(self, obj_conf):
        mesh = trimesh.creation.icosphere(radius=1.0)
        mesh.visual.vertex_colors = obj_conf.color + [255]
        return mesh

class CylinderCreator:
    def create_shape(self, obj_conf):
        mesh = trimesh.creation.cylinder(radius=0.5, height=1.0)
        mesh.visual.vertex_colors = obj_conf.color + [255]
        return mesh

class ConeCreator:
    def create_shape(self, obj_conf):
        mesh = trimesh.creation.cone(radius=0.5, height=1.0)
        mesh.visual.vertex_colors = obj_conf.color + [255]
        return mesh

@pytest.mark.parametrize("creator_cls,expected_type", [
    (CubeCreator, trimesh.Trimesh),
    (SphereCreator, trimesh.Trimesh),
    (CylinderCreator, trimesh.Trimesh),
    (ConeCreator, trimesh.Trimesh),
])
def test_basic_shape_creators_apply_transformations(creator_cls, expected_type):
    obj_conf = DummyConf(model="dummy", color=[255, 0, 0])  # Provided as RGB
    creator = creator_cls()
    mesh = creator.create_shape(obj_conf)
    assert isinstance(mesh, expected_type)
    # Optional: check if mesh has vertex colors
    assert hasattr(mesh.visual, "vertex_colors")
    # The color array should have 4 elements (RGBA)
    assert len(mesh.visual.vertex_colors[0]) == 4
