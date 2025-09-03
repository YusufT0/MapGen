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
    internal bool showConfigFoldout;
    internal bool showMapFoldout;

    internal string desktopPath;
    internal string configFolder;
    internal string mapFolder;

    internal string baseURL = "http://127.0.0.1:8000/";

    // for Progress bar 
    internal bool showProgressBar = false;
    internal float progress = 0f;
    internal WebSocket webSocket;
    internal string currentTaskId = "";

    [MenuItem("Tools/MapGen")]
    public static void ShowWindow()
    {
        GetWindow<MyToolWindow>("MapGen");
    }

    protected IWebRequestHandler requestHandler = new WebRequestHandler();

    internal void SetRequestHandler(IWebRequestHandler handler)
    {
        this.requestHandler = handler;
    }

    internal void OnEnable()
    {
        showConfigFoldout = false;
        showMapFoldout = false;

    }

    internal void OnGUI()
    {
        Event evt = Event.current;

        GUILayout.Label("Input Fields", EditorStyles.boldLabel);

        // === MODEL KLASÖRÜ (Eski Model dosyası) ===
        GUILayout.BeginHorizontal();
        Rect modelRect = GUILayoutUtility.GetRect(new GUIContent("Model Folder"), GUI.skin.textField);
        GUI.SetNextControlName("Model Folder");
        newModelPath = EditorGUI.TextField(modelRect, "Model Folder", newModelPath);

        // Eğer kullanıcı elle yazdıysa, sadece "Enter" tuşuna bastığında kabul et
        if (evt.isKey && evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Return)
        {
            string focused = GUI.GetNameOfFocusedControl();

            if (focused == "Model Folder")
            {
                if (!Path.IsPathRooted(newModelPath))
                {
                    PopupMessageManager("Warning", "Please enter absolute path");
                }
                else if (!Directory.Exists(newModelPath))
                {
                    PopupMessageManager("Warning", "Directory does not exist");
                }
                else
                {
                    UnityEngine.Debug.Log("Model Folder DONE");
                    modelPath = newModelPath;
                    ProcessModelFolder(); // Değişti: ProcessModelPath yerine ProcessModelFolder
                }
            }
            else if (focused == "Config (.yaml)")
            {
                if (!Path.IsPathRooted(configPath))
                {
                    PopupMessageManager("Warning", "Please enter absolute path");
                }
                else if (!File.Exists(configPath))
                {
                    PopupMessageManager("Warning", "File does not exist");
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
                showAbsolutePathWarning = false;
                EditorApplication.delayCall += () =>
                {
                    EditorUtility.DisplayDialog(title, message, "OK");
                };
            }
        }

        if (GUILayout.Button("Browse", GUILayout.MaxWidth(80)))
        {
            // File yerine Folder seçimi
            string path = EditorUtility.OpenFolderPanel("Select Model Folder", "", "");
            if (!string.IsNullOrEmpty(path))
            {
                newModelPath = Path.GetFullPath(path);
                modelPath = newModelPath;
                ProcessModelFolder(); // Değişti: ProcessModelPath yerine ProcessModelFolder
            }
        }
        GUILayout.EndHorizontal();

        // Model alanı için drag drop (klasör desteği ile)
        if (modelRect.Contains(evt.mousePosition))
        {
            if (evt.type == EventType.DragUpdated)
            {
                bool hasValidFolder = false;
                foreach (var path in DragAndDrop.paths)
                {
                    if (Directory.Exists(path))
                    {
                        hasValidFolder = true;
                        break;
                    }
                }
                DragAndDrop.visualMode = hasValidFolder ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                evt.Use();
            }
            else if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (var path in DragAndDrop.paths)
                {
                    if (Directory.Exists(path))
                    {
                        UnityEngine.Debug.Log("Model folder dragged: " + path);
                        newModelPath = Path.GetFullPath(path);
                        modelPath = newModelPath;
                        ProcessModelFolder(); // Değişti: ProcessModelPath yerine ProcessModelFolder
                        break;
                    }
                }
                evt.Use();
            }
        }

        // === CONFIG DOSYASI (Aynı kalacak) ===
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
                // Eğer model zaten seçilmişse, otomatik upload başlat
                if (!string.IsNullOrEmpty(objPath) && File.Exists(objPath))
                {
                    EditorCoroutineUtility.StartCoroutineOwnerless(UploadModelAndConfig());
                }
            }
        }
        GUILayout.EndHorizontal();

        // Config alanı için drag drop (aynı kalacak)
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
                        // Eğer model zaten seçilmişse, otomatik upload başlat
                        if (!string.IsNullOrEmpty(objPath) && File.Exists(objPath))
                        {
                            EditorCoroutineUtility.StartCoroutineOwnerless(UploadModelAndConfig());
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning("Unsupported file for config: " + path);
                        PopupMessageManager("Warning", "Unsupported file for config");
                    }
                }
                evt.Use();
            }
        }

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

    internal virtual IEnumerator ShowConfigsCoroutine()
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



    internal virtual IEnumerator ClearDirectoryCoroutine(string endpoint)
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


    internal virtual IEnumerator CreateMapsAndListenProgress()
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



    internal virtual IEnumerator ShowMapsCoroutine()
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


    internal void ProcessModelFolder()
    {
        // Klasördeki FBX dosyalarını bul
        string[] fbxFiles = Directory.GetFiles(modelPath, "*.fbx", SearchOption.TopDirectoryOnly);
        string[] objFiles = Directory.GetFiles(modelPath, "*.obj", SearchOption.TopDirectoryOnly);

        if (fbxFiles.Length > 0)
        {
            // İlk FBX dosyasını kullan
            string fbxPath = fbxFiles[0];
            (objPath, mtlPath) = FBXtoOBJExporter.ConvertExternalFBX(fbxPath);

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
        else if (objFiles.Length > 0)
        {
            // İlk OBJ dosyasını kullan
            objPath = objFiles[0];

            // İlgili MTL dosyasını bulmaya çalış
            string objNameWithoutExt = Path.GetFileNameWithoutExtension(objPath);
            string potentialMtlPath = Path.Combine(modelPath, objNameWithoutExt + ".mtl");
            mtlPath = File.Exists(potentialMtlPath) ? potentialMtlPath : "";

            UnityEngine.Debug.Log("OBJ selected: " + objPath);
            if (!string.IsNullOrEmpty(mtlPath))
            {
                UnityEngine.Debug.Log("MTL found: " + mtlPath);
            }
        }
        else
        {
            UnityEngine.Debug.LogError("No FBX or OBJ files found in the folder.");
            return;
        }

        // Eğer config de varsa upload başlasın
        if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
        {
            EditorCoroutineUtility.StartCoroutineOwnerless(UploadModelAndConfig());
        }
    }


    internal async Task ListenProgressWebSocket(string taskId)
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



    internal IEnumerator WaitForTask(Task task)
    {
        while (!task.IsCompleted)
            yield return null;

        if (task.IsFaulted)
            UnityEngine.Debug.LogError(task.Exception);
    }

    internal async Task CloseWebSocketSafely()
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

    internal virtual IEnumerator UploadModelAndConfig()
    {
        if (string.IsNullOrEmpty(objPath) || !File.Exists(objPath) ||
            string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
        {
            UnityEngine.Debug.LogWarning("Upload skipped: OBJ or Config path invalid.");
            yield break;
        }

        WWWForm form = new WWWForm();

        // OBJ dosyasını yükle
        byte[] objData = File.ReadAllBytes(objPath);
        string objFileName = Path.GetFileName(objPath);
        form.AddBinaryData("obj_file", objData, objFileName);

        // MTL dosyasını yükle (eğer varsa)
        if (!string.IsNullOrEmpty(mtlPath) && File.Exists(mtlPath))
        {
            byte[] mtlData = File.ReadAllBytes(mtlPath);
            string mtlFileName = Path.GetFileName(mtlPath);
            form.AddBinaryData("mtl_file", mtlData, mtlFileName);
        }

        // Texture dosyalarını bul ve yükle (PNG, JPG, JPEG, TGA, BMP)
        string[] textureExtensions = { ".png", ".jpg", ".jpeg", ".tga", ".bmp", ".tif" };
        foreach (string filePath in Directory.GetFiles(modelPath))
        {
            string extension = Path.GetExtension(filePath).ToLower();

            // Düzeltilmiş Contains kullanımı
            bool isTexture = false;
            foreach (string texExt in textureExtensions)
            {
                if (extension == texExt)
                {
                    isTexture = true;
                    break;
                }
            }

            if (isTexture)
            {
                string textureFileName = Path.GetFileName(filePath);
                byte[] textureData = File.ReadAllBytes(filePath);
                form.AddBinaryData("texture_files", textureData, textureFileName);
                UnityEngine.Debug.Log("Adding texture to upload: " + textureFileName);
            }
        }

        // Config dosyasını yükle
        byte[] configData = File.ReadAllBytes(configPath);
        string configFileName = Path.GetFileName(configPath);
        form.AddBinaryData("config_file", configData, configFileName);

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


    internal void UploadFolderContents()
    {
        try
        {
            string uploadsDir = Path.Combine(Application.dataPath, "..", "uploads");
            Directory.CreateDirectory(uploadsDir);

            foreach (string filePath in Directory.GetFiles(modelPath))
            {
                string extension = Path.GetExtension(filePath).ToLower();
                string fileName = Path.GetFileName(filePath);

                if (extension != ".fbx")
                {
                    string destPath = Path.Combine(uploadsDir, fileName);
                    File.Copy(filePath, destPath, true);
                    UnityEngine.Debug.Log("Copied file to uploads: " + fileName);
                }
            }

            UnityEngine.Debug.Log("Folder contents copied to uploads successfully");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("Error copying folder contents: " + ex.Message);
        }
    }


    internal virtual IEnumerator DownloadConfigs()
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
            
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Error deleting folder {folderPath}: {e.Message}");
        }
    }


}
