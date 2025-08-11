using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

internal class TestableConfigManager : MyToolWindow
{
    //CreateConfig k�sm�
    public bool SendCreateConfigsRequestCalled = false;
    public string PassedJson = null;

    internal override IEnumerator SendCreateConfigsRequest(string json)
    {
        SendCreateConfigsRequestCalled = true;
        PassedJson = json;
        yield return null;  // Coroutine sim�lasyonu
    }


    //ShowConfig k�sm�
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

        // JSON olu�turuyoruz
        string expectedJson = "{\"obj_path\":\"model.fbx\",\"mtl_path\":\"material.mtl\",\"config_path\":\"config.yaml\"}";

        // Fieldlara de�er verelim
        manager.modelPath = "model.fbx";
        manager.mtlPath = "material.mtl";
        manager.configPath = "config.yaml";

        // Burada buton t�klama sim�lasyonu yap�labilir
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
        manager.DirectoryExists = false; // klas�r yokmu� gibi sim�le ediyoruz

        // Beklenen hata mesaj�
        LogAssert.Expect(LogType.Error, "Invalid path or folder does not exists: C:/MockFolder");

        yield return manager.ShowConfigsCoroutine();

        // Burada OpenFolderInExplorer �a�r�lmad���n� test etmek i�in ek kontrol olabilir
        Assert.IsFalse(manager.OpenFolderCalled);
    }
}
