import yaml
import json
import jsonschema
from jsonschema import validate

def validate_config(config_path="config.yaml", schema_path="./validation/schema.json") -> bool:
    try:
        with open(config_path, "r") as f:
            data = yaml.safe_load(f)

        with open(schema_path, "r") as f:
            schema = json.load(f)

        validate(instance=data, schema=schema)
        print("config file controls are done.")
        return True

    except jsonschema.exceptions.ValidationError as err:
        print("config file is corrupted:")
        print(f"Error: {err.message}")
        if err.absolute_path:
            print(f"{'.'.join([str(i) for i in err.absolute_path])}")
        return False

    except Exception as e:
        print(f"Exceptional error: {str(e)}")
        return False
