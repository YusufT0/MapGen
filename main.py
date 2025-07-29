import yaml
import trimesh
import numpy as np
from pathlib import Path
import uuid
import os


# print(trimesh.__version__)

def load_config(path) -> dict:
    yaml_path = Path(path)
    with open(yaml_path, 'r') as file:
        config = yaml.safe_load(file)
    return config

def load_scene(map_path: Path) -> trimesh.Scene:
    if map_path.exists():
        mesh = trimesh.load(map_path)
        return trimesh.Scene(mesh)
    return trimesh.Scene()

def create_model(bounds,base_mesh, aug: dict) -> trimesh.Trimesh:
    
    if aug['model'] == 'cube':
        scale = aug.get('scale', 5.0)
        count = aug.get('count', 1)
        position = aug.get('position', 'random')
        cubes = []

        for i in range(0, count):
            cube = trimesh.creation.box(extents=scale * np.ones(3))
            cube.visual.face_colors = [255, 0, 0, 255]  # Red color

            try:
                if (aug['position'] == "random"):
                    x,y,z = create_random_xyz(bounds)
                    conf_y = control_random_creation(x,y,z, base_mesh)
                    final_y = conf_y + (scale / 2)
                    cube.apply_translation([x, final_y, z])
                else:
                    cube.apply_translation(position * np.ones(3))
            except:
                print("Please provide a valid position [x,y,z] or 'random'.")

            cubes.append(cube)

        return cubes
    
    raise ValueError(f"Unknown model type: {aug['model']}")


def visualize_ray(scene, origin, direction, length=10.0, hit_points=None):
    # Ensure origin and direction are numpy arrays
    origin = np.array(origin)
    direction = np.array(direction).flatten()  # Flatten in case it's 2D
    
    # Create a thin cylinder to represent the ray
    end_point = origin + direction * length
    ray_length = np.linalg.norm(end_point - origin)
    
    # Create a thin cylinder
    cylinder = trimesh.creation.cylinder(radius=0.02, height=ray_length)
    
    # Calculate the transformation to align the cylinder with the ray direction
    # Default cylinder is along Z-axis, we need to rotate it to align with our direction
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
    
    # Set color to make it visible (optional)
    cylinder.visual.face_colors = [255, 0, 0, 128]  # Red with some transparency
    
    scene.add_geometry(cylinder)
    
    # Add hit points as small spheres
    if hit_points is not None and len(hit_points) > 0:
        for pt in hit_points:
            sphere = trimesh.creation.uv_sphere(radius=0.1)
            sphere.apply_translation(pt)
            sphere.visual.face_colors = [0, 255, 0, 255]  # Green spheres
            scene.add_geometry(sphere)


def control_random_creation(x, y, z, mesh_for_query):
    offset = 0.01
    sceneCenter = 0
    if y < sceneCenter:
        ray_direction = np.array([0, 1, 0])  # Work in y axis 
        ray_origin = np.array([x, y - offset, z])  
    else:
        ray_direction = np.array([0, -1, 0])   
        ray_origin = np.array([x, y + offset, z])  

    locations, index_ray, index_tri = mesh_for_query.ray.intersects_location(
        ray_origins=[ray_origin],
        ray_directions=[ray_direction]  
    )

    # visualize_ray(scene, ray_origin, ray_direction, length=20.0, hit_points=locations)

    # Check if we have any intersections at all
    if len(locations) > 0:
        # Find the closest intersection point to the ray origin
        distances = np.linalg.norm(locations - ray_origin, axis=1)
        closest_idx = np.argmin(distances)
        closest_intersection = locations[closest_idx]
        
        print(f"Ray hit the mesh at: {closest_intersection}")
        return closest_intersection[1]  # Return Y coordinate of intersection
    else:
        # No intersections found, use nearest point on surface
        closest_point, distance, face_id = mesh_for_query.nearest.on_surface([ray_origin])
        print(f"No ray intersection, using nearest surface point: {closest_point[0]}")
        return closest_point[0][1]
    

def create_random_xyz(map_bounds):
    x = np.random.uniform(map_bounds[0][0], map_bounds[1][0])
    y = np.random.uniform(map_bounds[0][1], map_bounds[1][1])
    z = np.random.uniform(map_bounds[0][2], map_bounds[1][2])
    return x,y,z



def apply_augmentations(scene: trimesh.Scene, augmentations: list):
    map_bounds = scene.bounds
    map_count = config.get('map_count', 1)  # Get map_count from top level

    if ((os.path.exists) == False):
        os.mkdir('maps')
    base_mesh = trimesh.util.concatenate(scene.dump())  # only terrain models here
    for i in range(map_count):
        scene_copy = scene.copy()  # Create a copy for each map
        for aug in augmentations:
            if aug['type'] == 'add_model':
                models = create_model(map_bounds, base_mesh, aug)
                for model in models:
                    scene_copy.add_geometry(model)
        
        filename = f"./maps/map_{uuid.uuid4()}.obj"
        export_scene(scene_copy, filename)
        
def export_scene(scene: trimesh.Scene, output_path: str):
    scene.export(output_path)


if __name__ == "__main__":
    path = "./setup.yaml"
    config = load_config(path)
    scene = load_scene(Path(config['map']))
    apply_augmentations(scene, config.get('augmentations', []))

    # export_scene(scene, "updated_map.obj")
