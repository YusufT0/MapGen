using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
using System.Diagnostics;
using System.IO;
using Unity.EditorCoroutines.Editor;
using UnityEngine.EventSystems;
using System.Collections.Generic;

internal class MyToolWindow : EditorWindow
{
    internal string modelPath = "";
    internal string objPath = "";
    internal string mtlPath = "";
    internal string configPath = "";

    string baseURL = "http://127.0.0.1:8000/";

    [MenuItem("Tools/My Tool UI")]
    public static void ShowWindow()
    {
        GetWindow<MyToolWindow>("MapGen");
    }

    private IWebRequestHandler requestHandler = new WebRequestHandler();

    internal void SetRequestHandler(IWebRequestHandler handler)
    {
        this.requestHandler = handler;
    }

    internal void OnGUI()
    {
        Event evt = Event.current;
        string tempModelPath = modelPath;

        GUILayout.Label("Input Fields", EditorStyles.boldLabel);

        // === MODEL DOSYASI ===
        GUILayout.BeginHorizontal();
        Rect modelRect = GUILayoutUtility.GetRect(new GUIContent("Model"), GUI.skin.textField);
        string newModelPath = EditorGUI.TextField(modelRect, "Model", modelPath);

        if (GUILayout.Button("Browse", GUILayout.MaxWidth(80)))
        {
            string path = EditorUtility.OpenFilePanel("Select FBX Model", "", "fbx,obj");
            if (!string.IsNullOrEmpty(path))
            {
                newModelPath = Path.GetFullPath(path); // Global path
                modelPath = newModelPath;
                ProcessModelPath(); // dönüşüm fonksiyonu
            }
        }
        GUILayout.EndHorizontal();

        if ((evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform) && modelRect.Contains(evt.mousePosition))
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (var path in DragAndDrop.paths)
                {
                    string ext = Path.GetExtension(path).ToLower();
                    if (ext == ".fbx" || ext == ".obj")
                    {
                        newModelPath = Path.GetFullPath(path); // Global path
                        modelPath = newModelPath;
                        ProcessModelPath(); // dönüşüm fonksiyonu
                        UnityEngine.Debug.Log("Model file dragged: " + path);
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning("Unsupported file for model: " + path);
                    }
                }
            }
            evt.Use();
        }

        // Eğer kullanıcı elle yazdıysa, sadece "Enter" tuşuna bastığında kabul et
        if (tempModelPath != modelPath && evt.isKey && evt.keyCode == KeyCode.Return)
        {
            modelPath = tempModelPath;
            ProcessModelPath();
        }

        // === CONFIG DOSYASI ===
        GUILayout.BeginHorizontal();
        Rect configRect = GUILayoutUtility.GetRect(new GUIContent("Config (.yaml)"), GUI.skin.textField);
        configPath = EditorGUI.TextField(configRect, "Config (.yaml)", configPath);

        if (GUILayout.Button("Browse", GUILayout.MaxWidth(80)))
        {
            string path = EditorUtility.OpenFilePanel("Select Config YAML", "", "yaml,yml");
            if (!string.IsNullOrEmpty(path)) configPath = path;
        }
        GUILayout.EndHorizontal();

        if ((evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform) && configRect.Contains(evt.mousePosition))
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (var path in DragAndDrop.paths)
                {
                    string ext = Path.GetExtension(path).ToLower();
                    if (ext == ".yaml" || ext == ".yml")
                    {
                        configPath = path;
                        UnityEngine.Debug.Log("Config file dragged: " + path);
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning("Unsupported file for config: " + path);
                    }
                }
            }
            evt.Use();
        }

        bool inputsFilled = !string.IsNullOrEmpty(modelPath)  && !string.IsNullOrEmpty(configPath) ;
  
        EditorGUI.BeginDisabledGroup(!inputsFilled);

        if (GUILayout.Button("Create Configs"))
        {
            UnityEngine.Debug.Log("Create Configs clicked");
            ConfigRequest postData = new ConfigRequest
            {
                obj_path = objPath,
                mtl_path = mtlPath,
                config_path = configPath
            };

            string json = JsonUtility.ToJson(postData);
            Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutineOwnerless(SendCreateConfigsRequest(json));
        }

        if (GUILayout.Button("Show Configs"))
        {
            UnityEngine.Debug.Log("Show Configs clicked");
            Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutineOwnerless(ShowConfigsCoroutine());
        }

        if (GUILayout.Button("Create Maps"))
        {
            UnityEngine.Debug.Log("Create Maps clicked");

            CreatorRequest sendData = new CreatorRequest
            {
                base_map = objPath
            };

            string json = JsonUtility.ToJson(sendData);
            Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutineOwnerless(SendCreateMapsRequest(json));
        }

        if (GUILayout.Button("Show Maps"))
        {
            UnityEngine.Debug.Log("Show Maps clicked");
            Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutineOwnerless(ShowMapsCoroutine());
        }

        EditorGUI.EndDisabledGroup();
    }

    internal virtual IEnumerator SendCreateConfigsRequest(string json)
    {
        string url = baseURL + "create_configs";

        using (UnityWebRequest www = UnityWebRequest.PostWwwForm(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            // HTTP isteği başarısızsa hata mesajı yaz
            if (www.result != UnityWebRequest.Result.Success)
            {
                UnityEngine.Debug.LogError("Create Configs HTTP Error: " + www.error);
            }
            else
            {
                UnityEngine.Debug.Log("Create Configs Response: " + www.downloadHandler.text);
            }
        }
    }

    internal virtual IEnumerator ShowConfigsCoroutine()
    {
        string endpoint = baseURL + "get_config_path";

        using (UnityWebRequest www = UnityWebRequest.Get(endpoint))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                UnityEngine.Debug.LogError("HTTP Error: " + www.error);
                yield break;
            }

            string jsonResponse = www.downloadHandler.text;
            UnityEngine.Debug.Log("JSON Response: " + jsonResponse);

            string path = ParsePathFromJson(jsonResponse);
            UnityEngine.Debug.Log("Parsed Path: " + path);

            bool exists = Directory.Exists(path);
            UnityEngine.Debug.Log("Directory.Exists: " + exists);

            if (!string.IsNullOrEmpty(path) && exists)
            {
                UnityEngine.Debug.Log("Calling OpenFolderInExplorer...");
                OpenFolderInExplorer(path);
            }
            else
            {
                UnityEngine.Debug.LogError("Invalid path or folder does not exist: " + path);
            }
        }
    }

    internal virtual IEnumerator SendCreateMapsRequest(string json)
    {
        string url = baseURL + "create_maps";

        var www = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            UnityEngine.Debug.LogError("Create Maps HTTP Error: " + www.error);
        }
        else
        {
            UnityEngine.Debug.Log("Create Maps Response: " + www.downloadHandler.text);
        }
    }

    internal virtual IEnumerator ShowMapsCoroutine()
    {
        string endpoint = baseURL + "get_map_path";

        using (UnityWebRequest www = UnityWebRequest.Get(endpoint))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                UnityEngine.Debug.LogError("HTTP Error: " + www.error);
                yield break;
            }

            string jsonResponse = www.downloadHandler.text;
            UnityEngine.Debug.Log("JSON Response: " + jsonResponse);

            string path = ParsePathFromJson(jsonResponse);
            UnityEngine.Debug.Log("Parsed Path: " + path);

            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                OpenFolderInExplorer(path);
            }
            else
            {
                UnityEngine.Debug.LogError("Invalid path or folder does not exist: " + path);
            }
        }
    }

    internal static string ParsePathFromJson(string json)
    {
        try
        {
            var parsed = JsonUtility.FromJson<PathResponse>(json);
            return parsed.path;
        }
        catch
        {
            UnityEngine.Debug.LogError("JSON could not be parsed: " + json);
            return null;
        }
    }

    [System.Serializable]
    internal class PathResponse
    {
        public string path;
    }

    internal virtual void OpenFolderInExplorer(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            UnityEngine.Debug.LogError("Path is null or empty");
            return;
        }

        try
        {
            string winPath = path.Replace("/", "\\");
            UnityEngine.Debug.Log("Trying to open folder: " + winPath);
            UnityEngine.Debug.Log("Folder exists? " + Directory.Exists(winPath));

            var psi = new ProcessStartInfo()
            {
                FileName = "explorer.exe",
                Arguments = $"\"{winPath}\"",
                UseShellExecute = true,
                Verb = "open"
            };

            Process.Start(psi);
            UnityEngine.Debug.Log("Explorer process started.");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("Explorer launch failed: " + ex.Message);
        }
    }

    void ProcessModelPath()
    {
        UnityEngine.Debug.Log("New model path selected: " + modelPath);

        if (modelPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
        {
            (objPath, mtlPath) = FBXtoOBJExporter.ConvertExternalFBX(modelPath);
            if (!string.IsNullOrEmpty(objPath))
                UnityEngine.Debug.Log("FBX conversion succeeded. OBJ: " + objPath + " | MTL: " + mtlPath);
            else
                UnityEngine.Debug.LogError("FBX conversion failed.");
        }
        else if (modelPath.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
        {
            objPath = modelPath;
            mtlPath = "";
            UnityEngine.Debug.Log(".obj selected. OBJ path: " + objPath);
        }
    }

}
