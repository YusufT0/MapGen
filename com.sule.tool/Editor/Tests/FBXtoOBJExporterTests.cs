using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.TestTools;

public class FBXtoOBJExporterTests
{
    [Test]
    public void TestFBXtoOBJExporterWithInvalidPath()
    {
        // Arrange: Define an invalid FBX file path outside the Assets folder
        string invalidPath = "nonexistent.fbx";
        string expectedFullPath = Path.Combine(Application.dataPath, invalidPath);

        // Expect an error log indicating the invalid relative path
        LogAssert.Expect(LogType.Error, $"Relative path is invalid and not found inside Assets: {expectedFullPath}");

        // Act: Call the ConvertExternalFBX method with the invalid path
        var result = FBXtoOBJExporter.ConvertExternalFBX(invalidPath);

        // Assert: Resulting obj and mtl paths should be null due to invalid input
        Assert.IsNull(result.objPath);
        Assert.IsNull(result.mtlPath);
    }

    [Test]
    public void TestFBXtoOBJExporterWithRelativePath()
    {
        // Arrange: Define a relative path within the Assets folder (assumed invalid for the test)
        string relativePath = "Assets/Test.fbx";
        string expectedFullPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, relativePath);

        // Expect an error log indicating the invalid relative path
        LogAssert.Expect(LogType.Error, $"Relative path is invalid and not found inside Assets: {expectedFullPath}");

        // Act: Call the ConvertExternalFBX method with the relative path
        var result = FBXtoOBJExporter.ConvertExternalFBX(relativePath);

        // Assert: Resulting obj and mtl paths should be null due to invalid input
        Assert.IsNull(result.objPath);
        Assert.IsNull(result.mtlPath);
    }

    [Test]
    public void TestFBXtoOBJExporterWithNonexistentRelativePath()
    {
        // Arrange: Define a relative path to a nonexistent FBX file inside Assets
        string relativePath = "NonexistentFolder/Test.fbx";
        string expectedFullPath = Path.Combine(Application.dataPath, relativePath);

        // Expect an error log indicating the invalid relative path
        LogAssert.Expect(LogType.Error, $"Relative path is invalid and not found inside Assets: {expectedFullPath}");

        // Act: Call the ConvertExternalFBX method with the nonexistent relative path
        var result = FBXtoOBJExporter.ConvertExternalFBX(relativePath);

        // Assert: Resulting obj and mtl paths should be null due to invalid input
        Assert.IsNull(result.objPath);
        Assert.IsNull(result.mtlPath);
    }
}
