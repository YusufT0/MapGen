"""
writer.py

All of the file config writing operations is going to be here.

"""

import yaml
def write_config(map_name, map_config):
    """
    Writing generated augmentation configs.
    """
    with open(f"./configs/{map_name}.yaml", "w", encoding="utf-8") as f:
        yaml.dump(map_config.model_dump(),f, sort_keys=False)
