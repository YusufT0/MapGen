"""
augment_tool.py

Contains augmentation logic for 3D map configuration files. Applies various transformation
and model addition strategies using registered augmentation classes.

"""
from abc import ABC, abstractmethod
import math
import numpy as np
import trimesh
from models.domain import CustomModelObject, StandardModelObject


class AugmenterRegistry:
    """
    A decorator class that is for creating augmentations.
    """
    _registry = {}

    @classmethod
    def register(cls, type_name):
        def wrapper(klass):
            cls._registry[type_name] = klass
            return klass
        return wrapper

    @classmethod
    def get(cls, type_name):
        return cls._registry.get(type_name)


# Base Class
class Augmenter(ABC):
    """
    An abstract class for providing augmentation informations. 
    All other augmentations is going to inherit from this one.
    """
    def __init__(self, bounds, base_mesh):
        self.bounds = bounds
        self.base_mesh = base_mesh

    @abstractmethod
    def generate(self, aug):
        pass



@AugmenterRegistry.register("add_model")
class ModelAdder(Augmenter):
    """
    ModelAdder class is for adding models to the map.
    """
    def generate(self, aug):
        results = []
        for _ in range(aug.count):
            pos = self._get_position(aug, results)
            model_obj = self._create_model_object(aug, pos)
            results.append(model_obj)
        return results

    def _get_position(self, aug, results):
        if aug.position == "random":
            return self._find_valid_position(aug.scale, results)
        return aug.position

    def _create_model_object(self, aug, position):
        base_data = {
            'model': aug.model,
            'position': position,
            'scale': aug.scale
        }
        
        if aug.model == "custom":
            return CustomModelObject(model_path=aug.custom_path, **base_data)
        else:
            return StandardModelObject(color=aug.color, **base_data)
    
    

    def _find_valid_position(self, scale, placed):
        """
        Create random positions, Control if that random positions attached to the ground.
        If it is control if that random positions collide with other objects.  
        """

        while True:
            x, y, z = create_random_xyz(self.bounds)
            y_fixed = control_random_creation(x, y, z, self.base_mesh) + scale / 2
            new_pos = [x, y_fixed, z]
            if not control_collision(new_pos, scale, placed):
                return new_pos
            

#TODO: Add landscape module. # pylint: disable=fixme
@AugmenterRegistry.register("landscape")
class LandScape(Augmenter):
    """
    This class is going to contain all of the environment manipulations 
    """
    def generate(self, aug):
        return []
    def do_things(self):
        pass

def augment(bounds, aug, base_mesh):
    """
    The function that is going to be called from outside.
    All main functionalities will work here.
    """
    augmenter_class = AugmenterRegistry.get(aug.type)

    if augmenter_class is None:
        raise ValueError(f"Unknown augment type: {aug.type}")

    augmenter = augmenter_class(bounds, base_mesh)
    return augmenter.generate(aug)



def create_random_xyz(map_bounds):
    """
    Creates random locations
    """
    x = np.random.uniform(map_bounds[0][0], map_bounds[1][0])
    y = np.random.uniform(map_bounds[0][1], map_bounds[1][1])
    z = np.random.uniform(map_bounds[0][2], map_bounds[1][2])
    return x, y, z



def control_collision(new_pos, new_scale, placed_objects):
    """
    Check if the model that is going to be created is colliding.
    """
    for obj in placed_objects:
        old_pos = obj.position
        old_scale = obj.scale

        dist = math.sqrt(
            (new_pos[0] - old_pos[0]) ** 2 +
            (new_pos[1] - old_pos[1]) ** 2 +
            (new_pos[2] - old_pos[2]) ** 2
        )

        min_dist = (new_scale + old_scale) / 2
        if dist < min_dist:
            return True

    return False

def control_random_creation(x, y, z, mesh_for_query):
    """
    Check if the object that is going to be created is on the ground.
    """
    offset = 0.01
    scene_center = 0

    if y < scene_center:
        ray_direction = np.array([0, 1, 0])
        ray_origin = np.array([x, y - offset, z])
    else:
        ray_direction = np.array([0, -1, 0])
        ray_origin = np.array([x, y + offset, z])

    locations, _, _ = mesh_for_query.ray.intersects_location(
        ray_origins=[ray_origin],
        ray_directions=[ray_direction]
    )

    if len(locations) > 0:
        distances = np.linalg.norm(locations - ray_origin, axis=1)
        closest_idx = np.argmin(distances)
        closest_intersection = locations[closest_idx]
        return closest_intersection[1]
    closest_point, _, _ = mesh_for_query.nearest.on_surface([ray_origin])
    return closest_point[0][1]


def visualize_ray(scene, origin, direction, length=10.0, hit_points=None):
    """
    Just a helper function to see can we find the nearest point to the ground.
    Just for visualization, there is nothing that is effecting the main logic.
    """
    origin = np.array(origin)
    direction = np.array(direction).flatten()  # Flatten in case it's 2D
    end_point = origin + direction * length
    ray_length = np.linalg.norm(end_point - origin)
    cylinder = trimesh.creation.cylinder(radius=0.02, height=ray_length)
    z_axis = np.array([0, 0, 1])
    if not np.allclose(direction, z_axis):
        rotation_axis = np.cross(z_axis, direction)
        if np.linalg.norm(rotation_axis) > 1e-6:  # Avoid division by zero
            rotation_axis = rotation_axis / np.linalg.norm(rotation_axis)
            angle = np.arccos(np.clip(np.dot(z_axis, direction), -1, 1))
            rotation_matrix = trimesh.transformations.rotation_matrix(angle, rotation_axis)
            cylinder.apply_transform(rotation_matrix)
    # Translate to the midpoint of the ray
    midpoint = origin + direction * (ray_length / 2)
    cylinder.apply_translation(midpoint)
    scene.add_geometry(cylinder)
    # Add hit points as small spheres
    if hit_points is not None and len(hit_points) > 0:
        for pt in hit_points:
            sphere = trimesh.creation.uv_sphere(radius=0.1)
            sphere.apply_translation(pt)
            scene.add_geometry(sphere)
