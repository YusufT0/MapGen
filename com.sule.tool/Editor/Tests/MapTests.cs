using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

public class MapTests
{
    private MyToolWindow toolWindow;

    [SetUp]
    public void Setup()
    {
        // Create a new instance of MyToolWindow for testing
        toolWindow = ScriptableObject.CreateInstance<MyToolWindow>();

        // Inject the real WebRequestHandler to test request handling
        toolWindow.SetRequestHandler(new WebRequestHandler());
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up the created toolWindow instance after each test
        if (toolWindow != null)
            Object.DestroyImmediate(toolWindow);
    }

    [UnityTest]
    public IEnumerator PostJson_Success_LogsCorrectly()
    {
        bool callbackCalled = false;

        // Perform a POST JSON request and wait for completion
        yield return toolWindow.requestHandler.PostJson("http://test.url/post", "{\"test\":1}",
            onSuccess: (response) =>
            {
                // Log success response and mark callback as called
                Debug.Log("POST Success: " + response);
                callbackCalled = true;
            },
            onError: (error) =>
            {
                // Log any error occurred during the POST request
                Debug.LogError("POST Error: " + error);
            });

        // Assert that the success log was printed as expected
        LogAssert.Expect(LogType.Log, "POST Success: {\"status\":\"ok\"}");

        // Verify that the success callback was indeed invoked
        Assert.IsTrue(callbackCalled);

        // Confirm that the last posted JSON data matches what was sent
        Assert.AreEqual("{\"test\":1}", ((WebRequestHandler)toolWindow.requestHandler).LastPostedJson);
    }

    [UnityTest]
    public IEnumerator Get_Success_LogsCorrectly()
    {
        bool callbackCalled = false;

        // Perform a GET request and wait for completion
        yield return toolWindow.requestHandler.Get("http://test.url/get",
            onSuccess: (response) =>
            {
                // Log success response and mark callback as called
                Debug.Log("GET Success: " + response);
                callbackCalled = true;
            },
            onError: (error) =>
            {
                // Log any error occurred during the GET request
                Debug.LogError("GET Error: " + error);
            });

        // Assert that the success log was printed as expected
        LogAssert.Expect(LogType.Log, "GET Success: {\"config\":\"mocked\"}");

        // Verify that the success callback was indeed invoked
        Assert.IsTrue(callbackCalled);

        // Confirm that the last GET URL matches what was requested
        Assert.AreEqual("http://test.url/get", ((WebRequestHandler)toolWindow.requestHandler).LastGetUrl);
    }

    [UnityTest]
    public IEnumerator Delete_Success_LogsCorrectly()
    {
        bool callbackCalled = false;

        // Perform a DELETE request and wait for completion
        yield return toolWindow.requestHandler.Delete("http://test.url/delete",
            onSuccess: () =>
            {
                // Log success message and mark callback as called
                Debug.Log("DELETE Success");
                callbackCalled = true;
            },
            onError: (error) =>
            {
                // Log any error occurred during the DELETE request
                Debug.LogError("DELETE Error: " + error);
            });

        // Assert that the success log was printed as expected
        LogAssert.Expect(LogType.Log, "DELETE Success");

        // Verify that the success callback was indeed invoked
        Assert.IsTrue(callbackCalled);

        // Confirm that the last DELETE URL matches what was requested
        Assert.AreEqual("http://test.url/delete", ((WebRequestHandler)toolWindow.requestHandler).LastDeleteUrl);
    }
}
