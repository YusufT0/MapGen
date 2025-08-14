import numpy as np
import trimesh
from pathlib import Path
from abc import ABC, abstractmethod
import shutil
from core.reader import load_map_config, load_scene
from trimesh.transformations import translation_matrix, scale_matrix
from core.augment_tool import control_random_creation


class ShapeCreator(ABC):
    """
    Abstract base class for shape creation.
    """
    
    @abstractmethod
    def create_shape(self, obj_conf) -> trimesh.Trimesh:
        """Create a specific shape based on configuration."""
        pass
    
    def _apply_transformations(self, mesh: trimesh.Trimesh, obj_conf):
        """Apply common transformations (scale, position, color)."""
        # Set color
        color = np.array(obj_conf.color * np.ones(3))
        mesh.visual.face_colors = color
        
        # Apply transformations
        mesh.apply_transform(scale_matrix(obj_conf.scale))
        mesh.apply_transform(translation_matrix(obj_conf.position))
        
        return mesh


class CubeCreator(ShapeCreator):
    """Creates cube shapes."""
    
    def create_shape(self, obj_conf) -> trimesh.Trimesh:
        cube = trimesh.creation.box(extents=[1, 1, 1])
        return self._apply_transformations(cube, obj_conf)


class SphereCreator(ShapeCreator):
    """Creates sphere shapes."""
    
    def create_shape(self, obj_conf) -> trimesh.Trimesh:
        sphere = trimesh.creation.uv_sphere(radius=0.5)
        return self._apply_transformations(sphere, obj_conf)


class CylinderCreator(ShapeCreator):
    """Creates cylinder shapes."""
    
    def create_shape(self, obj_conf) -> trimesh.Trimesh:
        cylinder = trimesh.creation.cylinder(radius=0.5, height=1.0)
        return self._apply_transformations(cylinder, obj_conf)


class ConeCreator(ShapeCreator):
    """Creates cone shapes."""
    
    def create_shape(self, obj_conf) -> trimesh.Trimesh:
        cone = trimesh.creation.cone(radius=0.5, height=1.0)
        return self._apply_transformations(cone, obj_conf)


class CustomModelCreator(ShapeCreator):
    """
    Creates shapes from custom model files with MTL support.
    """
    
    def __init__(self):
        self.used_mtl_files = set()  # Track MTL files we need to handle
    
    def create_shape(self, obj_conf) -> trimesh.Trimesh:
        # Load custom model 
        mesh = trimesh.load(obj_conf.model_path)
        
        # Track the MTL file if it exists
        self._track_mtl_file(obj_conf.model_path)
        
        # Only apply scale and position for custom models (they have their own materials)
        mesh.apply_transform(scale_matrix(obj_conf.scale))
        mesh.apply_transform(translation_matrix(obj_conf.position))
        
        # Add metadata for unique naming
        mesh.metadata['original_name'] = getattr(obj_conf, 'name', 'CustomObject')
        
        return mesh
    
    def _track_mtl_file(self, obj_path):
        """Track MTL files that need to be copied during export"""
        obj_path = Path(obj_path)
        mtl_path = obj_path.with_suffix('.mtl')
        
        if mtl_path.exists():
            self.used_mtl_files.add(mtl_path)
    
    def _apply_transformations(self, mesh, obj_conf):
        """
        Changing apply_transformations because there is no need for colors for
        custom models. They have their own .mtl files for colors.
        """
        mesh.apply_transform(scale_matrix(obj_conf.scale))
        mesh.apply_transform(translation_matrix(obj_conf.position))
        
        return mesh
    
    def get_used_mtl_files(self):
        """Get list of MTL files that were used"""
        return list(self.used_mtl_files)
    
    def reset_mtl_tracking(self):
        """Reset MTL tracking for new scene"""
        self.used_mtl_files.clear()


