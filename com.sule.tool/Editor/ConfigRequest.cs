using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class ConfigRequest
{
    public string obj_path;
    public string mtl_path;
    public string config_path;
}

public class CreatorRequest
{
    public string base_map;
}

[System.Serializable]
internal class PathResponse
{
    public string path;
}

[Serializable]
public class ProgressMessage
{
    public float progress;
}

[Serializable]
public class ConfigList
{
    public List<string> files;
}

[Serializable]
public class CreateMapsResponse
{
    public string status;
    public string message;
    public string task_id;
}
