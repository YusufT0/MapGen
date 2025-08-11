using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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
        LogAssert.Expect(LogType.Error, "JSON can not parsed: {invalid json}");

        string invalidJson = "{invalid json}";
        string result = MyToolWindow.ParsePathFromJson(invalidJson);
        Assert.IsNull(result);
    }

}