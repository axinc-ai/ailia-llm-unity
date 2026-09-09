# ailia LLM Unity Package

!! CAUTION !!
“ailia” IS NOT OPEN SOURCE SOFTWARE (OSS).
As long as user complies with the conditions stated in [License Document](https://ailia.ai/license/), user may use the Software for free of charge, but the Software is basically paid software.

## About ailia LLM

ailia LLM is a library for running LLMs on edge devices. It provides bindings for C++ and Unity.

## Tool Use (JSON API)

Use `ailiaLLMSetPromptJson` from the first user message while tools are configured.
The legacy `ailiaLLMSetPrompt` and `ailiaLLMSetMultimodalPrompt` return INVALID_STATE until tools are cleared.
Define tools with `ailiaLLMSetTools`, set the JSON messages array, and call `ailiaLLMGenerate` until done.
Deltas remain available through `ailiaLLMGetDeltaText` for previews; retrieving or concatenating them is optional.
Retrieve assistant JSON with `ailiaLLMGetResponseJsonSize` / `ailiaLLMGetResponseJson`, append the object to history,
execute complete calls, and append tool results with matching `tool_call_id`. Set the next JSON prompt to continue.

The SDK accumulates generated output. Reads do not consume it; a new prompt resets it.

### Structured history

```json
[
 {"role":"user","content":"Weather in Tokyo?"},
 {"role":"assistant","content":"","tool_calls":[
  {"id":"call_0","type":"function","function":{"name":"get_weather","arguments":"{\"city\":\"Tokyo\"}"}}
 ]},
 {"role":"tool","tool_call_id":"call_0","content":"Snow, -3 C"}
]
```

Content is not reparsed as tool syntax. Supply assistant thinking in `reasoning_content`.
Arguments must be a string containing a JSON object. Call IDs/names must be nonempty; IDs are unique per assistant,
but may be reused in later turns. Consecutive tool results match the preceding assistant by ID, not order.
Unknown IDs, duplicate results and conflicting names are rejected. Tool content must be a string; serialize objects first.

### Images and audio

Load a compatible projector with `ailiaLLMOpenMultimodalProjectorFileA` and use ordered user content parts:

```json
[{"role":"user","content":[
 {"type":"text","text":"Describe this image and audio."},
 {"type":"image","file_path":"/path/to/image.jpg"},
 {"type":"audio","file_path":"/path/to/audio.wav"}
]}]
```

Each image/audio part specifies exactly one of `file_path` or `data`. Data is standard padded Base64 containing
encoded file bytes (JPEG/PNG, WAV/MP3/FLAC, etc.). No URL fetching, video or raw RGB/PCM is supported.
Media works with or without tools and is re-evaluated each prompt. An unloaded/incompatible projector returns INVALID_STATE.
Query image/audio support with `ailiaLLMGetMultimodalCapabilities`.

### Incomplete output and errors

GetResponseJson rejects unfinished calls/thinking with PARSE_ERROR. It never repairs arguments or returns executable partial results.
SetPromptJson rejects incomplete arguments, invalid JSON/types/ID associations and empty arrays with INVALID_ARGUMENT.
If GetResponseJson returns PARSE_ERROR, do not append the failed assistant. Append a retry user message to the existing history; no partial content/reasoning retrieval is needed. Do not invent tool results.

After clearing tools with SetTools(NULL), SetPromptJson still accepts historical tool calls and results for summaries or final answers. Retrieve response JSON before clearing tools, which invalidates the output parser. Use finally to clear tools even when an exception occurs.
An unset prompt or changed tools/thinking returns INVALID_STATE; before generation an initialized prompt returns an empty assistant.
File failures return ERROR_FILE_API, invalid Base64 returns INVALID_ARGUMENT, and undecodable media buffers return ERROR_BUFFER_API.

```csharp
if (!llm.SetPromptJson(messagesJson)) return;
bool done = false;
while (!done) { if (!llm.Generate(ref done)) return; }
string responseJson = llm.GetResponseJson();
// Empty string signals error. Otherwise append the object and ID-linked tool results.
```


## API specification

https://github.com/ailia-ai/ailia-sdk

