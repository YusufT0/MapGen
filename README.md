# 🗺️ MapGen - Map Generation Tool Using Unity & Python

**MapGen** is a tool developed with Unity and Python (FastAPI) that allows users to generate configurations and maps from 3D models. The main goal is to augment scenes with map data through an easy-to-use interface.

---

## 🎯 Purpose

This project enables users to:

- Work with `.fbx`, `.obj`, and `.yaml` files
- Generate map data based on input models and configuration files
- Automate the process through a user-friendly Unity UI

---

## 🧑‍💻 Unity Editor Interface

Accessible from the Unity menu:  
**`Tools > My Tool UI`**

### 1. 📂 Input Fields for Models and Configs

Users can add 3D model and config files in three different ways:
- By selecting a file via File Explorer
- By entering the file path manually
- By dragging and dropping files into the Unity UI

> **Supported model formats**: `.fbx`, `.obj`  
> **Supported config formats**: `.yaml`, `.yml`

### 2. 🔘 Buttons and Actions

| Button            | Function |
|-------------------|----------|
| **Create Configs** | Generates configuration files. |
| **Show Configs**   | Opens the folder containing the configs in the system file explorer. |
| **Create Maps**    | Generates map files based on configs and base map. |
| **Show Maps**      | Opens the generated maps in the file explorer. Users can drag them into the Unity scene. |

---

## 🔄 Conversion Logic

- `.fbx` files are converted **manually** into `.obj` and `.mtl` inside Unity via a custom C# class.
- No external tools such as Blender or Assimp are used.

---

## 🌐 Python Backend (FastAPI)

The backend is built with FastAPI to receive HTTP requests from Unity and generate content.

### 📡 API Endpoints

| Method | Endpoint            | Description                        |
|--------|---------------------|------------------------------------|
| `POST` | `/create_configs`   | Generates config files             |
| `POST` | `/create_maps`      | Creates maps from configs          |
| `GET`  | `/get_config_path`  | Returns the path to the configs folder |
| `GET`  | `/get_map_path`     | Returns the path to the maps folder |

---

## 📄 Configuration Files (.yaml)

The configuration files define how maps will be generated and augmented in the project. These YAML files contain settings for:

- The number of maps to generate (`map_count`)
- Output file format (`output_type`), e.g., "obj"
- A list of augmentations to apply on the maps (`augmentations`), which specify:
  - The type of augmentation (e.g., adding a custom model, generating landscape features)
  - Parameters for each augmentation, such as:
    - Model path and scale
    - Count of objects to add
    - Position (can be fixed or random)
    - Additional properties like smoothness and radius for landscapes
