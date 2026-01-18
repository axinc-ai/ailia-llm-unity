/* ailia LLM model unit tests */
/* Copyright 2025 AXELL CORPORATION */

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using ailiaLLM;

namespace ailiaLLM.Tests
{
    [TestFixture]
    public class AiliaLLMModelTests
    {
        private AiliaLLMModel model;
        private string modelPath;
        private bool modelAvailable;

        [SetUp]
        public void SetUp()
        {
            model = new AiliaLLMModel();

            // Check for model from environment variable
            modelPath = Environment.GetEnvironmentVariable("AILIA_LLM_MODEL_PATH") ?? "";
            modelAvailable = !string.IsNullOrEmpty(modelPath) && File.Exists(modelPath);
        }

        [TearDown]
        public void TearDown()
        {
            if (model != null)
            {
                model.Close();
                model.Dispose();
            }
        }

        [Test]
        public void PromptTokenCount_ReturnsZero_WhenNotInitialized()
        {
            // When LLM is not initialized, should return 0
            uint count = model.PromptTokenCount();
            Assert.AreEqual(0u, count);
        }

        [Test]
        public void GeneratedTokenCount_ReturnsZero_WhenNotInitialized()
        {
            // When LLM is not initialized, should return 0
            uint count = model.GeneratedTokenCount();
            Assert.AreEqual(0u, count);
        }

        [Test]
        public void PromptTokenCount_ReturnsPositiveValue_AfterSetPrompt()
        {
            if (!modelAvailable)
            {
                Assert.Ignore("Model not available. Set AILIA_LLM_MODEL_PATH environment variable.");
                return;
            }

            // Create and open model
            bool created = model.Create();
            Assert.IsTrue(created, "Failed to create LLM instance");

            bool opened = model.Open(modelPath, 2048);
            Assert.IsTrue(opened, "Failed to open model");

            // Set prompt
            var messages = new List<AiliaLLMChatMessage>
            {
                new AiliaLLMChatMessage { role = "user", content = "Hello, how are you?" }
            };
            bool promptSet = model.SetPrompt(messages);
            Assert.IsTrue(promptSet, "Failed to set prompt");

            // Check prompt token count
            uint count = model.PromptTokenCount();
            Assert.Greater(count, 0u, "Prompt token count should be greater than 0 after SetPrompt");
        }

        [Test]
        public void GeneratedTokenCount_ReturnsPositiveValue_AfterGenerate()
        {
            if (!modelAvailable)
            {
                Assert.Ignore("Model not available. Set AILIA_LLM_MODEL_PATH environment variable.");
                return;
            }

            // Create and open model
            bool created = model.Create();
            Assert.IsTrue(created, "Failed to create LLM instance");

            bool opened = model.Open(modelPath, 2048);
            Assert.IsTrue(opened, "Failed to open model");

            // Set sampling parameters
            model.SetSamplingParam(40, 0.9f, 0.4f, 1234);

            // Set prompt
            var messages = new List<AiliaLLMChatMessage>
            {
                new AiliaLLMChatMessage { role = "user", content = "Hello" }
            };
            bool promptSet = model.SetPrompt(messages);
            Assert.IsTrue(promptSet, "Failed to set prompt");

            // Generate at least one token
            bool done = false;
            int maxIterations = 10;
            int iterations = 0;

            while (!done && iterations < maxIterations)
            {
                bool success = model.Generate(ref done);
                if (!success)
                {
                    break;
                }
                model.GetDeltaText();
                iterations++;
            }

            // Check generated token count
            uint count = model.GeneratedTokenCount();
            Assert.Greater(count, 0u, "Generated token count should be greater than 0 after Generate");
        }

        [Test]
        public void TokenCounts_IncrementDuringGeneration()
        {
            if (!modelAvailable)
            {
                Assert.Ignore("Model not available. Set AILIA_LLM_MODEL_PATH environment variable.");
                return;
            }

            // Create and open model
            bool created = model.Create();
            Assert.IsTrue(created, "Failed to create LLM instance");

            bool opened = model.Open(modelPath, 2048);
            Assert.IsTrue(opened, "Failed to open model");

            // Set sampling parameters
            model.SetSamplingParam(40, 0.9f, 0.4f, 1234);

            // Set prompt
            var messages = new List<AiliaLLMChatMessage>
            {
                new AiliaLLMChatMessage { role = "user", content = "Count from 1 to 5" }
            };
            bool promptSet = model.SetPrompt(messages);
            Assert.IsTrue(promptSet, "Failed to set prompt");

            // Get initial prompt token count
            uint promptCount = model.PromptTokenCount();
            Assert.Greater(promptCount, 0u, "Prompt token count should be greater than 0");

            // Generate multiple tokens and verify count increases
            bool done = false;
            int maxIterations = 20;
            int iterations = 0;
            uint previousGeneratedCount = 0;

            while (!done && iterations < maxIterations)
            {
                bool success = model.Generate(ref done);
                if (!success)
                {
                    break;
                }
                model.GetDeltaText();

                uint currentGeneratedCount = model.GeneratedTokenCount();

                // Verify generated count is non-decreasing
                Assert.GreaterOrEqual(currentGeneratedCount, previousGeneratedCount,
                    "Generated token count should be non-decreasing");

                previousGeneratedCount = currentGeneratedCount;
                iterations++;
            }

            // Final generated count should be positive
            uint finalGeneratedCount = model.GeneratedTokenCount();
            Assert.Greater(finalGeneratedCount, 0u, "Final generated token count should be greater than 0");
        }
    }
}
