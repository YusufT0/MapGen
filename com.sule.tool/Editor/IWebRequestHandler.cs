using System;
using System.Collections;
using UnityEngine;

public interface IWebRequestHandler
{
    // Sends a POST request with JSON data to the specified URL.
    // onSuccess is called with the response string if the request succeeds.
    // onError is called with an error message if the request fails.
    IEnumerator PostJson(string url, string json, Action<string> onSuccess, Action<string> onError);

    // Sends a GET request to the specified URL.
    // onSuccess is called with the response string if the request succeeds.
    // onError is called with an error message if the request fails.
    IEnumerator Get(string url, Action<string> onSuccess, Action<string> onError);

    // Sends a DELETE request to the specified URL.
    // onSuccess is called when the request succeeds.
    // onError is called with an error message if the request fails.
    IEnumerator Delete(string url, Action onSuccess, Action<string> onError);
}
