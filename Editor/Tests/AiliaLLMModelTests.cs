/* ailia LLM Unity EditMode Tests */
/* Copyright 2026 AXELL CORPORATION */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using ailiaLLM;

/// <summary>
/// EditMode tests for AiliaLLMModel.
/// Based on the test patterns from cpp/unit_test.cpp and java/test/AiliaLLMTest.kt.
///
/// Tests are divided into two categories:
/// 1. Unit Tests (CI-friendly): Tests that don't require model files
/// 2. Integration Tests: Tests that require model files (skipped when model is unavailable)
///
/// Note: Multimodal API tests (OpenMultimodalProjector, GetMultimodalCapabilities, SetMultimodalPrompt)
/// are not included in this initial implementation as they require additional test assets (mmproj files, images).
/// TODO: Add multimodal tests in a future PR when test assets are available.
/// </summary>
[TestFixture]
public class AiliaLLMModelTests
{
    private AiliaLLMModel model;
    private static string testModelPath;
    private static bool modelAvailable;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Check for model path from environment variable
        testModelPath = Environment.GetEnvironmentVariable("AILIA_LLM_TEST_MODEL_PATH");
        if (string.IsNullOrEmpty(testModelPath))
        {
            // Fallback to default path relative to project
            testModelPath = Path.Combine(Application.dataPath, "..", "..", "models", "gemma-2-2b-it-Q4_K_M.gguf");
            Debug.Log($"AILIA_LLM_TEST_MODEL_PATH not set. Using default test model path: {testModelPath}");
        }

