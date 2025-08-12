from pydantic import BaseModel, Field
from typing import Optional


class ConfigInput(BaseModel):
    obj_path: str
    mtl_path: str
    config_path: str



class CreatorInput(BaseModel):
    base_map: Optional[str] = None