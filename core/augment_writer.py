import numpy as np
import trimesh
from pathlib import Path
from abc import ABC, abstractmethod
from core.reader import load_map_config, load_scene
from trimesh.transformations import translation_matrix, scale_matrix


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
        color = np.array(obj_conf.color * np.ones(4))
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
    Creates shapes from custom model files.
    """
    
    def create_shape(self, obj_conf) -> trimesh.Trimesh:
        # Load custom model 
        mesh = trimesh.load(obj_conf.model_path)
        
        # Only apply scale and position for custom models (they have their own materials)
        mesh.apply_transform(scale_matrix(obj_conf.scale))
        mesh.apply_transform(translation_matrix(obj_conf.position))
        
        return mesh
    
    def _apply_transformations(self, mesh, obj_conf):
        """
        Changing apply_transformations because there is no need for colors for
        custom models. They have their own .mtl files for colors.
        """
        mesh.apply_transform(scale_matrix(obj_conf.scale))
        mesh.apply_transform(translation_matrix(obj_conf.position))
        
        return mesh

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



def build_scene(config, shape_factory, base_scene):
    """
    Load the scene, load the shape factory, find the correct create_shape function.
    Create the shape with trimesh add_geometry api.
    Do it for all the objects that map configuration contains.
    """
    for obj in config.objects:
        shape = shape_factory.create_shape(obj)
        base_scene.add_geometry(shape)
    return base_scene

def export_scene(scene, filename, output_folder="output"):
    """
    Go into the output folder. If it's not exists create one.
    Then export the scene to the output folder with the correspounding filename
    The filename is going to be created uniquely when the entry code is executed.
    """

    Path(output_folder).mkdir(exist_ok=True)
    scene.export(f"{output_folder}/{filename}.obj")



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
        self.output_folder = output_folder
    
    def process_all_scenes(self):
        """
        Process all configuration files and export scenes.
        """
        config_files = self.config_processor.get_config_files()
        
        for config_file in config_files:
            config = self.config_processor.load_config(config_file)
            base_scene = load_scene(self.base_map)
            
            scene = build_scene(config, self.shape_factory, base_scene)
            export_scene(scene, config_file.stem, self.output_folder)



def run_scene_builder(config_folder="./configs", base_map="./map.obj", output_folder="./maps"):
    """Entry point function."""
    manager = SceneManager(config_folder, base_map, output_folder)
    manager.process_all_scenes()
