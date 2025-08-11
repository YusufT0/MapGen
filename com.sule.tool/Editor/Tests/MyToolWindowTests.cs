using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class MyToolWindowTests
{
    private MyToolWindow window;

    [SetUp]
    public void Setup()
    {
        window = ScriptableObject.CreateInstance<MyToolWindow>();
    }

    [Test]
    public void InputsFilled_WhenModelAndConfigPathAreSet_ReturnsTrue()
    {
        window.modelPath = "someModel.fbx";
        window.configPath = "someConfig.yaml";

        bool inputsFilled = !string.IsNullOrEmpty(window.modelPath) && !string.IsNullOrEmpty(window.configPath);

        Assert.IsTrue(inputsFilled);
    }

    [Test]
    public void InputsFilled_WhenModelOrConfigPathIsEmpty_ReturnsFalse()
    {
        window.modelPath = "";
        window.configPath = "someConfig.yaml";

        bool inputsFilled = !string.IsNullOrEmpty(window.modelPath) && !string.IsNullOrEmpty(window.configPath);

        Assert.IsFalse(inputsFilled);

        window.modelPath = "someModel.fbx";
        window.configPath = "";

        inputsFilled = !string.IsNullOrEmpty(window.modelPath) && !string.IsNullOrEmpty(window.configPath);

        Assert.IsFalse(inputsFilled);
    }
}
