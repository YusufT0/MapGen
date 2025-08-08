import numpy as np
import trimesh
from pathlib import Path
from abc import ABC, abstractmethod
from core.reader import load_map_config, load_scene
from trimesh.transformations import translation_matrix, scale_matrix


class ShapeCreator(ABC):
    """Abstract base class for shape creation."""
    
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
    """Creates shapes from custom model files."""
    
    def create_shape(self, obj_conf) -> trimesh.Trimesh:
        # Load custom model 
        mesh = trimesh.load(obj_conf.model_path)
        
        # Only apply scale and position for custom models (they have their own materials)
        mesh.apply_transform(scale_matrix(obj_conf.scale))
        mesh.apply_transform(translation_matrix(obj_conf.position))
        
        return mesh
    
    def _apply_transformations(self, mesh, obj_conf):

        mesh.apply_transform(scale_matrix(obj_conf.scale))
        mesh.apply_transform(translation_matrix(obj_conf.position))
        
        return mesh

class ShapeFactory:
    """Factory for creating shapes based on model type."""
    
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


class SceneBuilder:
    """Builds 3D scenes from configuration files."""
    
    def __init__(self, shape_factory: ShapeFactory):
        self.shape_factory = shape_factory
    
    def build_scene(self, config, base_scene: trimesh.Scene) -> trimesh.Scene:
        """Build a scene by adding objects to the base scene."""
        for obj in config.objects:
            shape = self.shape_factory.create_shape(obj)
            base_scene.add_geometry(shape)
        return base_scene


class SceneExporter:
    """Handles exporting scenes to files."""
    
    def __init__(self, output_folder: str):
        self.output_path = Path(output_folder)
        self.output_path.mkdir(exist_ok=True)
    
    def export_scene(self, scene: trimesh.Scene, filename: str):
        """Export a scene to a file."""
        output_file = self.output_path / f"{filename}.obj"
        scene.export(str(output_file))


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
    """Main orchestrator class that coordinates all components."""
    
    def __init__(self, config_folder="./configs", base_map="./map.obj", output_folder="./maps"):
        self.base_map = base_map
        self.config_processor = ConfigProcessor(config_folder)
        self.scene_builder = SceneBuilder(ShapeFactory())
        self.scene_exporter = SceneExporter(output_folder)
    
    def process_all_scenes(self):
        """Process all configuration files and export scenes."""
        config_files = self.config_processor.get_config_files()
        
        for config_file in config_files:
            config = self.config_processor.load_config(config_file)
            base_scene = load_scene(self.base_map)
            
            scene = self.scene_builder.build_scene(config, base_scene)
            self.scene_exporter.export_scene(scene, config_file.stem)


def run_scene_builder(config_folder="./configs", base_map="./map.obj", output_folder="./maps"):
    """Entry point function."""
    manager = SceneManager(config_folder, base_map, output_folder)
    manager.process_all_scenes()
