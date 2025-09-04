import sys
import os

# Add the parent directory (MapGen) to PYTHONPATH
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '..')))

import pytest
from core.augment_tool import augment, AugmenterRegistry, ModelAdder, LandScape, control_collision
from models.domain import ModelObject, CustomModelObject, StandardModelObject


class DummyAug:
    def __init__(self, type_, count=1, position="random", scale=1.0, model="tree", custom_path=None, color=None):
        self.type = type_
        self.count = count
        self.position = position
        self.scale = scale
        self.model = model
        self.custom_path = custom_path
        self.color = color

class DummyMesh:
    def __init__(self):
        # Mock ray and nearest for control_random_creation
        self.ray = self
        self.nearest = self
    
    def intersects_location(self, ray_origins, ray_directions):
        # Always return a fixed point
        return ([ [0,0,0] ], None, None)
    
    def on_surface(self, points):
        return ([ [0,0,0] ], None, None)

def test_registry_register_and_get():
    assert AugmenterRegistry.get("add_model") is ModelAdder
    assert AugmenterRegistry.get("landscape") is LandScape
    assert AugmenterRegistry.get("nonexistent") is None

def test_augment_add_model_random_position():
    bounds = [(0,0,0),(100,100,100)]
    mesh = DummyMesh()
    aug = DummyAug("add_model", count=2, position="random", scale=1.0, model="tree", color=[0.0, 1.0, 0.0])
    results = augment(bounds, aug, mesh)
    assert len(results) == 2
    for obj in results:
        # Check the type of the model object
        assert isinstance(obj, StandardModelObject)
        assert obj.model == "tree"
        assert len(obj.position) == 3

def test_augment_add_model_custom_model():
    bounds = [(0,0,0),(100,100,100)]
    mesh = DummyMesh()
    aug = DummyAug("add_model", count=1, position="random", scale=1.0, model="custom", custom_path="path/to/model.obj")
    results = augment(bounds, aug, mesh)
    assert len(results) == 1
    obj = results[0]
    assert isinstance(obj, CustomModelObject)
    assert obj.model == "custom"
    assert obj.model_path == "path/to/model.obj"

def test_augment_add_model_fixed_position():
    bounds = [(0,0,0),(10,10,10)]
    mesh = DummyMesh()
    fixed_pos = [1.0, 2.0, 3.0]
    aug = DummyAug("add_model", count=1, position=fixed_pos, scale=1.0, model="tree", color=[0.0, 1.0, 0.0]) 
    results = augment(bounds, aug, mesh)
    assert len(results) == 1
    assert results[0].position == fixed_pos

def test_augment_unknown_type_raises():
    bounds = [(0,0,0),(100,100,100)]
    mesh = DummyMesh()
    aug = DummyAug("unknown_type")
    with pytest.raises(ValueError):
        augment(bounds, aug, mesh)

def test_control_collision_with_collision():
    placed_objects = [
        StandardModelObject(model="tree.obj", position=[0, 0, 0], scale=1.0, color=[0.0, 1.0, 0.0])
    ]
    new_pos = [0.2, 0, 0]  # Close, colliding
    new_scale = 1.0

    assert control_collision(new_pos, new_scale, placed_objects) == True

def test_control_collision_without_collision():
    placed_objects = [
        StandardModelObject(model="tree.obj", position=[0, 0, 0], scale=1.0, color=[0.0, 1.0, 0.0])
    ]
    new_pos = [3.0, 0, 0]  # Far, no collision
    new_scale = 1.0

    assert control_collision(new_pos,new_scale, placed_objects) == False
