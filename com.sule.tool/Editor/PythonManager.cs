using NativeWebSocket;
using UnityEditor;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Diagnostics;
using System.IO;
using Unity.EditorCoroutines.Editor;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Threading.Tasks;


internal class MyToolWindow : EditorWindow
{
    internal string modelPath = "";
    internal string objPath = "";
    internal string mtlPath = "";
    internal string configPath = "";
    internal string base_map = "";

    internal string newModelPath = "";

    internal bool showAbsolutePathWarning = false;
    private bool showConfigFoldout;
    private bool showMapFoldout;

    string desktopPath;
    string configFolder;
    string mapFolder;

    string baseURL = "http://127.0.0.1:8000/";

    // for Progress bar 
    private bool showProgressBar = false;
    private float progress = 0f;
    private WebSocket webSocket;
    private string currentTaskId = "";

    [MenuItem("Tools/MapGen")]
    public static void ShowWindow()
    {
        GetWindow<MyToolWindow>("MapGen");
    }

    private IWebRequestHandler requestHandler = new WebRequestHandler();

    internal void SetRequestHandler(IWebRequestHandler handler)
    {
        this.requestHandler = handler;
    }

    private void OnEnable()
    {
        showConfigFoldout = false;
        showMapFoldout = false;

    }

    internal void OnGUI()
    {
        Event evt = Event.current;

        GUILayout.Label("Input Fields", EditorStyles.boldLabel);

        // === MODEL DOSYASI ===
        GUILayout.BeginHorizontal();
        Rect modelRect = GUILayoutUtility.GetRect(new GUIContent("Model"), GUI.skin.textField);
        GUI.SetNextControlName("Model");
        newModelPath = EditorGUI.TextField(modelRect, "Model", newModelPath);

        // Eğer kullanıcı elle yazdıysa, sadece "Enter" tuşuna bastığında kabul et
        if (evt.isKey && evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Return) 
        {
            string focused = GUI.GetNameOfFocusedControl();

            if (focused == "Model")
            {
                if (!Path.IsPathRooted(newModelPath))
                {
                    PopupMessageManager("Warning", "Please enter absolute path");
                }
                else
                {
                    UnityEngine.Debug.Log("ModelDONE");
                    modelPath = newModelPath;
                    ProcessModelPath();
                }
            }
            else if (focused == "Config (.yaml)")
            {
                if (!Path.IsPathRooted(configPath))
                {
                    PopupMessageManager("Warning", "Please enter absolute path");
                }
                else
                {
                    UnityEngine.Debug.Log("Config accepted: " + configPath);
                    if (!string.IsNullOrEmpty(objPath) && File.Exists(objPath))
                    {
                        EditorCoroutineUtility.StartCoroutineOwnerless(UploadModelAndConfig());
                    }
                }
            }


        }

        void PopupMessageManager(string title, string message) 
        {
            showAbsolutePathWarning = true;

            if (showAbsolutePathWarning)
            {
                  showAbsolutePathWarning = false; // sadece 1 kez çalışsın
                  EditorApplication.delayCall += () =>
                  {
                  EditorUtility.DisplayDialog(title, message, "OK");
                  };
            }
        }

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
                        UnityEngine.Debug.Log("Model file dragged: " + path);
                        newModelPath = Path.GetFullPath(path); // Global path
                        modelPath = newModelPath;
                        ProcessModelPath(); // dönüşüm fonksiyonu   
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning("Unsupported file for model: " + path);
                        PopupMessageManager("Warning", "Unsupported file for model");
                    }
                }
            }
            evt.Use();
        }

        // === CONFIG DOSYASI ===
        GUILayout.BeginHorizontal();
        Rect configRect = GUILayoutUtility.GetRect(new GUIContent("Config (.yaml)"), GUI.skin.textField);
        GUI.SetNextControlName("Config (.yaml)");
        configPath = EditorGUI.TextField(configRect, "Config (.yaml)", configPath);

        if (GUILayout.Button("Browse", GUILayout.MaxWidth(80)))
        {
            string path = EditorUtility.OpenFilePanel("Select Config YAML", "", "yaml,yml");
            if (!string.IsNullOrEmpty(path))
            {
                configPath = path;
            }
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
                        PopupMessageManager("Warning", "Unsupported file for config");
                    }
                }
            }
            evt.Use();
        }

        bool inputsFilled = !string.IsNullOrEmpty(modelPath) && System.IO.Path.IsPathRooted(modelPath) &&
        !string.IsNullOrEmpty(configPath) && System.IO.Path.IsPathRooted(configPath); ;
  
        EditorGUI.BeginDisabledGroup(!inputsFilled);

        if (GUILayout.Button("Clear Uploads"))
        {
            UnityEngine.Debug.Log("Clear Uploads clicked");
            EditorCoroutineUtility.StartCoroutineOwnerless(ClearDirectoryCoroutine("clear_uploads"));
        }

        showConfigFoldout = EditorGUILayout.Foldout(showConfigFoldout, "Config Management", true);
        if (showConfigFoldout)
        {
            EditorGUI.indentLevel++;

            if (GUILayout.Button("Create Configs"))
            {
                UnityEngine.Debug.Log("Create Configs clicked");
                ConfigRequest postData = new ConfigRequest
                {
                    obj_path = Path.GetFileName(objPath),
                    config_path = Path.GetFileName(configPath),
                    mtl_path = string.IsNullOrEmpty(mtlPath) ? null : Path.GetFileName(mtlPath)
                };

                string json = JsonUtility.ToJson(postData);
                EditorCoroutineUtility.StartCoroutineOwnerless(SendCreateConfigsRequest(json));
            }

            if (GUILayout.Button("Show Configs"))
            {
                UnityEngine.Debug.Log("Show Configs clicked");
                EditorCoroutineUtility.StartCoroutineOwnerless(ShowConfigsCoroutine());
            }

            if (GUILayout.Button("Clear Configs"))
            {
                UnityEngine.Debug.Log("Clear Configs clicked");
                EditorCoroutineUtility.StartCoroutineOwnerless(ClearDirectoryCoroutine("clear_configs"));
                ClearFolderandDelete(configFolder);
            }

            EditorGUI.indentLevel--;
        }


        // === MAP FOLDOUT ===
        showMapFoldout = EditorGUILayout.Foldout(showMapFoldout, "Map Management", true);
        if (showMapFoldout)
        {
            EditorGUI.indentLevel++;

            if (GUILayout.Button("Create Maps"))
            {
                UnityEngine.Debug.Log("Create Maps clicked");
                EditorCoroutineUtility.StartCoroutineOwnerless(CreateMapsAndListenProgress());
            }

            if (GUILayout.Button("Show Maps"))
            {
                UnityEngine.Debug.Log("Show Maps clicked");
                EditorCoroutineUtility.StartCoroutineOwnerless(ShowMapsCoroutine());
            }

            if (GUILayout.Button("Clear Maps"))
            {
                UnityEngine.Debug.Log("Clear Maps clicked");
                EditorCoroutineUtility.StartCoroutineOwnerless(ClearDirectoryCoroutine("clear_maps"));
                ClearFolderandDelete(mapFolder);
            }

            EditorGUI.indentLevel--;
        }


        if (webSocket != null && webSocket.State == WebSocketState.Open)
        {
            try
            {
                webSocket.DispatchMessageQueue();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("Exception during DispatchMessageQueue: " + ex.Message);
            }
        }

        if (showProgressBar)
        {
            GUILayout.Space(10);
            GUILayout.Label($"Map generation progress: {Mathf.RoundToInt(progress * 100)}%", EditorStyles.boldLabel);
            Rect rect = GUILayoutUtility.GetRect(18, 18, "TextField");
            EditorGUI.ProgressBar(rect, progress, "Progress");
            Repaint();
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
                UnityEngine.Debug.LogError($"Error {www.responseCode}: {www.downloadHandler.text}");
            }
            else
            {
                UnityEngine.Debug.Log("Create Configs Response: " + www.downloadHandler.text);
            }
        }
    }

    internal IEnumerator ShowConfigsCoroutine()
    {
        desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        configFolder = System.IO.Path.Combine(desktopPath, "ConfigsFolder");

        if (!Directory.Exists(configFolder))
        {
            Directory.CreateDirectory(configFolder);
        }

        UnityWebRequest listRequest = UnityWebRequest.Get(baseURL + "configs/list");
        yield return listRequest.SendWebRequest();

        if (listRequest.result != UnityWebRequest.Result.Success)
        {
            UnityEngine.Debug.LogError("Failed to get config list: " + listRequest.error);
            yield break;
        }

        ConfigList files = JsonUtility.FromJson<ConfigList>(listRequest.downloadHandler.text);

        foreach (string file in files.files)
        {
            string fileUrl = baseURL + $"configs/file/{file}";
            UnityWebRequest fileRequest = UnityWebRequest.Get(fileUrl);
            yield return fileRequest.SendWebRequest();

            if (fileRequest.result == UnityWebRequest.Result.Success)
            {
                string savePath = Path.Combine(configFolder, file);
                File.WriteAllBytes(savePath, fileRequest.downloadHandler.data);
            }
            else
            {
                UnityEngine.Debug.LogError($"Failed to download {file}: {fileRequest.error}");
            }
        }

        OpenFolderInExplorer(configFolder);
    }



    private IEnumerator ClearDirectoryCoroutine(string endpoint)
    {
        string url = baseURL + endpoint;

        using (UnityWebRequest www = UnityWebRequest.Delete(url))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                UnityEngine.Debug.LogError("Clear failed: " + www.error);
            }
            else
            {
                UnityEngine.Debug.Log($"{endpoint} cleared successfully.");
            }
        }
    }


    internal IEnumerator CreateMapsAndListenProgress()
    {
        showProgressBar = true;
        progress = 0f;

        string startProgressUrl = baseURL + $"start_progress/{Guid.NewGuid()}";
        using (UnityWebRequest www = UnityWebRequest.Get(startProgressUrl))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                UnityEngine.Debug.LogError("Failed to start progress: " + www.error);
                showProgressBar = false;
                yield break;
            }
            else
            {
                UnityEngine.Debug.Log("Started progress with task_id placeholder.");
            }
        }

        CreatorRequest sendData = new CreatorRequest
        {
            base_map = objPath.Replace("\\", "/")
        };

        string json = JsonUtility.ToJson(sendData);

        using (UnityWebRequest www = new UnityWebRequest(baseURL + "create_maps", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                UnityEngine.Debug.LogError($"Error {www.responseCode}: {www.downloadHandler.text}");
                showProgressBar = false;
                yield break;
            }
            else
            {
                UnityEngine.Debug.Log("Create Maps Response: " + www.downloadHandler.text);

                CreateMapsResponse responseObj = JsonUtility.FromJson<CreateMapsResponse>(www.downloadHandler.text);
                if (responseObj == null || string.IsNullOrEmpty(responseObj.task_id))
                {
                    UnityEngine.Debug.LogError("Task ID alınamadı!");
                    showProgressBar = false;
                    yield break;
                }
                currentTaskId = responseObj.task_id;
            }
        }

        yield return WaitForTask(ListenProgressWebSocket(currentTaskId));
    }



    internal IEnumerator ShowMapsCoroutine()
    {
        string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        mapFolder = Path.Combine(desktopPath, "MapsFolder");

        if (!Directory.Exists(mapFolder))
            Directory.CreateDirectory(mapFolder);

        UnityWebRequest listRequest = UnityWebRequest.Get(baseURL + "maps/list");
        yield return listRequest.SendWebRequest();

        if (listRequest.result != UnityWebRequest.Result.Success)
        {
            UnityEngine.Debug.LogError("Failed to get map list: " + listRequest.error);
            yield break;
        }

        ConfigList files = JsonUtility.FromJson<ConfigList>(listRequest.downloadHandler.text);

        foreach (string file in files.files)
        {
            string fileUrl = baseURL + $"maps/file/{file}";
            UnityWebRequest fileRequest = UnityWebRequest.Get(fileUrl);
            yield return fileRequest.SendWebRequest();

            if (fileRequest.result == UnityWebRequest.Result.Success)
            {
                string savePath = Path.Combine(mapFolder, file);
                File.WriteAllBytes(savePath, fileRequest.downloadHandler.data);
            }
            else
            {
                UnityEngine.Debug.LogError($"Failed to download {file}: {fileRequest.error}");
            }
        }

        OpenFolderInExplorer(mapFolder);
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

    internal virtual void OpenFolderInExplorer(string path)
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    System.Diagnostics.Process.Start("explorer.exe", path.Replace("/", "\\"));
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    System.Diagnostics.Process.Start("open", path);
#endif
    }


    void ProcessModelPath()
    {
        if (modelPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
        {
            (objPath, mtlPath) = FBXtoOBJExporter.ConvertExternalFBX(modelPath);
            if (!string.IsNullOrEmpty(objPath))
            {
                UnityEngine.Debug.Log("FBX conversion succeeded. OBJ: " + objPath);
            }
            else
            {
                UnityEngine.Debug.LogError("FBX conversion failed.");
                return;
            }
        }
        else if (modelPath.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
        {
            objPath = modelPath;
            mtlPath = "";
            UnityEngine.Debug.Log("OBJ selected: " + objPath);
        }

        //Eğer config de varsa upload başlasın
        if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
        {
            EditorCoroutineUtility.StartCoroutineOwnerless(UploadModelAndConfig());
        }
    }


    private async Task ListenProgressWebSocket(string taskId)
    {
        if (string.IsNullOrEmpty(taskId))
        {
            UnityEngine.Debug.LogError("Task ID is null or empty. Aborting WebSocket connection.");
            return;
        }

        string wsUrl = $"ws://127.0.0.1:8000/ws/progress/{taskId}";
        UnityEngine.Debug.Log($"Attempting WebSocket connection to: {wsUrl}");

        webSocket = new WebSocket(wsUrl);

        webSocket.OnOpen += () =>
        {
            UnityEngine.Debug.Log("WebSocket connection opened.");
        };

        webSocket.OnClose += (e) =>
        {
            UnityEngine.Debug.Log("WebSocket connection closed.");
            showProgressBar = false;
        };


        webSocket.OnError += (e) =>
        {
            showProgressBar = false;
        };


        webSocket.OnMessage += async (bytes) =>
        {
            string message = System.Text.Encoding.UTF8.GetString(bytes);

            if (string.IsNullOrWhiteSpace(message)) return;

            var msg = JsonUtility.FromJson<ProgressMessage>(message);
            if (msg == null)
            {
                UnityEngine.Debug.LogError("JSON parse failed. Raw: " + message);
                return;
            }

            progress = Mathf.Clamp01(msg.progress);

            if (progress >= 1f)
            {
                UnityEngine.Debug.Log("Progress complete.");
                showProgressBar = false;

                await CloseWebSocketSafely();

            }
        };


        try
        {
            await webSocket.Connect();
            UnityEngine.Debug.Log("WebSocket successfully connected.");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("WebSocket connect exception: " + ex.Message);
            showProgressBar = false;
            return;
        }


        while (webSocket != null && webSocket.State == WebSocketState.Open)
        {
            webSocket.DispatchMessageQueue();
            await Task.Delay(100);
        }

        webSocket = null;
        UnityEngine.Debug.Log("WebSocket closed and cleaned up.");

    }



    private IEnumerator WaitForTask(Task task)
    {
        while (!task.IsCompleted)
            yield return null;

        if (task.IsFaulted)
            UnityEngine.Debug.LogError(task.Exception);
    }

    private async Task CloseWebSocketSafely()
    {
        if (webSocket != null && webSocket.State == WebSocketState.Open)
        {
            try
            {
                await webSocket.Close();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("Exception during WebSocket close: " + ex.Message);
            }
        }
    }

    internal IEnumerator UploadModelAndConfig()
    {
        if (string.IsNullOrEmpty(objPath) || !File.Exists(objPath) ||
            string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
        {
            UnityEngine.Debug.LogWarning("Upload skipped: OBJ or Config path invalid.");
            yield break;
        }

        WWWForm form = new WWWForm();

        // Add .obj
        byte[] objData = File.ReadAllBytes(objPath);
        form.AddBinaryData("obj_file", objData, Path.GetFileName(objPath));

        // Add .mtl if exists
        if (!string.IsNullOrEmpty(mtlPath) && File.Exists(mtlPath))
        {
            byte[] mtlData = File.ReadAllBytes(mtlPath);
            form.AddBinaryData("mtl_file", mtlData, Path.GetFileName(mtlPath));
        }

        // Add config
        byte[] configData = File.ReadAllBytes(configPath);
        form.AddBinaryData("config_file", configData, Path.GetFileName(configPath));

        using (UnityWebRequest www = UnityWebRequest.Post(baseURL + "upload_model_config", form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                UnityEngine.Debug.LogError("Upload failed: " + www.error);
            }
            else
            {
                UnityEngine.Debug.Log("Files uploaded successfully: " + www.downloadHandler.text);
            }
        }
    }

    IEnumerator DownloadConfigs()
    {
        string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        string configsFolder = System.IO.Path.Combine(desktopPath, "ConfigsFolder");
        if (!System.IO.Directory.Exists(configsFolder))
            System.IO.Directory.CreateDirectory(configsFolder);

        // Dosya listesini al
        UnityWebRequest www = UnityWebRequest.Get( baseURL + "/configs/list");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            UnityEngine.Debug.LogError("Config list error: " + www.error);
            yield break;
        }

        var json = www.downloadHandler.text;
        var files = JsonUtility.FromJson<ConfigList>(json);

        foreach (string file in files.files)
        {
            string fileUrl = baseURL + $"/configs/file/{file}";
            UnityWebRequest fileReq = UnityWebRequest.Get(fileUrl);
            yield return fileReq.SendWebRequest();

            if (fileReq.result == UnityWebRequest.Result.Success)
            {
                string savePath = System.IO.Path.Combine(configsFolder, file);
                System.IO.File.WriteAllBytes(savePath, fileReq.downloadHandler.data);
                UnityEngine.Debug.Log($"Saved config {file}");
            }
            else
            {
                UnityEngine.Debug.LogError($"Failed to download {file}: {fileReq.error}");
            }
        }
    }

    internal void ClearFolderandDelete(string folderPath)
    {
        try
        {
            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, true); // 'true' recursive silme için
                UnityEngine.Debug.Log($"Deleted folder and contents: {folderPath}");
            }
            else
            {
                UnityEngine.Debug.LogWarning($"Folder does not exist: {folderPath}");
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Error deleting folder {folderPath}: {e.Message}");
        }
    }


}
