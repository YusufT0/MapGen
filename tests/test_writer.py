import yaml
import os
from models.domain import MapConfig
import core.writer as writer

def test_write_config(tmp_path, monkeypatch):
    
    configs_dir = tmp_path / "configs"
    configs_dir.mkdir()

    monkeypatch.chdir(tmp_path)

    dummy_config = MapConfig(
        map="test_map",
        size=50,
        objects=[],
        landscapes=[]
    )

    writer.write_config("test_map", dummy_config)

    output_file = configs_dir / "test_map.yaml"
    assert output_file.exists()

    with open(output_file, encoding="utf-8") as f:
        data = yaml.safe_load(f)
    
    assert data == dummy_config.model_dump()
