using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;

public class JsonParseTests
{

    //Geçerli JSON verildiðinde "path" deðeri doðru dönüyor mu?
    [Test]
    public void ParsePathFromJson_ValidJson_ReturnsPath()
    {
        //örnek (dummy) bir JSON verisi
        string validJson = "{\"path\":\"C:/Users/Test\"}";

        string result = MyToolWindow.ParsePathFromJson(validJson);  // Test sýrasýnda fonksiyon public static yapýldý
        Assert.AreEqual("C:/Users/Test", result);
    }

    //Geçersiz JSON verildiðinde null dönüyor mu? 
    [Test]
    public void ParsePathFromJson_InvalidJson_ReturnsNull()
    {
        LogAssert.Expect(LogType.Error, new Regex("JSON could not be parsed.*"));

        string invalidJson = "{invalid json}";
        string result = MyToolWindow.ParsePathFromJson(invalidJson);
        Assert.IsNull(result);
    }

}