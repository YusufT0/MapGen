using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;
using Unity.EditorCoroutines.Editor;
using UnityEngine.Networking; 

public class MyToolWindow : EditorWindow
{
    // Kullan�c�n�n se�ece�i model dosyas�n�n yolu (.fbx vb.)
    string modelPath = "";
    string mtlPath = "";

    // Kullan�c�n�n se�ece�i config dosyas�n�n yolu (.yaml)
    string configPath = "";

    // Backend API base URL(FastAPI �al��t��� adres)
    string baseURL = "http://127.0.0.1:8000/";

    // Unity Editor men�s�ne bu pencereyi ekler, "Tools/My Tool UI" se�ene�iyle a��l�r
    [MenuItem("Tools/My Tool UI")]
    public static void ShowWindow()
    {
        GetWindow<MyToolWindow>("My Tool UI");
    }

    // Editor penceresindeki UI elemanlar�n� �izdi�imiz metod
    void OnGUI()
    {
        GUILayout.Label("Input Fields", EditorStyles.boldLabel);

        // Model dosyas� i�in input ve Browse butonu
        GUILayout.BeginHorizontal();
        modelPath = EditorGUILayout.TextField("Model", modelPath);
        if (GUILayout.Button("Browse", GUILayout.MaxWidth(80)))
        {
            // Dosya se�me paneli a�, sadece fbx dosyalar� g�ster
            string path = EditorUtility.OpenFilePanel("Select FBX Model", "", "fbx");
            if (!string.IsNullOrEmpty(path)) modelPath = path;
        }
        GUILayout.EndHorizontal();

        // Config (.yaml) dosyas� i�in input ve Browse butonu
        GUILayout.BeginHorizontal();
        configPath = EditorGUILayout.TextField("Config (.yaml)", configPath);
        if (GUILayout.Button("Browse", GUILayout.MaxWidth(80)))
        {
            // Dosya se�me paneli a�, sadece yaml dosyalar� g�ster
            string path = EditorUtility.OpenFilePanel("Select Config YAML", "", "yaml");
            if (!string.IsNullOrEmpty(path)) configPath = path;
        }
        GUILayout.EndHorizontal();

        // Model ve config yolu dolu mu diye kontrol
        bool inputsFilled = !string.IsNullOrEmpty(modelPath) && !string.IsNullOrEmpty(configPath);

        // Butonlar� enable/disable yapmak i�in blok (�u anda kullan�lm�yor)
        EditorGUI.BeginDisabledGroup(!inputsFilled);
        EditorGUI.EndDisabledGroup();

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
            // Backend'den config klas�r yolunu al�p explorer'da a�an coroutine ba�lat�l�r
            Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutineOwnerless(ShowConfigsCoroutine());
        }

        // "Create Maps" butonu
        if (GUILayout.Button("Create Maps"))
        {
            UnityEngine.Debug.Log("Create Maps clicked");
            // TODO: Add logic
        }

        // "Show Maps" butonu
        if (GUILayout.Button("Show Maps"))
        {
            UnityEngine.Debug.Log("Show Maps clicked");
            // Backend'den haritalar�n oldu�u klas�r yolunu al�p explorer'da a�an coroutine ba�lat�l�r
            Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutineOwnerless(ShowMapsCoroutine());
        }
    }

    private System.Collections.IEnumerator SendCreateConfigsRequest(string json)
    {
        string url = baseURL + "create_configs";

        using (UnityWebRequest www = UnityWebRequest.PostWwwForm(url, ""))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            // HTTP iste�i başarısızsa hata mesaj� yaz
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

    // Haritalar�n bulundu�u klas�r�n yolunu backend'den al�p a�an coroutine
    private System.Collections.IEnumerator ShowMapsCoroutine()
    {
        string endpoint = baseURL + "get_map_path";

        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(endpoint))
        {
            yield return www.SendWebRequest();

            // HTTP iste�i ba�ar�s�zsa hata mesaj� yaz
            if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                UnityEngine.Debug.LogError("HTTP Error: " + www.error);
                yield break;
            }

            // JSON cevab� al
            string jsonResponse = www.downloadHandler.text;
            UnityEngine.Debug.Log("JSON Response: " + jsonResponse);

            // JSON'dan path de�erini parse et
            string path = ParsePathFromJson(jsonResponse);
            UnityEngine.Debug.Log("Parsed Path: " + path);

            // Path ge�erliyse klas�r� explorer'da a�
            if (!string.IsNullOrEmpty(path) && System.IO.Directory.Exists(path))
            {
                OpenFolderInExplorer(path);
            }
            else
            {
                UnityEngine.Debug.LogError("Invalid path or folder does not exists: " + path);
            }
        }
    }

    // Config klas�r yolunu backend'den al�p a�an coroutine
    private System.Collections.IEnumerator ShowConfigsCoroutine()
    {
        string endpoint = baseURL + "get_config_path";

        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(endpoint))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                UnityEngine.Debug.LogError("HTTP Error: " + www.error);
                yield break;
            }

            string jsonResponse = www.downloadHandler.text;
            UnityEngine.Debug.Log("JSON Response: " + jsonResponse);

            string path = ParsePathFromJson(jsonResponse);
            UnityEngine.Debug.Log("Parsed Path: " + path);

            if (!string.IsNullOrEmpty(path) && System.IO.Directory.Exists(path))
            {
                OpenFolderInExplorer(path);
            }
            else
            {
                UnityEngine.Debug.LogError("Invalid path or folder does not exists: " + path);
            }
        }
    }

    // Backend'den d�nen JSON'dan "path" de�erini parse eder
    string ParsePathFromJson(string json)
    {
        try
        {
            var parsed = JsonUtility.FromJson<PathResponse>(json);
            return parsed.path;
        }
        catch
        {
            UnityEngine.Debug.LogError("JSON can not parsed: " + json);
            return null;
        }
    }

    // JSON parse etmek i�in kullan�lan s�n�f, backend {"path": "some/folder"} �eklinde d�ner
    [System.Serializable]
    class PathResponse
    {
        public string path;
    }

    // Verilen klas�r yolunu Windows Explorer'da a�ar
    void OpenFolderInExplorer(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            UnityEngine.Debug.LogError("Path is null or empty");
            return;
        }
        // Windows i�in explorer.exe ile klas�r� a�
        System.Diagnostics.Process.Start("explorer.exe", path.Replace("/", "\\"));   
 
    }


    // Masa�st�n� Windows Explorer'da a�an yard�mc� metod (�u an kullan�lm�yor)
    void OpenFileExplorer()
    {
        string path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);

        ProcessStartInfo startInfo = new ProcessStartInfo()
        {
            FileName = "explorer.exe",
            Arguments = path,
            UseShellExecute = true
        };

        Process.Start(startInfo);
    }

}

