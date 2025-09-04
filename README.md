#  MapGen - Map Generation Tool Using Unity & Python

**MapGen** is a tool developed with Unity and Python (FastAPI) that allows users to generate configurations and maps from 3D models. The main goal is to augment scenes with map data through an easy-to-use interface.

---

##  Purpose

This project enables users to:

- Work with `.fbx`, `.obj`, and `.yaml` files
- Generate map data based on input models and configuration files
- Automate the process through a user-friendly Unity UI

---

##  Unity Editor Interface

- Accessible from the Unity menu:  **`Tools > MapGen`**
- Install the required Unity packages via Package Manager:
  
  - Unity UI (for Event System)
  - Unity Test Framework (for running tests)
  - Editor Coroutines (for coroutine support in the editor)
  - [NativeWebSocket](https://github.com/endel/NativeWebSocket) (real-time Unity-backend communication.)

---
## Setup Instructions
- Clone or Download the Project
  - Or run backend with Docker (pull from Docker Hub):
    ```bash
    docker pull sule194/mapgen-backend:latest
    docker run -p 8000:8000 sule194/mapgen-backend:latest
- Install Dependencies
  ```bash
  pip install -r requirements.txt
- Start the Backend Server
   ```bash
  uvicorn app:app --reload
   ```
  - Or run it in Docker (optional):
    ```bash
    docker build -t mapgen-backend .
    docker run -p 8000:8000 mapgen-backend
- Open the Unity Project
- Make sure required packages are installed in Unity.
- Copy the `com.sule.tool` folder into your Unity project’s Packages directory.
- Ready to use.

---
### 1.  Input Fields for Models and Configs

Users can add 3D model and config files in three different ways:
- By selecting a file via File Explorer
- By entering the file path manually
- By dragging and dropping files into the Unity UI

> **Supported model formats**: `.fbx`, `.obj`  
> **Supported config formats**: `.yaml`, `.yml`

### 2.  Buttons and Actions

| Button            | Function |
|-------------------|----------|
| **Clear Uploads**   | Deletes all uploaded model and config files from the `/uploads` folder (except `info.txt`). |
| **Create Configs** | Generates configuration files. |
| **Show Configs**   | Opens the folder containing the configs in the system file explorer. |
| **Clear Configs**   |  Deletes all generated config files from the `/configs` folder (except `info.txt`). |
| **Create Maps**    | Generates map files based on configs and base map. |
| **Show Maps**      | Opens the generated maps in the file explorer. Users can drag them into the Unity scene. |
| **Clear Maps**   | Deletes all generated map files from the `/maps` folder (except `info.txt`). |

---

##  Conversion Logic

- `.fbx` files are converted **manually** into `.obj` and `.mtl` inside Unity via a custom C# class.
- No external tools such as Blender or Assimp are used.

---

##  Python Backend (FastAPI)

The backend is built with FastAPI to receive HTTP requests from Unity and generate content.

### API Endpoints

| Method   | Endpoint                   | Description                                  |
|----------|----------------------------|----------------------------------------------|
| `POST`   | `/create_configs`          | Generates config files from uploaded files.  |
| `POST`   | `/upload_model_config`     | Uploads `.obj`, `.mtl`, and `.yaml` files.   |
| `POST`   | `/create_maps`             | Creates maps from configs and base model.    |
| `GET`    | `/configs/list`            | Lists config files.                          |
| `GET`    | `/configs/file/{filename}` | Downloads config file.            |
| `GET`    | `/maps/list`               | Lists generated maps.                        |
| `GET`    | `/maps/file/{filename}`    | Downloads map file.               |
| `GET`    | `/start_progress/{task_id}`| Checks progress of map generation.           |
| `WS`     | `/ws/progress/{task_id}`   | Sends real-time progress updates.          |
| `DELETE` | `/clear_configs`           | Clears all config files.                     |
| `DELETE` | `/clear_maps`              | Clears all map files.                        |
| `DELETE` | `/clear_uploads`           | Clears all uploaded files.                   |

---

## Configuration Overview

**map_count**  
Number of unique scenes to generate per run.

**output_type**  
Output format of the generated 3D scenes. (.glb, .obj, .stl)

**augmentations**  
A list of scene modifications applied to each map.

### Augmentation Details

**1. Basic Model Addition**

```
type: add_model
model: cube
scale: 0.3
count: 10
position: random
color: [0,0,255]
```

- This configuration adds 10 randomly placed blue cubes to the screen that is scaled 0.3.
- Available basic models: cube, sphere, cylinder, cone
- Additionally position can be given as [x,y,z] and it places object at that position.

**2. Custom Model Addition**

```
type: add_model
model: custom
custom_path: "./rock.glb"
scale: 0.3
count: 10
position: random
```

- Generates 10 random rock models that is scaled 0.3 to the screen.
- It can take glb, obj and dae files as input.
- position can be given as [x,y,z] still.

**3. Landscape**

```
type: landscape
position: random
count: 3
radius: random
smoothness: random
```

- Generates 3 randomly positioned landscapes.
- position can be fixed again.

---

## Running Tests with Unity Test Framework

This project uses the **Unity Test Framework** for automated testing. Follow these steps to run tests inside the Unity Editor.

### How to Run Tests Locally

1. **Open Your Project in Unity Editor**  
   Launch Unity Hub and open this project.

2. **Open the Test Runner Window**  
   Go to the menu:  
   `Window` → `General` → `Test Runner`  
   This will open the Unity Test Runner panel.

3. **Choose Test Mode**  
   In the Test Runner, select **Edit Mode** to run tests that don’t require Play Mode.

4. **Run Tests**  
   Click the **Run All** button to execute all tests.  
   The test results will appear in the Test Runner window, showing which tests passed or failed.

---

## Running Tests with pytest

This project uses `pytest` as the testing framework. Follow the steps below to run the tests locally.

### How to Run Tests Locally

1. **Open Terminal**

2. **Run all tests by typing:**
   ```bash
   pytest
   ```
   Check the test results in the terminal output.

---

##  Docker
- The backend service is containerized using Docker for easy deployment and scalability.
- You can find the Docker image on Docker Hub: [mapgen-backend](https://hub.docker.com/r/sule194/mapgen-backend)

To pull the image, run:

```bash
docker pull sule194/mapgen-backend:latest

