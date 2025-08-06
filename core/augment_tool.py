import numpy as np
import math
import trimesh
import yaml
from schemas.domain import ModelObject

def augment(bounds, aug, base_mesh):
    if aug.type == "add_model":
        add_configs = create_adding_config(bounds, aug, base_mesh)
    else:
        landscape_configs = create_landscape_config()

    return add_configs


# TODO add landscape logic to here.
def create_landscape_config():
    return []


# The function that is going to be called. Sums every function up.
def create_adding_config(bounds, aug, base_mesh):
    save_inf = []
    for i in range(aug.count):
        if aug.position == "random":
            trigger = True
            while trigger:
                x, y, z = create_random_xyz(bounds)
                conf_y = control_random_creation(x, y, z, base_mesh)
                final_y = conf_y + aug.scale / 2
                updated_coords = [x, final_y, z]
                trigger = control_collision(updated_coords, save_inf, aug.scale)
        else:
            updated_coords = aug.position
        save_inf.append(
                ModelObject(
                    model=aug.model,
                    position=updated_coords,
                    scale=aug.scale
                )
            )

    
    return save_inf


# Function to control if the new model that is going to be placed is colliding with other meshes.
def control_collision(new_pos, placed_objects, new_scale):
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



# Random position creator.
def create_random_xyz(map_bounds):
    x = np.random.uniform(map_bounds[0][0], map_bounds[1][0])
    y = np.random.uniform(map_bounds[0][1], map_bounds[1][1])
    z = np.random.uniform(map_bounds[0][2], map_bounds[1][2])
    return x,y,z


# Controlling if the random position that is selected is on the map.
# If it's not it fixes the relevant mesh to the nearest location on the map.
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
        
        # print(f"Ray hit the mesh at: {closest_intersection}")
        return closest_intersection[1]  # Return Y coordinate of intersection
    else:
        # No intersections found, use nearest point on surface
        closest_point, distance, face_id = mesh_for_query.nearest.on_surface([ray_origin])
        # print(f"No ray intersection, using nearest surface point: {closest_point[0]}")
        return closest_point[0][1]



#Just a helper function to see can we find the nearest point to the ground.
#Just for visualization, there is nothing that is effecting the main logic.
def visualize_ray(scene, origin, direction, length=10.0, hit_points=None):

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
