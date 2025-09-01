"""
writer.py

All of the file config writing operations are handled here.
"""

import yaml
from pathlib import Path
from models.domain import MapConfig

CONFIGS_DIR = Path("./configs")
CONFIGS_DIR.mkdir(parents=True, exist_ok=True)  # Ensure it exists

def write_config(map_name: str, map_config: MapConfig):
    """
    Writing generated augmentation configs to the configs directory.
    """
    safe_name = Path(map_name).stem  # Remove extension if any
    output_path = CONFIGS_DIR / f"{safe_name}.yaml"

    with open(output_path, "w", encoding="utf-8") as f:
        yaml.dump(map_config.model_dump(), f, sort_keys=False)
