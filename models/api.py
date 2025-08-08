from pydantic import BaseModel


class ConfigInput(BaseModel):
    obj_path: str
    mtl_path: str
    config_path: str



