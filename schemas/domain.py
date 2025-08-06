from pydantic import BaseModel, Field
from typing import Union, Optional, Literal, List


class AugmentConfig(BaseModel):
    type: str
    model: str  
    custom_path: Optional[str] = None
    scale: float
    count: int
    position: Union[str, list[float]]
    color: list[int] = Field(default_factory=lambda: [0, 0, 255])  


class MainConfig(BaseModel):
    map: str
    map_count: int
    output_type: Literal["glb", "gltf", "obj"]
    augmentations: List[AugmentConfig]


class Landscape(BaseModel):
    position: list[float]
    radius: float
    shrink_vel: float

class ModelObject(BaseModel):
    model: str
    scale: float
    position: list[float]


class MapConfig(BaseModel):
    map: str
    objects: List[ModelObject]
    landscapes: List[Landscape]
