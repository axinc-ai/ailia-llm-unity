# ailia LLM Unity Package

!! CAUTION !!
“ailia” IS NOT OPEN SOURCE SOFTWARE (OSS).
As long as user complies with the conditions stated in [License Document](https://ailia.ai/license/), user may use the Software for free of charge, but the Software is basically paid software.

## About ailia LLM

ailia LLM is a library for running LLMs on edge devices. It provides bindings for C++ and Unity.

## Tool Use (Function Calling)

With models whose chat template supports tool calling (e.g. Gemma 4), pass OpenAI-compatible tool definitions with `AiliaLLMModel.SetTools` and convert the raw output into tool calls with `ParseResponse`. Keep the raw output as the `assistant` content of the history and return tool results as the content of `tool` messages (matched to the tool calls by order).

## API specification

https://github.com/ailia-ai/ailia-sdk

