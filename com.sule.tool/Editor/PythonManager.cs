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
    // File paths for model and configuration files
    internal string modelPath = "";
    internal string objPath = "";
    internal string mtlPath = "";
    internal string configPath = "";
    internal string base_map = "";

    internal string newModelPath = "";

    // UI state flags
    internal bool showAbsolutePathWarning = false;
    internal bool showConfigFoldout;
    internal bool showMapFoldout;

    // Folder paths for downloaded content
    internal string desktopPath;
    internal string configFolder;
    internal string mapFolder;

    // Server connection settings
    internal string baseURL = "http://127.0.0.1:8000/";

    // Progress tracking variables
    internal bool showProgressBar = false;
    internal float progress = 0f;
    internal WebSocket webSocket;
    internal string currentTaskId = "";

    // Menu item to open the tool window
    [MenuItem("Tools/MapGen")]
    public static void ShowWindow()
    {
        GetWindow<MyToolWindow>("MapGen");
    }

    // Dependency injection for web request handling (for testing)
    internal IWebRequestHandler requestHandler = new WebRequestHandler();

    internal void SetRequestHandler(IWebRequestHandler handler)
    {
        this.requestHandler = handler;
    }

    // Initialize window state
    internal void OnEnable()
    {
        showConfigFoldout = false;
        showMapFoldout = false;
    }

    // Main GUI rendering method
    internal void OnGUI()
    {
        Event evt = Event.current;

        GUILayout.Label("Input Fields", EditorStyles.boldLabel);

        // === MODEL FOLDER SELECTION ===
        GUILayout.BeginHorizontal();
        Rect modelRect = GUILayoutUtility.GetRect(new GUIContent("Model Folder"), GUI.skin.textField);
        GUI.SetNextControlName("Model Folder");
        newModelPath = EditorGUI.TextField(modelRect, "Model Folder", newModelPath);

        // Handle Enter key press for manual input validation
        if (evt.isKey && evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Return)
        {
            string focused = GUI.GetNameOfFocusedControl();

            if (focused == "Model Folder")
            {
                // Validate absolute path
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
                    ProcessModelFolder(); // Process the selected model folder
                }
            }
            else if (focused == "Config (.yaml)")
            {
                // Validate config file path
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
                    // Auto-upload if model is already selected
                    if (!string.IsNullOrEmpty(objPath) && File.Exists(objPath))
                    {
                        EditorCoroutineUtility.StartCoroutineOwnerless(UploadModelAndConfig());
                    }
                }
            }
        }

        // Helper function to display popup messages
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

        // Browse button for folder selection
        if (GUILayout.Button("Browse", GUILayout.MaxWidth(80)))
        {
            // Open folder selection dialog
            string path = EditorUtility.OpenFolderPanel("Select Model Folder", "", "");
            if (!string.IsNullOrEmpty(path))
            {
                newModelPath = Path.GetFullPath(path);
                modelPath = newModelPath;
                ProcessModelFolder(); // Process the selected folder
            }
        }
        GUILayout.EndHorizontal();

        // Drag and drop support for model folder
        if (modelRect.Contains(evt.mousePosition))
        {
            if (evt.type == EventType.DragUpdated)
            {
                // Validate dragged items are folders
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
                // Accept folder drag and drop
                DragAndDrop.AcceptDrag();
                foreach (var path in DragAndDrop.paths)
                {
                    if (Directory.Exists(path))
                    {
                        UnityEngine.Debug.Log("Model folder dragged: " + path);
                        newModelPath = Path.GetFullPath(path);
                        modelPath = newModelPath;
                        ProcessModelFolder(); // Process the dragged folder
                        break;
                    }
                }
                evt.Use();
            }
        }

        // === CONFIG FILE SELECTION ===
        GUILayout.BeginHorizontal();
        Rect configRect = GUILayoutUtility.GetRect(new GUIContent("Config (.yaml)"), GUI.skin.textField);
        GUI.SetNextControlName("Config (.yaml)");
        configPath = EditorGUI.TextField(configRect, "Config (.yaml)", configPath);

        // Browse button for config file selection
        if (GUILayout.Button("Browse", GUILayout.MaxWidth(80)))
        {
            string path = EditorUtility.OpenFilePanel("Select Config YAML", "", "yaml,yml");
            if (!string.IsNullOrEmpty(path))
            {
                configPath = path;
                // Auto-upload if model is already selected
                if (!string.IsNullOrEmpty(objPath) && File.Exists(objPath))
                {
                    EditorCoroutineUtility.StartCoroutineOwnerless(UploadModelAndConfig());
                }
            }
        }
        GUILayout.EndHorizontal();

        // Drag and drop support for config files
        if ((evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform) && configRect.Contains(evt.mousePosition))
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (var path in DragAndDrop.paths)
                {
                    string ext = Path.GetExtension(path).ToLower();
                    // Only accept YAML files
                    if (ext == ".yaml" || ext == ".yml")
                    {
                        configPath = path;
                        UnityEngine.Debug.Log("Config file dragged: " + path);
                        // Auto-upload if model is already selected
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

        // Clear uploaded files button
        if (GUILayout.Button("Clear Uploads"))
        {
            UnityEngine.Debug.Log("Clear Uploads clicked");
            EditorCoroutineUtility.StartCoroutineOwnerless(ClearDirectoryCoroutine("clear_uploads"));
        }

        // === CONFIG MANAGEMENT FOLDOUT ===
        showConfigFoldout = EditorGUILayout.Foldout(showConfigFoldout, "Config Management", true);
        if (showConfigFoldout)
        {
            EditorGUI.indentLevel++;

            // Create configs from current model
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

            // Download and show available configs
            if (GUILayout.Button("Show Configs"))
            {
                UnityEngine.Debug.Log("Show Configs clicked");
                EditorCoroutineUtility.StartCoroutineOwnerless(ShowConfigsCoroutine());
            }

            // Clear config files
            if (GUILayout.Button("Clear Configs"))
            {
                UnityEngine.Debug.Log("Clear Configs clicked");
                EditorCoroutineUtility.StartCoroutineOwnerless(ClearDirectoryCoroutine("clear_configs"));
                ClearFolderandDelete(configFolder);
            }

            EditorGUI.indentLevel--;
        }

        // === MAP MANAGEMENT FOLDOUT ===
        showMapFoldout = EditorGUILayout.Foldout(showMapFoldout, "Map Management", true);
        if (showMapFoldout)
        {
            EditorGUI.indentLevel++;

            // Generate maps from current model
            if (GUILayout.Button("Create Maps"))
            {
                UnityEngine.Debug.Log("Create Maps clicked");
                EditorCoroutineUtility.StartCoroutineOwnerless(CreateMapsAndListenProgress());
            }

            // Download and show generated maps
            if (GUILayout.Button("Show Maps"))
            {
                UnityEngine.Debug.Log("Show Maps clicked");
                EditorCoroutineUtility.StartCoroutineOwnerless(ShowMapsCoroutine());
            }

            // Clear map files
            if (GUILayout.Button("Clear Maps"))
            {
                UnityEngine.Debug.Log("Clear Maps clicked");
                EditorCoroutineUtility.StartCoroutineOwnerless(ClearDirectoryCoroutine("clear_maps"));
                ClearFolderandDelete(mapFolder);
            }

            EditorGUI.indentLevel--;
        }

        // Process WebSocket messages if connection is open
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

        // Display progress bar if map generation is in progress
        if (showProgressBar)
        {
            GUILayout.Space(10);
            GUILayout.Label($"Map generation progress: {Mathf.RoundToInt(progress * 100)}%", EditorStyles.boldLabel);
            Rect rect = GUILayoutUtility.GetRect(18, 18, "TextField");
            EditorGUI.ProgressBar(rect, progress, "Progress");
            Repaint();
        }
    }

    // Send request to create configuration files on server
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

            // Handle HTTP request failure
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

    // Coroutine to download and display configuration files
    internal virtual IEnumerator ShowConfigsCoroutine()
    {
        desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        configFolder = System.IO.Path.Combine(desktopPath, "ConfigsFolder");

        // Create config folder if it doesn't exist
        if (!Directory.Exists(configFolder))
        {
            Directory.CreateDirectory(configFolder);
        }

        // Get list of available config files from server
        UnityWebRequest listRequest = UnityWebRequest.Get(baseURL + "configs/list");
        yield return listRequest.SendWebRequest();

        if (listRequest.result != UnityWebRequest.Result.Success)
        {
            UnityEngine.Debug.LogError("Failed to get config list: " + listRequest.error);
            yield break;
        }

        // Parse file list from JSON response
        ConfigList files = JsonUtility.FromJson<ConfigList>(listRequest.downloadHandler.text);

        // Download each config file
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

        // Open folder in file explorer
        OpenFolderInExplorer(configFolder);
    }

    // Coroutine to clear directory on server
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

    // Coroutine to create maps and monitor progress via WebSocket
    internal virtual IEnumerator CreateMapsAndListenProgress()
    {
        showProgressBar = true;
        progress = 0f;

        // Initialize progress tracking on server
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

        // Prepare map creation request data
        CreatorRequest sendData = new CreatorRequest
        {
            base_map = objPath.Replace("\\", "/")
        };

        string json = JsonUtility.ToJson(sendData);

        // Send map creation request to server
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

                // Parse response to get task ID for progress tracking
                CreateMapsResponse responseObj = JsonUtility.FromJson<CreateMapsResponse>(www.downloadHandler.text);
                if (responseObj == null || string.IsNullOrEmpty(responseObj.task_id))
                {
                    UnityEngine.Debug.LogError("Task ID not received!");
                    showProgressBar = false;
                    yield break;
                }
                currentTaskId = responseObj.task_id;
            }
        }

        // Wait for WebSocket progress monitoring to complete
        yield return WaitForTask(ListenProgressWebSocket(currentTaskId));
    }

    // Coroutine to download and display generated maps
    internal virtual IEnumerator ShowMapsCoroutine()
    {
        string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        mapFolder = Path.Combine(desktopPath, "MapsFolder");

        // Create maps folder if it doesn't exist
        if (!Directory.Exists(mapFolder))
            Directory.CreateDirectory(mapFolder);

        // Get list of available map files from server
        UnityWebRequest listRequest = UnityWebRequest.Get(baseURL + "maps/list");
        yield return listRequest.SendWebRequest();

        if (listRequest.result != UnityWebRequest.Result.Success)
        {
            UnityEngine.Debug.LogError("Failed to get map list: " + listRequest.error);
            yield break;
        }

        // Parse file list from JSON response
        ConfigList files = JsonUtility.FromJson<ConfigList>(listRequest.downloadHandler.text);

        // Download each map file
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

        // Open folder in file explorer
        OpenFolderInExplorer(mapFolder);
    }

    // Parse file path from JSON response
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

    // Open folder in system file explorer
    internal virtual void OpenFolderInExplorer(string path)
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        System.Diagnostics.Process.Start("explorer.exe", path.Replace("/", "\\"));
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        System.Diagnostics.Process.Start("open", path);
#endif
    }

    // Process model folder to find and convert 3D model files
    internal void ProcessModelFolder()
    {
        // Find FBX and OBJ files in the selected folder
        string[] fbxFiles = Directory.GetFiles(modelPath, "*.fbx", SearchOption.TopDirectoryOnly);
        string[] objFiles = Directory.GetFiles(modelPath, "*.obj", SearchOption.TopDirectoryOnly);

        // Prioritize FBX files for conversion
        if (fbxFiles.Length > 0)
        {
            // Use first FBX file found
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
            // Use first OBJ file found
            objPath = objFiles[0];

            // Try to find corresponding MTL file
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

        // Auto-upload if config file is also selected
        if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
        {
            EditorCoroutineUtility.StartCoroutineOwnerless(UploadModelAndConfig());
        }
    }

    // WebSocket connection for real-time progress monitoring
    internal async Task ListenProgressWebSocket(string taskId)
    {
        if (string.IsNullOrEmpty(taskId))
        {
            UnityEngine.Debug.LogError("Task ID is null or empty. Aborting WebSocket connection.");
            return;
        }

        // WebSocket URL for progress updates
        string wsUrl = $"ws://127.0.0.1:8000/ws/progress/{taskId}";
        UnityEngine.Debug.Log($"Attempting WebSocket connection to: {wsUrl}");

        webSocket = new WebSocket(wsUrl);

        // WebSocket event handlers
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

            // Parse progress message from JSON
            var msg = JsonUtility.FromJson<ProgressMessage>(message);
            if (msg == null)
            {
                UnityEngine.Debug.LogError("JSON parse failed. Raw: " + message);
                return;
            }

            // Update progress bar
            progress = Mathf.Clamp01(msg.progress);

            // Handle completion
            if (progress >= 1f)
            {
                UnityEngine.Debug.Log("Progress complete.");
                showProgressBar = false;

                await CloseWebSocketSafely();
            }
        };

        // Establish WebSocket connection
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

        // Process incoming messages while connection is open
        while (webSocket != null && webSocket.State == WebSocketState.Open)
        {
            webSocket.DispatchMessageQueue();
            await Task.Delay(100);
        }

        webSocket = null;
        UnityEngine.Debug.Log("WebSocket closed and cleaned up.");
    }

    // Coroutine to wait for async task completion
    internal IEnumerator WaitForTask(Task task)
    {
        while (!task.IsCompleted)
            yield return null;

        if (task.IsFaulted)
            UnityEngine.Debug.LogError(task.Exception);
    }

    // Safely close WebSocket connection
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

    // Upload model files and configuration to server
    internal virtual IEnumerator UploadModelAndConfig()
    {
        // Validate file paths
        if (string.IsNullOrEmpty(objPath) || !File.Exists(objPath) ||
            string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
        {
            UnityEngine.Debug.LogWarning("Upload skipped: OBJ or Config path invalid.");
            yield break;
        }

        WWWForm form = new WWWForm();

        // Upload OBJ file
        byte[] objData = File.ReadAllBytes(objPath);
        string objFileName = Path.GetFileName(objPath);
        form.AddBinaryData("obj_file", objData, objFileName);

        // Upload MTL file if exists
        if (!string.IsNullOrEmpty(mtlPath) && File.Exists(mtlPath))
        {
            byte[] mtlData = File.ReadAllBytes(mtlPath);
            string mtlFileName = Path.GetFileName(mtlPath);
            form.AddBinaryData("mtl_file", mtlData, mtlFileName);
        }

        // Find and upload texture files
        string[] textureExtensions = { ".png", ".jpg", ".jpeg", ".tga", ".bmp", ".tif" };
        foreach (string filePath in Directory.GetFiles(modelPath))
        {
            string extension = Path.GetExtension(filePath).ToLower();

            // Check if file is a supported texture format
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

        // Upload config file
        byte[] configData = File.ReadAllBytes(configPath);
        string configFileName = Path.GetFileName(configPath);
        form.AddBinaryData("config_file", configData, configFileName);

        // Send upload request to server
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

    // Copy folder contents to uploads directory (legacy function)
    internal void UploadFolderContents()
    {
        try
        {
            string uploadsDir = Path.Combine(Application.dataPath, "..", "uploads");
            Directory.CreateDirectory(uploadsDir);

            // Copy all non-FBX files to uploads directory
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

    // Download config files from server (legacy function)
    internal virtual IEnumerator DownloadConfigs()
    {
        string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        string configsFolder = System.IO.Path.Combine(desktopPath, "ConfigsFolder");
        if (!System.IO.Directory.Exists(configsFolder))
            System.IO.Directory.CreateDirectory(configsFolder);

        // Get list of config files from server
        UnityWebRequest www = UnityWebRequest.Get(baseURL + "/configs/list");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            UnityEngine.Debug.LogError("Config list error: " + www.error);
            yield break;
        }

        var json = www.downloadHandler.text;
        var files = JsonUtility.FromJson<ConfigList>(json);

        // Download each config file
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

    // Delete folder and all its contents
    internal void ClearFolderandDelete(string folderPath)
    {
        try
        {
            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, true); // 'true' for recursive deletion
                UnityEngine.Debug.Log($"Deleted folder and contents: {folderPath}");
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Error deleting folder {folderPath}: {e.Message}");
        }
    }
}