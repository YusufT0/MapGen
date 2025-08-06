import yaml
import json

def write_config(map_name, map_config):
    with open(f"./configs/{map_name}.yaml", "w") as f:
        yaml.dump(map_config.model_dump(),f, sort_keys=False)