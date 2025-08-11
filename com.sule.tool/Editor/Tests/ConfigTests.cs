using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

internal class TestableConfigManager : MyToolWindow
{
    //CreateConfig kýsmý
    public bool SendCreateConfigsRequestCalled = false;
    public string PassedJson = null;

    internal override IEnumerator SendCreateConfigsRequest(string json)
    {
        SendCreateConfigsRequestCalled = true;
        PassedJson = json;
        yield return null;  // Coroutine simülasyonu
    }


    //ShowConfig kýsmý
    public bool OpenFolderCalled = false;
    public string OpenedPath = null;
    public string MockJsonResponse = "{\"path\": \"C:/MockFolder\"}";
    public bool DirectoryExists = true;

    // ShowConfigsCoroutine override edilir
    internal override IEnumerator ShowConfigsCoroutine()
    {
        string path = ParsePathFromJson(MockJsonResponse);

        if (!string.IsNullOrEmpty(path) && DirectoryExists)
        {
            OpenFolderInExplorer(path);
        }
        else
        {
            UnityEngine.Debug.LogError("Invalid path or folder does not exists: " + path);
        }

        yield return null;
    }

    // OpenFolderInExplorer mock versiyonu
    internal override void OpenFolderInExplorer(string path)
    {
        OpenFolderCalled = true;
        OpenedPath = path;
    }
}

public class APITests
{
    //CreateConfig Test
    [UnityTest]
    public IEnumerator CreateConfigsButton_Triggers_SendCreateConfigsRequest()
    {
        var manager = ScriptableObject.CreateInstance<TestableConfigManager>();

        // JSON oluþturuyoruz
        string expectedJson = "{\"obj_path\":\"model.fbx\",\"mtl_path\":\"material.mtl\",\"config_path\":\"config.yaml\"}";

        // Fieldlara deðer verelim
        manager.modelPath = "model.fbx";
        manager.mtlPath = "material.mtl";
        manager.configPath = "config.yaml";

        // Burada buton týklama simülasyonu yapýlabilir
        yield return manager.SendCreateConfigsRequest(expectedJson);

        Assert.IsTrue(manager.SendCreateConfigsRequestCalled);
        Assert.AreEqual(expectedJson, manager.PassedJson);
    }

    //ShowConfig Tests
    [UnityTest]
    public IEnumerator ShowConfigsCoroutine_ValidPath_CallsOpenFolder()
    {
        // Arrange
        var testManager = new TestableConfigManager
        {
            DirectoryExists = true
        };

        // Act
        yield return testManager.ShowConfigsCoroutine();

        // Assert
        Assert.IsTrue(testManager.OpenFolderCalled);
        Assert.AreEqual("C:/MockFolder", testManager.OpenedPath);

    }

    [UnityTest]
    public IEnumerator ShowConfigsCoroutine_InvalidPath_DoesNotCallOpenFolder()
    {
        var manager = ScriptableObject.CreateInstance<TestableConfigManager>();

        manager.MockJsonResponse = "{\"path\": \"C:/MockFolder\"}";
        manager.DirectoryExists = false; // klasör yokmuþ gibi simüle ediyoruz

        // Beklenen hata mesajý
        LogAssert.Expect(LogType.Error, "Invalid path or folder does not exists: C:/MockFolder");

        yield return manager.ShowConfigsCoroutine();

        // Burada OpenFolderInExplorer çaðrýlmadýðýný test etmek için ek kontrol olabilir
        Assert.IsFalse(manager.OpenFolderCalled);
    }
}
