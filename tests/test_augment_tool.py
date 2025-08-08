import pytest
from core.augment_tool import augment, AugmenterRegistry, ModelAdder, LandScape, control_collision
from models.domain import ModelObject

class DummyAug:
    def __init__(self, type_, count=1, position="random", scale=1.0, model="tree"):
        self.type = type_
        self.count = count
        self.position = position
        self.scale = scale
        self.model = model

class DummyMesh:
    def __init__(self):
        # mock ray and nearest for control_random_creation
        self.ray = self
        self.nearest = self
    
    def intersects_location(self, ray_origins, ray_directions):
        # always return a fixed point
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
    aug = DummyAug("add_model", count=2, position="random", scale=1.0)
    results = augment(bounds, aug, mesh)
    assert len(results) == 2
    for obj in results:
        assert isinstance(obj, ModelObject)
        assert obj.model == "tree"
        assert len(obj.position) == 3


def test_augment_add_model_fixed_position():
    bounds = [(0,0,0),(10,10,10)]
    mesh = DummyMesh()
    fixed_pos = [1.0, 2.0, 3.0]
    aug = DummyAug("add_model", count=1, position=fixed_pos, scale=1.0)
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
        ModelObject(model="tree.obj", position=[0, 0, 0], scale=1.0)
    ]
    new_pos = [0.2, 0, 0]  # Yakın, çakışmalı
    new_scale = 1.0

    assert control_collision(new_pos, new_scale, placed_objects) == True


def test_control_collision_without_collision():
    placed_objects = [
        ModelObject(model="tree.obj", position=[0, 0, 0], scale=1.0)
    ]
    new_pos = [3.0, 0, 0]  # Uzak, çakışmaz
    new_scale = 1.0

    assert control_collision(new_pos,new_scale, placed_objects) == False