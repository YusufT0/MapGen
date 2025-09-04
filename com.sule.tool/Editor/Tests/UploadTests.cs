using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

public class UploadTests
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
        // Destroy the toolWindow instance after each test to clean up
        if (toolWindow != null)
            Object.DestroyImmediate(toolWindow);
    }

    [UnityTest]
    public IEnumerator Test_Upload_PostJson_Success_Logs()
    {
        bool callbackCalled = false;

        string uploadUrl = "http://test.url/upload";
        string jsonPayload = "{\"file\":\"dummydata\"}";

        // Call the PostJson method and wait for the coroutine to finish
        yield return toolWindow.requestHandler.PostJson(uploadUrl, jsonPayload,
            onSuccess: (response) =>
            {
                // Log success response and mark callback as called
                Debug.Log("Upload POST Success: " + response);
                callbackCalled = true;
            },
            onError: (error) =>
            {
                // Log error if request fails
                Debug.LogError("Upload POST Error: " + error);
            });

        // Assert that the expected success log was output
        LogAssert.Expect(LogType.Log, "Upload POST Success: {\"status\":\"ok\"}");
        // Assert that the success callback was actually called
        Assert.IsTrue(callbackCalled);

        // Verify that the JSON payload sent matches the mock handler's recorded value
        Assert.AreEqual(jsonPayload, ((WebRequestHandler)toolWindow.requestHandler).LastPostedJson);
    }
}
