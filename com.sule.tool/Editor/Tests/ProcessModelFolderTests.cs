using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

public class ProcessModelFolderTests
{
    private MyToolWindow toolWindow;

    [SetUp]
    public void Setup()
    {
        // Create an instance of MyToolWindow before each test
        toolWindow = ScriptableObject.CreateInstance<MyToolWindow>();

        // Assign the mock WebRequestHandler to handle requests
        toolWindow.SetRequestHandler(new WebRequestHandler());
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up the toolWindow instance after each test
        if (toolWindow != null)
            Object.DestroyImmediate(toolWindow);
    }

    [UnityTest]
    public IEnumerator Test_ProcessModelFolder_PostJson_Success_Logs()
    {
        bool callbackCalled = false;

        string testUrl = "http://test.url/processmodel";
        string testJson = "{\"model\":\"data\"}";

        // Perform a POST JSON request and wait for the response
        yield return toolWindow.requestHandler.PostJson(testUrl, testJson,
            onSuccess: (response) =>
            {
                // Log success and mark callback as called
                Debug.Log("ProcessModelFolder POST Success: " + response);
                callbackCalled = true;
            },
            onError: (error) =>
            {
                // Log any errors from the POST request
                Debug.LogError("ProcessModelFolder POST Error: " + error);
            });

        // Verify the expected success log was printed
        LogAssert.Expect(LogType.Log, "ProcessModelFolder POST Success: {\"status\":\"ok\"}");
        // Assert that the success callback was executed
        Assert.IsTrue(callbackCalled);
        // Confirm that the JSON payload sent matches what was recorded in the mock handler
        Assert.AreEqual(testJson, ((WebRequestHandler)toolWindow.requestHandler).LastPostedJson);
    }

    [UnityTest]
    public IEnumerator Test_ProcessModelFolder_Get_Success_Logs()
    {
        bool callbackCalled = false;

        string testUrl = "http://test.url/getmodel";

        // Perform a GET request and wait for the response
        yield return toolWindow.requestHandler.Get(testUrl,
            onSuccess: (response) =>
            {
                // Log success and mark callback as called
                Debug.Log("ProcessModelFolder GET Success: " + response);
                callbackCalled = true;
            },
            onError: (error) =>
            {
                // Log any errors from the GET request
                Debug.LogError("ProcessModelFolder GET Error: " + error);
            });

        // Verify the expected success log was printed
        LogAssert.Expect(LogType.Log, "ProcessModelFolder GET Success: {\"config\":\"mocked\"}");
        // Assert that the success callback was executed
        Assert.IsTrue(callbackCalled);
        // Confirm that the GET URL matches what was recorded in the mock handler
        Assert.AreEqual(testUrl, ((WebRequestHandler)toolWindow.requestHandler).LastGetUrl);
    }

    [UnityTest]
    public IEnumerator Test_ProcessModelFolder_Delete_Success_Logs()
    {
        bool callbackCalled = false;

        string testUrl = "http://test.url/deletemodel";

        // Perform a DELETE request and wait for the response
        yield return toolWindow.requestHandler.Delete(testUrl,
            onSuccess: () =>
            {
                // Log success and mark callback as called
                Debug.Log("ProcessModelFolder DELETE Success");
                callbackCalled = true;
            },
            onError: (error) =>
            {
                // Log any errors from the DELETE request
                Debug.LogError("ProcessModelFolder DELETE Error: " + error);
            });

        // Verify the expected success log was printed
        LogAssert.Expect(LogType.Log, "ProcessModelFolder DELETE Success");
        // Assert that the success callback was executed
        Assert.IsTrue(callbackCalled);
        // Confirm that the DELETE URL matches what was recorded in the mock handler
        Assert.AreEqual(testUrl, ((WebRequestHandler)toolWindow.requestHandler).LastDeleteUrl);
    }
}
