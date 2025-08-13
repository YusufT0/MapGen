from pydantic import BaseModel, Field
from typing import Union, Optional, Literal, List



class BaseAugmentConfig(BaseModel):
    type: str

class AddModelConfig(BaseAugmentConfig):
    type: Literal["add_model"]
    model: str
    custom_path: Optional[str] = None
    scale: float
    count: int
    position: Union[str, list[float]]
    color: list[int] = Field(default_factory=lambda: [0, 0, 255])

class LandscapeConfig(BaseAugmentConfig):
    type: Literal["landscape"]
    position: Union[str, list[float]]
    smoothness: Union[str, float]
    count: int
    radius: Union[str, float]


class MainConfig(BaseModel):
    map_count: int
    output_type: Literal["glb", "gltf", "obj"]
    augmentations: List[Union[AddModelConfig, LandscapeConfig]]


class LandscapeModel(BaseModel):
    position: list[float]
    radius: float
    smoothness: float

class ModelObject(BaseModel):
    model: str
    scale: float
    position: list[float]

class StandardModelObject(ModelObject):
    color: list[float]

class CustomModelObject(ModelObject):
    model_path: str

class MapConfig(BaseModel):
    map: str
    objects: List[Union[StandardModelObject, CustomModelObject]]
    landscapes: List[LandscapeModel]