class ShapeFactory:
    """
    Factory for creating shapes based on model type.
    If anyone is going to create a new shape for the config:
    Add the new model name as string here and create a new shape class. Inherit from the ShapeCreator
    Then one can change the create_shape function that is inside of that class.
    """
    
    def __init__(self):
        self._creators = {
            'cube': CubeCreator(),
            'sphere': SphereCreator(),
            'cylinder': CylinderCreator(),
            'cone': ConeCreator(),
            'custom': CustomModelCreator(),
        }
    
    def create_shape(self, obj_conf) -> trimesh.Trimesh:
        """Create a shape based on the object configuration."""
        model_type = obj_conf.model.lower()
        
        if model_type not in self._creators:
            raise ValueError(f"Unsupported model type: {model_type}")
        
        return self._creators[model_type].create_shape(obj_conf)
    
    def register_creator(self, model_type: str, creator: ShapeCreator):
        """Register a new shape creator."""
        self._creators[model_type] = creator
    
    def get_used_mtl_files(self):
        """Get all MTL files used by custom model creators"""
        all_mtl_files = set()
        for creator in self._creators.values():
            if hasattr(creator, 'get_used_mtl_files'):
                all_mtl_files.update(creator.get_used_mtl_files())
        return list(all_mtl_files)
    
    def reset_mtl_tracking(self):
        """Reset MTL tracking for all creators"""
        for creator in self._creators.values():
            if hasattr(creator, 'reset_mtl_tracking'):
                creator.reset_mtl_tracking()


def apply_landscape(config, landscape_builder, base_scene):
    for land in config.landscapes:
        new_mesh = landscape_builder.create_landscape(base_scene, land)
        geom_name = next(iter(base_scene.geometry.keys()))
        del base_scene.geometry[geom_name]
        base_scene.add_geometry(new_mesh, geom_name)
    return base_scene


def build_scene(config, shape_factory, base_scene):
    """
    Load the scene, load the shape factory, find the correct create_shape function.
    Create the shape with trimesh add_geometry api.
    Do it for all the objects that map configuration contains.
    """
    mesh = next(iter(base_scene.geometry.values()))
    for i, obj in enumerate(config.objects):
        new_y = control_random_creation(obj.position[0], obj.position[1], obj.position[2], mesh) + (obj.scale / 2)
        obj.position[1] = new_y
        shape = shape_factory.create_shape(obj)
        
        # Create unique node name for each object
        object_name = getattr(obj, 'name', f'Object_{i:03d}')
        base_scene.add_geometry(shape, node_name=object_name)
    return base_scene


def _merge_mtl_files(mtl_files, output_path):
    """Merge multiple MTL files into one, handling name conflicts"""
    merged_materials = {}
    
    for mtl_file in mtl_files:
        with open(mtl_file, 'r') as f:
            content = f.read()
            
        # Simple parsing to extract material names and avoid conflicts
        current_material = None
        for line in content.split('\n'):
            line = line.strip()
            if line.startswith('newmtl '):
                material_name = line.split(' ', 1)[1]
                # Add prefix to avoid conflicts between different models
                model_prefix = Path(mtl_file).stem
                unique_name = f"{model_prefix}_{material_name}"
                merged_materials[unique_name] = []
                current_material = unique_name
                merged_materials[current_material].append(f"newmtl {unique_name}")
            elif current_material and line and not line.startswith('#'):
                merged_materials[current_material].append(line)
    
    # Write merged MTL file
    with open(output_path, 'w') as f:
        f.write("# Merged MTL file from multiple custom models\n\n")
        for material_name, lines in merged_materials.items():
            for line in lines:
                f.write(line + '\n')
            f.write('\n')


def export_scene(scene, filename, output_folder="maps", shape_factory=None):
    """
    Go into the output folder. If it's not exists create one.
    Then export the scene to the output folder with the corresponding filename.
    The filename is going to be created uniquely when the entry code is executed.
    Enhanced to handle MTL files from custom models for OBJ exports only.
    """
    Path(output_folder).mkdir(exist_ok=True)
    
    # Export the scene
    output_path = f"{output_folder}/{filename}"
    
    # Determine file extension from filename
    file_path = Path(filename)
    file_extension = file_path.suffix.lower()

    # Export the scene with the proper filename
    scene.export(f"{output_path}")
    
    # Only handle MTL files for OBJ exports
    if file_extension == '.obj' and shape_factory:
        mtl_files = shape_factory.get_used_mtl_files()
        
        if mtl_files:
            # Get the base name without extension for MTL file
            base_name = file_path.stem if file_path.suffix else filename
            merged_mtl_path = f"{output_folder}/{base_name}.mtl"
            _merge_mtl_files(mtl_files, merged_mtl_path)
            print(f"Created MTL file for OBJ export: {base_name}.mtl with {len(mtl_files)} source materials")
    elif file_extension != '.obj' and shape_factory and shape_factory.get_used_mtl_files():
        print(f"Note: MTL materials not applicable for {file_extension} format - materials embedded in file format")