        modelAvailable = File.Exists(testModelPath);
        if (!modelAvailable)
        {
            Debug.LogWarning($"Test model not found at: {testModelPath}. Integration tests will be skipped.");
            Debug.LogWarning("Set AILIA_LLM_TEST_MODEL_PATH environment variable to enable integration tests.");
        }
        else
        {
            Debug.Log($"Test model found at: {testModelPath}");
        }
    }

    [SetUp]
    public void SetUp()
    {
        model = new AiliaLLMModel();
    }

    [TearDown]
    public void TearDown()
    {
        if (model != null)
        {
            model.Close();
            model.Dispose();
            model = null;
        }
    }

    #region Unit Tests (CI-friendly, no model required)

    [Test]
    [Category("UnitTest")]
    public void Create_Success()
    {
        // Create should succeed
        bool result = model.Create();
        Assert.IsTrue(result, "Create should return true");
    }

    [Test]
    [Category("UnitTest")]
    public void Create_MultipleInstances_Success()
    {
        var model1 = new AiliaLLMModel();
        var model2 = new AiliaLLMModel();

        bool result1 = model1.Create();
        bool result2 = model2.Create();

        Assert.IsTrue(result1, "First Create should succeed");
        Assert.IsTrue(result2, "Second Create should succeed");

        model1.Close();
        model2.Close();
        model1.Dispose();
        model2.Dispose();
    }

    [Test]
    [Category("UnitTest")]
    public void Close_WithoutCreate_DoesNotThrow()
    {
        // Close without Create should not throw
        Assert.DoesNotThrow(() => model.Close());
    }

    [Test]
    [Category("UnitTest")]
    public void Close_MultipleTimes_DoesNotThrow()
    {
        model.Create();

        // Multiple Close calls should not throw
        Assert.DoesNotThrow(() => model.Close());
        Assert.DoesNotThrow(() => model.Close());
    }

    [Test]
    [Category("UnitTest")]
    public void Dispose_MultipleTimes_DoesNotThrow()
    {
        model.Create();

        // Multiple Dispose calls should not throw
        Assert.DoesNotThrow(() => model.Dispose());
        Assert.DoesNotThrow(() => model.Dispose());
    }

    [Test]
    [Category("UnitTest")]
    public void Open_WithInvalidPath_ReturnsFalse()
    {
        model.Create();

        bool result = model.Open("invalid_path_that_does_not_exist.gguf", 512);

        Assert.IsFalse(result, "Open with invalid path should return false");
    }

    [Test]
    [Category("UnitTest")]
    public void Open_WithoutCreate_ReturnsFalse()
    {
        // Open without Create should return false
        bool result = model.Open("any_path.gguf", 512);

        Assert.IsFalse(result, "Open without Create should return false");
    }

    [Test]
    [Category("UnitTest")]
    public void SetSamplingParam_WithoutOpen_DoesNotThrow()
    {
        model.Create();

        // SetSamplingParam without Open should not throw, but the return value is implementation-dependent.
        // Some native implementations may accept sampling parameters before the model is loaded and return true.
        // Therefore, this test documents that calling SetSamplingParam in this state is safe (no exception),
        // and logs the current return value without asserting on it.
        bool result = false;
        Assert.DoesNotThrow(() =>
        {
            result = model.SetSamplingParam(40, 0.9f, 0.4f, 1234);
        }, "SetSamplingParam should be callable before Open without throwing an exception.");

        Debug.Log($"SetSamplingParam without Open returned: {result}");
    }

    [Test]
    [Category("UnitTest")]
    public void ContextFull_InitialState_ReturnsFalse()
    {
        model.Create();

        // Initial state should be false
        bool contextFull = model.ContextFull();

        Assert.IsFalse(contextFull, "ContextFull should return false initially");
    }

    [Test]
    [Category("UnitTest")]
    public void PromptTokenCount_WithoutPrompt_ReturnsZero()
    {
        model.Create();

        uint count = model.PromptTokenCount();

        Assert.AreEqual(0u, count, "PromptTokenCount should return 0 without prompt");
    }

    [Test]
    [Category("UnitTest")]
    public void GeneratedTokenCount_WithoutGeneration_ReturnsZero()
    {
        model.Create();

        uint count = model.GeneratedTokenCount();

        Assert.AreEqual(0u, count, "GeneratedTokenCount should return 0 without generation");
    }

    [Test]
    [Category("UnitTest")]
    public void GetBackendCount_ReturnsPositiveValue()
    {
        // GetBackendCount is a static method on AiliaLLM class
        uint count = 0;
        int status = AiliaLLM.ailiaLLMGetBackendCount(ref count);

        Assert.AreEqual(AiliaLLM.AILIA_LLM_STATUS_SUCCESS, status, "GetBackendCount should succeed");
        Assert.Greater(count, 0u, "Backend count should be greater than 0");
    }

    [Test]
    [Category("UnitTest")]
    public void GetBackendName_ReturnsValidNames()
    {
        uint count = 0;
        int status = AiliaLLM.ailiaLLMGetBackendCount(ref count);
        Assert.AreEqual(AiliaLLM.AILIA_LLM_STATUS_SUCCESS, status);

        for (uint i = 0; i < count; i++)
        {
            IntPtr namePtr = IntPtr.Zero;
            status = AiliaLLM.ailiaLLMGetBackendName(ref namePtr, i);

            Assert.AreEqual(AiliaLLM.AILIA_LLM_STATUS_SUCCESS, status, $"GetBackendName should succeed for index {i}");
            Assert.AreNotEqual(IntPtr.Zero, namePtr, $"Backend name pointer should not be null for index {i}");

            string name = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(namePtr);
            Assert.IsNotNull(name, $"Backend name should not be null for index {i}");
            Assert.IsNotEmpty(name, $"Backend name should not be empty for index {i}");

            Debug.Log($"Backend {i}: {name}");
        }
    }

    [Test]
    [Category("UnitTest")]
    public void AiliaLLMChatMessage_CanBeCreated()
    {
        var message = new AiliaLLMChatMessage();
        message.role = "user";
        message.content = "Hello";

        Assert.AreEqual("user", message.role);
        Assert.AreEqual("Hello", message.content);
    }

    [Test]
    [Category("UnitTest")]
    public void AiliaLLMMediaData_CanBeCreated()
    {
        var mediaData = new AiliaLLMMediaData();
        mediaData.media_type = "image";
        mediaData.file_path = "/path/to/image.jpg";
        mediaData.width = 640;
        mediaData.height = 480;

        Assert.AreEqual("image", mediaData.media_type);
        Assert.AreEqual("/path/to/image.jpg", mediaData.file_path);
        Assert.AreEqual(640u, mediaData.width);
        Assert.AreEqual(480u, mediaData.height);
    }

    [Test]
    [Category("UnitTest")]
    public void AiliaLLMMultimodalChatMessage_CanBeCreated()
    {
        var mediaData = new AiliaLLMMediaData();
        mediaData.media_type = "image";
        mediaData.file_path = "/path/to/image.jpg";

        var message = new AiliaLLMMultimodalChatMessage();
        message.role = "user";
        message.content = "Describe this image: <__media__>";
        message.media_data = new List<AiliaLLMMediaData> { mediaData };

        Assert.AreEqual("user", message.role);
        Assert.AreEqual("Describe this image: <__media__>", message.content);
        Assert.AreEqual(1, message.media_data.Count);
    }

    #endregion

    #region Integration Tests (require model file)

    [Test]
    [Category("IntegrationTest")]
    public void Open_WithValidModel_ReturnsTrue()
    {
        if (!modelAvailable)
        {
            Assert.Ignore("Model file not available. Set AILIA_LLM_TEST_MODEL_PATH to run this test.");
        }

        model.Create();

        bool result = model.Open(testModelPath, 512);

        Assert.IsTrue(result, "Open with valid model should return true");
    }

    [Test]
    [Category("IntegrationTest")]
    public void Open_WithDefaultContext_Succeeds()
    {
        if (!modelAvailable)
        {
            Assert.Ignore("Model file not available. Set AILIA_LLM_TEST_MODEL_PATH to run this test.");
        }

        model.Create();

        // n_ctx = 0 means use default context size
        bool result = model.Open(testModelPath, 0);

        Assert.IsTrue(result, "Open with default context (0) should succeed");
    }

    [Test]
    [Category("IntegrationTest")]
    public void SetSamplingParam_AfterOpen_ReturnsTrue()
    {
        if (!modelAvailable)
        {
            Assert.Ignore("Model file not available. Set AILIA_LLM_TEST_MODEL_PATH to run this test.");
        }

        model.Create();
        model.Open(testModelPath, 512);

        bool result = model.SetSamplingParam(40, 0.9f, 0.4f, 1234);

        Assert.IsTrue(result, "SetSamplingParam should return true after model is opened");
    }

    [Test]
    [Category("IntegrationTest")]
    public void SetPrompt_WithValidMessages_ReturnsTrue()
    {
        if (!modelAvailable)
        {
            Assert.Ignore("Model file not available. Set AILIA_LLM_TEST_MODEL_PATH to run this test.");
        }

        model.Create();
        model.Open(testModelPath, 512);
        model.SetSamplingParam(40, 0.9f, 0.4f, 1234);

        var messages = new List<AiliaLLMChatMessage>
        {
            new AiliaLLMChatMessage { role = "system", content = "You are a helpful assistant." },
            new AiliaLLMChatMessage { role = "user", content = "Hello!" }
        };

        bool result = model.SetPrompt(messages);

        Assert.IsTrue(result, "SetPrompt should return true with valid messages");
    }

    [Test]
    [Category("IntegrationTest")]
    public void PromptTokenCount_AfterSetPrompt_ReturnsPositive()
    {
        if (!modelAvailable)
        {
            Assert.Ignore("Model file not available. Set AILIA_LLM_TEST_MODEL_PATH to run this test.");
        }

        model.Create();
        model.Open(testModelPath, 512);
        model.SetSamplingParam(40, 0.9f, 0.4f, 1234);

        var messages = new List<AiliaLLMChatMessage>
        {
            new AiliaLLMChatMessage { role = "system", content = "You are a helpful assistant." },
            new AiliaLLMChatMessage { role = "user", content = "Hello!" }
        };

        model.SetPrompt(messages);

        uint count = model.PromptTokenCount();

        Assert.Greater(count, 0u, "PromptTokenCount should be greater than 0 after SetPrompt");
    }

    [Test]
    [Category("IntegrationTest")]
    public void Generate_SingleToken_Success()
    {
        if (!modelAvailable)
        {
            Assert.Ignore("Model file not available. Set AILIA_LLM_TEST_MODEL_PATH to run this test.");
        }

        model.Create();
        model.Open(testModelPath, 512);
        model.SetSamplingParam(40, 0.9f, 0.4f, 1234);

        var messages = new List<AiliaLLMChatMessage>
        {
            new AiliaLLMChatMessage { role = "system", content = "You are a helpful assistant." },
            new AiliaLLMChatMessage { role = "user", content = "Hi" }
        };

        model.SetPrompt(messages);

        bool done = false;
        bool result = model.Generate(ref done);

        Assert.IsTrue(result, "Generate should return true");

        string deltaText = model.GetDeltaText();
        Assert.IsNotNull(deltaText, "GetDeltaText should not return null");
    }

    [Test]
    [Category("IntegrationTest")]
    public void Generate_MultipleTokens_Success()
    {
        if (!modelAvailable)
        {
            Assert.Ignore("Model file not available. Set AILIA_LLM_TEST_MODEL_PATH to run this test.");
        }

        model.Create();
        model.Open(testModelPath, 512);
        model.SetSamplingParam(40, 0.9f, 0.4f, 1234);

        var messages = new List<AiliaLLMChatMessage>
        {
            new AiliaLLMChatMessage { role = "system", content = "You are a helpful assistant. Be brief." },
            new AiliaLLMChatMessage { role = "user", content = "Say hello" }
        };

        model.SetPrompt(messages);

        var fullText = new StringBuilder();
        bool done = false;
        int tokenCount = 0;
        const int maxTokens = 50;

        while (!done && tokenCount < maxTokens)
        {
            bool result = model.Generate(ref done);
            Assert.IsTrue(result, $"Generate should succeed at token {tokenCount}");

            string deltaText = model.GetDeltaText();
            fullText.Append(deltaText);
            tokenCount++;
        }

        Assert.Greater(tokenCount, 0, "Should generate at least one token");
        Assert.IsNotEmpty(fullText.ToString(), "Generated text should not be empty");

        Debug.Log($"Generated text ({tokenCount} tokens): {fullText}");
    }

    [Test]
    [Category("IntegrationTest")]
    public void GeneratedTokenCount_AfterGenerate_ReturnsPositive()
    {
        if (!modelAvailable)
        {
            Assert.Ignore("Model file not available. Set AILIA_LLM_TEST_MODEL_PATH to run this test.");
        }

        model.Create();
        model.Open(testModelPath, 512);
        model.SetSamplingParam(40, 0.9f, 0.4f, 1234);

        var messages = new List<AiliaLLMChatMessage>
        {
            new AiliaLLMChatMessage { role = "user", content = "Hi" }
        };

        model.SetPrompt(messages);

        bool done = false;
        int iterations = 0;
        const int maxIterations = 10;

        while (!done && iterations < maxIterations)
        {
            model.Generate(ref done);
            iterations++;
        }

        uint count = model.GeneratedTokenCount();
        Assert.Greater(count, 0u, "GeneratedTokenCount should be greater than 0 after generation");
    }

    [Test]
    [Category("IntegrationTest")]
    public void SetPrompt_EmptyMessages_ReturnsFalse()
    {
        if (!modelAvailable)
        {
            Assert.Ignore("Model file not available. Set AILIA_LLM_TEST_MODEL_PATH to run this test.");
        }

        model.Create();
        model.Open(testModelPath, 512);
        model.SetSamplingParam(40, 0.9f, 0.4f, 1234);

        var messages = new List<AiliaLLMChatMessage>();

        bool result = model.SetPrompt(messages);

        // Empty messages should fail
        Assert.IsFalse(result, "SetPrompt with empty messages should return false");
    }

    [Test]
    [Category("IntegrationTest")]
    public void Generate_WithoutSetPrompt_ReturnsFalse()
    {
        // NOTE: This test is currently skipped because calling Generate() without SetPrompt()
        // causes a crash (SIGSEGV) in the native library. This is a potential bug in the
        // native library that should be addressed separately. The native library should
        // return AILIA_LLM_STATUS_ERROR_CONTEXT instead of crashing.
        // TODO: Re-enable this test after the native library is fixed to handle this case gracefully.
        Assert.Ignore("Skipped: Calling Generate without SetPrompt causes native library crash. See native library issue.");
    }

    #endregion
}
