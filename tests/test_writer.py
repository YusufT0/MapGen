import sys
import os

# Add the parent directory (MapGen) to PYTHONPATH
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '..')))

import yaml
import pytest
from models.domain import MapConfig
import core.writer as writer

def test_write_config(tmp_path, monkeypatch):
    """
    Test that write_config writes the MapConfig YAML file correctly
    to the patched configs directory inside the temporary test path.
    """

    # Create a temporary 'configs' directory inside tmp_path
    configs_dir = tmp_path / "configs"
    configs_dir.mkdir()

    # Patch writer.CONFIGS_DIR to this temp directory
    monkeypatch.setattr(writer, "CONFIGS_DIR", configs_dir)

    dummy_config = MapConfig(
        map="test_map",
        size=50,
        objects=[],
        landscapes=[]
    )

    # Call write_config with a simple map_name (no extension)
    writer.write_config("test_map", dummy_config)

    output_file = configs_dir / "test_map.yaml"

    # Assert the file was created
    assert output_file.exists()

    # Load YAML content back and compare with original dummy config dict
    with open(output_file, encoding="utf-8") as f:
        data = yaml.safe_load(f)

    assert data == dummy_config.model_dump()