class ConfigProcessor:
    """Processes configuration files."""
    
    def __init__(self, config_folder: str):
        self.config_folder = Path(config_folder)
    
    def get_config_files(self) -> list[Path]:
        """Get all YAML configuration files."""
        return list(self.config_folder.glob("*.yaml"))
    
    def load_config(self, config_file: Path):
        """Load a single configuration file."""
        return load_map_config(str(config_file))


class LandScapeBuilder:
    def __init__(self):
        pass
        
    def create_landscape(self, scene, landscape_config):
        mesh = next(iter(scene.geometry.values()))
        position = list(landscape_config.position)
        smoothness = float(landscape_config.smoothness)
        radius = float(landscape_config.radius)

        vertices = mesh.vertices.copy()
        peak_x, peak_y, peak_z = position[0], position[1], position[2]
        
        vertex_xz = vertices[:, [0, 2]]
        peak_xz = np.array([peak_x, peak_z])
        distances = np.linalg.norm(vertex_xz - peak_xz, axis=1)

        max_radius = abs(peak_y) * radius  # Use absolute value for radius calculation
        affected_mask = distances <= max_radius
        affected_vertices = np.where(affected_mask)[0]
        
        for i in affected_vertices:
            vertex = vertices[i]
            distance = distances[i]
            
            if distance == 0:
                falloff = 1.0
            else:
                normalized_distance = distance / max_radius
                if smoothness == 0:
                    # Linear falloff (sharp edges)
                    falloff = max(0, 1 - normalized_distance)
                elif smoothness == 1:
                    # Smooth polynomial falloff (cosine-based for natural hills)
                    falloff = 0.5 * (1 - np.cos(np.pi * (1-normalized_distance))) if normalized_distance <= 1 else 0
                else:
                    # Blend between linear and polynomial
                    linear_falloff = max(0, 1 - normalized_distance)
                    smooth_falloff = 0.5 * (1 + np.cos(np.pi * normalized_distance)) if normalized_distance <= 1 else 0
                    falloff = linear_falloff * (1 - smoothness) + smooth_falloff * smoothness
            
            ground_y = control_random_creation(vertex[0], vertex[1], vertex[2], mesh)
            
            if peak_y >= 0:
                # Raising terrain (mountains/hills)
                target_height = ground_y + (peak_y - ground_y) * falloff
                if target_height > ground_y and target_height > vertex[1]:
                    vertices[i][1] = target_height
            else:
                # Lowering terrain (valleys/depressions)
                depression_depth = abs(peak_y) * falloff
                target_height = ground_y - depression_depth
                if target_height < ground_y and target_height < vertex[1]:
                    vertices[i][1] = target_height

        mesh.vertices = vertices
        mesh.remove_degenerate_faces()
        mesh.remove_duplicate_faces()
        mesh.fix_normals()
        return mesh


class SceneManager:
    """
    Main orchestrator class that coordinates all components.
    Manager is going to take the configuration input folder,
    base map file that the augmentations are going to be made and
    the map output folder.
    """
    
    def __init__(self, config_folder, base_map, output_folder):
        self.base_map = base_map
        self.config_processor = ConfigProcessor(config_folder)
        self.shape_factory = ShapeFactory()
        self.landscape_builder = LandScapeBuilder()
        self.output_folder = output_folder
        self.base_scene = load_scene(self.base_map)
    
    def process_all_scenes(self):
        config_files = self.config_processor.get_config_files()
        
        for config_file in config_files:
            config = self.config_processor.load_config(config_file)
            
            # Reset MTL tracking for each new scene
            self.shape_factory.reset_mtl_tracking()
            
            # Load a fresh copy of the base scene for each config
            fresh_base_scene = load_scene(self.base_map)
            
            scene_with_landscape = apply_landscape(config, self.landscape_builder, fresh_base_scene)
            final_scene = build_scene(config, self.shape_factory, scene_with_landscape)
            
            # Pass shape_factory to export_scene for MTL handling
            export_scene(final_scene, config.map, self.output_folder, self.shape_factory)


def run_scene_builder(config_folder="./configs", base_map="./map.obj", output_folder="./maps"):
    """Entry point function."""
    print(base_map)
    manager = SceneManager(config_folder, base_map, output_folder)
    manager.process_all_scenes()