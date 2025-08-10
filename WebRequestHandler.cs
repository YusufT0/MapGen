using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class WebRequestHandler : IWebRequestHandler
{
    // JSON verisi ile POST isteði gönderir
    public IEnumerator PostJson(string url, string json, Action<string> onSuccess, Action<string> onError)
    {
        using (UnityWebRequest www = UnityWebRequest.PostWwwForm(url, ""))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
                onError?.Invoke(www.error);
            else
                onSuccess?.Invoke(www.downloadHandler.text);
        }
    }

    // GET isteði gönderir
    public IEnumerator Get(string url, Action<string> onSuccess, Action<string> onError)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
                onError?.Invoke(www.error);
            else
                onSuccess?.Invoke(www.downloadHandler.text);
        }
    }
}
