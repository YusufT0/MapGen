using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class JsonParseTests
{
    [Test]
    public void TestParsePathFromJson()
    {
        // Test that a valid JSON string returns the correct path value
        string json = @"{""path"":""/test/path/file.obj""}";
        string result = MyToolWindow.ParsePathFromJson(json);

        Assert.AreEqual("/test/path/file.obj", result);
    }

    [Test]
    public void TestParseInvalidJson()
    {
        // Test that invalid JSON logs an error and returns null
        string invalidJson = @"{invalid:json}";

        LogAssert.Expect(LogType.Error, "JSON could not be parsed: {invalid:json}");

        string result = MyToolWindow.ParsePathFromJson(invalidJson);

        Assert.IsNull(result);
    }

    [Test]
    public void TestParseEmptyJson()
    {
        // Test that an empty JSON string logs an error and returns null
        string emptyJson = @"";

        LogAssert.Expect(LogType.Error, "JSON could not be parsed: ");

        string result = MyToolWindow.ParsePathFromJson(emptyJson);

        Assert.IsNull(result);
    }

    [Test]
    public void TestParseMalformedJson()
    {
        // Test that malformed JSON (missing closing bracket) logs an error and returns null
        string malformedJson = @"{""path"":""test""";

        LogAssert.Expect(LogType.Error, "JSON could not be parsed: {\"path\":\"test\"");

        string result = MyToolWindow.ParsePathFromJson(malformedJson);

        Assert.IsNull(result);
    }
}
