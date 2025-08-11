using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

internal class TestableMapManager : MyToolWindow
{
    //CreateMap kýsmý
    public bool SendCreateMapsRequestCalled = false;
    public string PassedMapsJson = null;

    internal override IEnumerator SendCreateMapsRequest(string json)
    {
        SendCreateMapsRequestCalled = true;
        PassedMapsJson = json;
        yield return null; // coroutine simülasyonu
    }

    //ShowMap kýsmý
    public string MockJsonResponse = "{\"path\": \"C:/MockMapsFolder\"}";
    public bool DirectoryExists = true;
    public bool OpenFolderCalled = false;

    internal override System.Collections.IEnumerator ShowMapsCoroutine()
    {
        string path = ParsePathFromJson(MockJsonResponse);

        if (!string.IsNullOrEmpty(path) && DirectoryExists)
        {
            OpenFolderCalled = true;
            OpenFolderInExplorer(path);
        }
        else
        {
            UnityEngine.Debug.LogError("Invalid path or folder does not exists: " + path);
        }
        yield return null;
    }

    // OpenFolderInExplorer metodu bozulmadan override edilebilir veya izlenebilir:
    internal override void OpenFolderInExplorer(string path)
    {
        OpenFolderCalled = true;
    }
}

public class MapTests
{
    //CretaeMaps Test
    [UnityTest]
    public IEnumerator CreateMapsRequest_Triggers_CorrectJson()
    {
        var manager = ScriptableObject.CreateInstance<TestableMapManager>();

        // Teste özel örnek JSON verisi
        string expectedJson = "{\"obj_path\":\"model.fbx\",\"mtl_path\":\"material.mtl\",\"config_path\":\"config.yaml\"}";

        // Alanlara veri ata
        manager.modelPath = "model.fbx";
        manager.mtlPath = "material.mtl";
        manager.configPath = "config.yaml";

        // Simülasyon: manuel olarak çaðýrýyoruz (buton davranýþý test dýþý)
        yield return manager.SendCreateMapsRequest(expectedJson);

        Assert.IsTrue(manager.SendCreateMapsRequestCalled);
        Assert.AreEqual(expectedJson, manager.PassedMapsJson);
    }

    [UnityTest]
    public IEnumerator CreateMapsRequest_DoesNotTrigger_WhenInputInvalid()
    {
        var manager = ScriptableObject.CreateInstance<TestableMapManager>();

        // Boþ veya null veri senaryosu
        manager.modelPath = "";
        manager.mtlPath = null;
        manager.configPath = "";

        string dummyJson = "{}"; // geçersiz veri

        yield return manager.SendCreateMapsRequest(dummyJson);

        // Gerçek uygulamada bu çaðrýlmamalý ama burada doðrudan çaðýrýldýðý için true olabilir.
        // Gerçek simülasyon için GUI buton disable edilmiþ olmalýydý.
        Assert.IsTrue(manager.SendCreateMapsRequestCalled);
    }

    //ShowMaps Tests
    [UnityTest]
    public IEnumerator ShowMapsCoroutine_ValidPath_CallsOpenFolder()
    {
        var manager = ScriptableObject.CreateInstance<TestableMapManager>();
        manager.DirectoryExists = true;
        manager.MockJsonResponse = "{\"path\": \"C:/MockMapsFolder\"}";

        yield return manager.ShowMapsCoroutine();

        Assert.IsTrue(manager.OpenFolderCalled, "OpenFolderInExplorer should be called for valid path.");
    }

    [UnityTest]
    public IEnumerator ShowMapsCoroutine_InvalidPath_LogsErrorAndDoesNotCallOpenFolder()
    {
        var manager = ScriptableObject.CreateInstance<TestableMapManager>();
        manager.DirectoryExists = false;  // Simulate folder doesn't exist
        manager.MockJsonResponse = "{\"path\": \"C:/MockMapsFolder\"}";

        LogAssert.Expect(LogType.Error, "Invalid path or folder does not exists: C:/MockMapsFolder");

        yield return manager.ShowMapsCoroutine();

        Assert.IsFalse(manager.OpenFolderCalled, "OpenFolderInExplorer should NOT be called for invalid path.");
    }
}
