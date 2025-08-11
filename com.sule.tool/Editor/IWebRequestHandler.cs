using System;
using System.Collections;
using UnityEngine;

public interface IWebRequestHandler
{
    // JSON verisini POST eden metot
    IEnumerator PostJson(string url, string json, Action<string> onSuccess, Action<string> onError);

    // GET isteði yapan metot
    IEnumerator Get(string url, Action<string> onSuccess, Action<string> onError);
}
