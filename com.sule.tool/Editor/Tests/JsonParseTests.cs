using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;

public class JsonParseTests
{

    //Ge�erli JSON verildi�inde "path" de�eri do�ru d�n�yor mu?
    [Test]
    public void ParsePathFromJson_ValidJson_ReturnsPath()
    {
        //�rnek (dummy) bir JSON verisi
        string validJson = "{\"path\":\"C:/Users/Test\"}";

        string result = MyToolWindow.ParsePathFromJson(validJson);  // Test s�ras�nda fonksiyon public static yap�ld�
        Assert.AreEqual("C:/Users/Test", result);
    }

    //Ge�ersiz JSON verildi�inde null d�n�yor mu? 
    [Test]
    public void ParsePathFromJson_InvalidJson_ReturnsNull()
    {
        LogAssert.Expect(LogType.Error, new Regex("JSON could not be parsed.*"));

        string invalidJson = "{invalid json}";
        string result = MyToolWindow.ParsePathFromJson(invalidJson);
        Assert.IsNull(result);
    }

}