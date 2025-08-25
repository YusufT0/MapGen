using UnityEngine;
using System;

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

public class ProgressData
{
    public float progress;
}
