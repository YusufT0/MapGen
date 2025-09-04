using System;
using System.Collections;
using UnityEngine;

public class WebRequestHandler : IWebRequestHandler
{
    // Stores the last JSON data sent via PostJson
    public string LastPostedJson { get; private set; }

    // Stores the last URL used for a Get request
    public string LastGetUrl { get; private set; }

    // Stores the last URL used for a Delete request
    public string LastDeleteUrl { get; private set; }

    // Simulates sending a POST request with JSON data.
    // Immediately invokes onSuccess callback with a mocked response.
    public IEnumerator PostJson(string url, string json, Action<string> onSuccess, Action<string> onError)
    {
        LastPostedJson = json;
        onSuccess?.Invoke("{\"status\":\"ok\"}");
        yield break;  // Ends the coroutine immediately
    }

    // Simulates sending a GET request.
    // Immediately invokes onSuccess callback with a mocked JSON response.
    public IEnumerator Get(string url, Action<string> onSuccess, Action<string> onError)
    {
        LastGetUrl = url;
        onSuccess?.Invoke("{\"config\":\"mocked\"}");
        yield break;  // Ends the coroutine immediately
    }

    // Simulates sending a DELETE request.
    // Immediately invokes onSuccess callback.
    public IEnumerator Delete(string url, Action onSuccess, Action<string> onError)
    {
        LastDeleteUrl = url;
        onSuccess?.Invoke();
        yield break;  // Ends the coroutine immediately
    }
}
