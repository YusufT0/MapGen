using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
using System.Diagnostics;
using System.IO;
using Unity.EditorCoroutines.Editor;

internal class MyToolWindow : EditorWindow
{
    // Kullanıcının seçeceği model dosyasının yolu (.fbx vb.)
    internal string modelPath = "";
    internal string mtlPath = "";

    // Kullanıcının seçeceği config dosyasının yolu (.yaml)
    internal string configPath = "";

    // Backend API base URL (FastAPI base adresi)
    string baseURL = "http://127.0.0.1:8000/";

    // Unity Editor menüsüne bu pencereyi ekler, "Tools/My Tool UI" seçeneği ile açılır
    [MenuItem("Tools/My Tool UI")]
    public static void ShowWindow()
    {
        GetWindow<MyToolWindow>("My Tool UI");
    }

    private IWebRequestHandler requestHandler = new WebRequestHandler(); // varsayılan handler

    // Testlerde bu değeri değiştirebiliriz
    internal void SetRequestHandler(IWebRequestHandler handler)
    {
        this.requestHandler = handler;
    }

    // Editor penceresindeki UI elemanlarını çizdiğimiz metod
    internal void OnGUI()
    {
        GUILayout.Label("Input Fields", EditorStyles.boldLabel);

        // Model dosyası için input ve "Browse" butonu
        GUILayout.BeginHorizontal();
        modelPath = EditorGUILayout.TextField("Model", modelPath);
        if (GUILayout.Button("Browse", GUILayout.MaxWidth(80)))
        {
            // Dosya seçme paneli açılır, sadece fbx dosyaları gösterilir
            string path = EditorUtility.OpenFilePanel("Select FBX Model", "", "fbx");
            if (!string.IsNullOrEmpty(path)) modelPath = path;
        }
        GUILayout.EndHorizontal();

        // Config (.yaml) dosyası için input ve "Browse" butonu
        GUILayout.BeginHorizontal();
        configPath = EditorGUILayout.TextField("Config (.yaml)", configPath);
        if (GUILayout.Button("Browse", GUILayout.MaxWidth(80)))
        {
            // Dosya seçme paneli açılır, sadece yaml dosyaları gösterilir
            string path = EditorUtility.OpenFilePanel("Select Config YAML", "", "yaml");
            if (!string.IsNullOrEmpty(path)) configPath = path;
        }
        GUILayout.EndHorizontal();

        // Model ve config yolu dolu mu diye kontrol
        bool inputsFilled = !string.IsNullOrEmpty(modelPath) && !string.IsNullOrEmpty(configPath);

        // Butonları aktif/pasif yapmak için blok
        EditorGUI.BeginDisabledGroup(!inputsFilled);

        // "Create Configs" butonu
        if (GUILayout.Button("Create Configs"))
        {
            UnityEngine.Debug.Log("Create Configs clicked");

            ConfigRequest postData = new ConfigRequest
            {
                obj_path = modelPath,
                mtl_path = mtlPath,
                config_path = configPath
            };

            string json = JsonUtility.ToJson(postData);

            Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutineOwnerless(SendCreateConfigsRequest(json));
        }

        // "Show Configs" butonu
        if (GUILayout.Button("Show Configs"))
        {
            UnityEngine.Debug.Log("Show Configs clicked");
            // Backend'den config klasör yolunu alıp explorer'da açan coroutine başlatılır
            Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutineOwnerless(ShowConfigsCoroutine());
        }

        // "Create Maps" butonu
        if (GUILayout.Button("Create Maps"))
        {
            UnityEngine.Debug.Log("Create Maps clicked");

            ConfigRequest sendData = new ConfigRequest
            {
                obj_path = modelPath,
                mtl_path = mtlPath,
                config_path = configPath
            };

            string json = JsonUtility.ToJson(sendData);

            Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutineOwnerless(SendCreateMapsRequest(json));
        }

        // "Show Maps" butonu
        if (GUILayout.Button("Show Maps"))
        {
            UnityEngine.Debug.Log("Show Maps clicked");
            // Backend'den haritaların olduğu klasör yolunu alıp explorer'da açan coroutine başlatılır
            Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutineOwnerless(ShowMapsCoroutine());
        }

        EditorGUI.EndDisabledGroup();
    }

    // Config oluşturma isteğini backend'e gönderen coroutine
    internal virtual IEnumerator SendCreateConfigsRequest(string json)
    {
        string url = baseURL + "create_configs";

        using (UnityWebRequest www = UnityWebRequest.PostWwwForm(url, ""))
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

    // Config klasör yolunu backend'den alıp açan coroutine
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

    // Harita oluşturma isteğini backend'e gönderen coroutine
    internal virtual IEnumerator SendCreateMapsRequest(string json)
    {
        string url = baseURL + "create_maps";

        using (UnityWebRequest www = UnityWebRequest.PostWwwForm(url, ""))
        {
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
    }

    // Haritaların bulunduğu klasör yolunu backend'den alıp açan coroutine
    internal virtual IEnumerator ShowMapsCoroutine()
    {
        string endpoint = baseURL + "get_map_path";

        using (UnityWebRequest www = UnityWebRequest.Get(endpoint))
        {
            yield return www.SendWebRequest();

            // HTTP isteği başarısızsa hata mesajı yaz
            if (www.result != UnityWebRequest.Result.Success)
            {
                UnityEngine.Debug.LogError("HTTP Error: " + www.error);
                yield break;
            }

            // JSON cevabı al
            string jsonResponse = www.downloadHandler.text;
            UnityEngine.Debug.Log("JSON Response: " + jsonResponse);

            // JSON'dan path değerini parse et
            string path = ParsePathFromJson(jsonResponse);
            UnityEngine.Debug.Log("Parsed Path: " + path);

            // Path geçerliyse klasörü explorer'da aç
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

    // Backend'den dönen JSON'dan "path" değerini parse eder
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

    // JSON parse etmek için kullanılan sınıf, backend {"path": "some/folder"} şeklinde döner
    [System.Serializable]
    internal class PathResponse
    {
        public string path;
    }

    // Verilen klasör yolunu Windows Explorer'da açar
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
                Arguments = $"\"{winPath}\"",  // Path'i çift tırnak içine al
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

    // Masaüstünü Windows Explorer'da açan yardımcı metod (şu an kullanılmıyor)
    internal virtual void OpenFileExplorer()
    {
        string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        ProcessStartInfo startInfo = new ProcessStartInfo()
        {
            FileName = "explorer.exe",
            Arguments = path,
            UseShellExecute = true
        };

        Process.Start(startInfo);
    }
}
