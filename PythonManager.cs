using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;
using Unity.EditorCoroutines.Editor;
using UnityEngine.Networking; 

public class MyToolWindow : EditorWindow
{
    // Kullanýcýnýn seçeceði model dosyasýnýn yolu (.fbx vb.)
    string modelPath = "";

    // Kullanýcýnýn seçeceði config dosyasýnýn yolu (.yaml)
    string configPath = "";

    // Backend API base URL(FastAPI çalýþtýðý adres)
    string baseURL = "http://127.0.0.1:8000/";

    // Unity Editor menüsüne bu pencereyi ekler, "Tools/My Tool UI" seçeneðiyle açýlýr
    [MenuItem("Tools/My Tool UI")]
    public static void ShowWindow()
    {
        GetWindow<MyToolWindow>("My Tool UI");
    }

    // Editor penceresindeki UI elemanlarýný çizdiðimiz metod
    void OnGUI()
    {
        GUILayout.Label("Input Fields", EditorStyles.boldLabel);

        // Model dosyasý için input ve Browse butonu
        GUILayout.BeginHorizontal();
        modelPath = EditorGUILayout.TextField("Model", modelPath);
        if (GUILayout.Button("Browse", GUILayout.MaxWidth(80)))
        {
            // Dosya seçme paneli aç, sadece fbx dosyalarý göster
            string path = EditorUtility.OpenFilePanel("Select FBX Model", "", "fbx");
            if (!string.IsNullOrEmpty(path)) modelPath = path;
        }
        GUILayout.EndHorizontal();

        // Config (.yaml) dosyasý için input ve Browse butonu
        GUILayout.BeginHorizontal();
        configPath = EditorGUILayout.TextField("Config (.yaml)", configPath);
        if (GUILayout.Button("Browse", GUILayout.MaxWidth(80)))
        {
            // Dosya seçme paneli aç, sadece yaml dosyalarý göster
            string path = EditorUtility.OpenFilePanel("Select Config YAML", "", "yaml");
            if (!string.IsNullOrEmpty(path)) configPath = path;
        }
        GUILayout.EndHorizontal();

        // Model ve config yolu dolu mu diye kontrol
        bool inputsFilled = !string.IsNullOrEmpty(modelPath) && !string.IsNullOrEmpty(configPath);

        // Butonlarý enable/disable yapmak için blok (þu anda kullanýlmýyor)
        EditorGUI.BeginDisabledGroup(!inputsFilled);
        EditorGUI.EndDisabledGroup();

        // "Create Configs" butonu
        if (GUILayout.Button("Create Configs"))
        {
            UnityEngine.Debug.Log("Create Configs clicked");
            // TODO: Add logic
        }

        // "Show Configs" butonu
        if (GUILayout.Button("Show Configs"))
        {
            UnityEngine.Debug.Log("Show Configs clicked");
            // Backend'den config klasör yolunu alýp explorer'da açan coroutine baþlatýlýr
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
            // Backend'den haritalarýn olduðu klasör yolunu alýp explorer'da açan coroutine baþlatýlýr
            Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutineOwnerless(ShowMapsCoroutine());
        }
    }

    // Haritalarýn bulunduðu klasörün yolunu backend'den alýp açan coroutine
    System.Collections.IEnumerator ShowMapsCoroutine()
    {
        string endpoint = baseURL + "/get_map_path";

        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(endpoint))
        {
            yield return www.SendWebRequest();

            // HTTP isteði baþarýsýzsa hata mesajý yaz
            if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                UnityEngine.Debug.LogError("HTTP Error: " + www.error);
                yield break;
            }

            // JSON cevabý al
            string jsonResponse = www.downloadHandler.text;
            UnityEngine.Debug.Log("JSON Response: " + jsonResponse);

            // JSON'dan path deðerini parse et
            string path = ParsePathFromJson(jsonResponse);
            UnityEngine.Debug.Log("Parsed Path: " + path);

            // Path geçerliyse klasörü explorer'da aç
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

    // Config klasör yolunu backend'den alýp açan coroutine
    System.Collections.IEnumerator ShowConfigsCoroutine()
    {
        string endpoint = baseURL + "/get_config_path";

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

    // Backend'den dönen JSON'dan "path" deðerini parse eder
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

    // JSON parse etmek için kullanýlan sýnýf, backend {"path": "some/folder"} þeklinde döner
    [System.Serializable]
    class PathResponse
    {
        public string path;
    }

    // Verilen klasör yolunu Windows Explorer'da açar
    void OpenFolderInExplorer(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            UnityEngine.Debug.LogError("Path is null or empty");
            return;
        }
        // Windows için explorer.exe ile klasörü aç
        System.Diagnostics.Process.Start("explorer.exe", path.Replace("/", "\\"));   
 
    }


    // Masaüstünü Windows Explorer'da açan yardýmcý metod (þu an kullanýlmýyor)
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
