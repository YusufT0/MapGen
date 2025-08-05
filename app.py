from fastapi import FastAPI, File, UploadFile, Form
from fastapi.responses import JSONResponse
import shutil
import os
import yaml

app = FastAPI()


@app.post("/create_configs")
async def create_configs():
    pass
@app.post("/create_maps")
async def create_maps():
    pass
@app.post("/get_map_path")
async def get_map_path():
    pass
@app.post("/get_config_path")
async def get_config_path():
    pass
