using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using UnityEditor;

public class ConfigTests
{
    private MyToolWindow window;
    private WebRequestHandler mockHandler;

    [SetUp]
    public void SetUp()
    {
        // Create a new instance of MyToolWindow for testing
        window = ScriptableObject.CreateInstance<MyToolWindow>();

        // Create an instance of the mock WebRequestHandler
        mockHandler = new WebRequestHandler();

        // Inject the mock handler into the window
        window.SetRequestHandler(mockHandler);
    }

    [TearDown]
    public void TearDown()
    {
        // Destroy the window instance after each test to clean up
        Object.DestroyImmediate(window);
    }

    [UnityTest]
    public IEnumerator TestGetRequestLogsSuccess()
    {
        string testUrl = "http://test.com/getConfig";

        // Perform a GET request and wait for the response
        yield return mockHandler.Get(testUrl,
            onSuccess: (response) => Debug.Log("GET success: " + response),
            onError: (error) => Debug.LogError("GET error: " + error));

        // Verify that the success log with the expected response was printed
        LogAssert.Expect(LogType.Log, $"GET success: {{\"config\":\"mocked\"}}");

        // Check that the last GET URL matches the test URL
        Assert.AreEqual(testUrl, mockHandler.LastGetUrl);
    }

    [UnityTest]
    public IEnumerator TestPostRequestLogsSuccess()
    {
        string testJson = "{\"key\":\"value\"}";

        // Perform a POST JSON request and wait for the response
        yield return mockHandler.PostJson("http://test.com/post", testJson,
            onSuccess: (response) => Debug.Log("POST success: " + response),
            onError: (error) => Debug.LogError("POST error: " + error));

        // Verify that the success log with the expected response was printed
        LogAssert.Expect(LogType.Log, "POST success: {\"status\":\"ok\"}");

        // Check that the last posted JSON matches the test JSON
        Assert.AreEqual(testJson, mockHandler.LastPostedJson);
    }

    [UnityTest]
    public IEnumerator TestDeleteRequestLogsSuccess()
    {
        string testUrl = "http://test.com/delete";

        // Perform a DELETE request and wait for the response
        yield return mockHandler.Delete(testUrl,
            onSuccess: () => Debug.Log("DELETE success"),
            onError: (error) => Debug.LogError("DELETE error: " + error));

        // Verify that the success log was printed
        LogAssert.Expect(LogType.Log, "DELETE success");

        // Check that the last delete URL matches the test URL
        Assert.AreEqual(testUrl, mockHandler.LastDeleteUrl);
    }
}
