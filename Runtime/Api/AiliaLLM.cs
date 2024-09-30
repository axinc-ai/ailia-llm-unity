/* ailia LLM Unity Plugin Native Interface */
/* Copyright 2024 AXELL CORPORATION */

using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Runtime.InteropServices;

namespace ailiaLLM{
public class AiliaLLM
{

    /* Native Binary 定義 */

    #if (UNITY_IPHONE && !UNITY_EDITOR) || (UNITY_WEBGL && !UNITY_EDITOR)
        public const String LIBRARY_NAME="__Internal";
    #else
        #if (UNITY_ANDROID && !UNITY_EDITOR) || (UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX)
            public const String LIBRARY_NAME="ailia_llm";
        #else
            public const String LIBRARY_NAME="ailia_llm";
        #endif
    #endif

    [StructLayout(LayoutKind.Sequential)]
    public class AILIAChatMessage {
        public IntPtr role;
        public IntPtr content;
    }

    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaLLMCreate(ref IntPtr net, int n_ctx);

    #if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
        [DllImport(LIBRARY_NAME, EntryPoint = "ailiaLLMOpenModelFileW", CharSet=CharSet.Unicode)]
        public static extern int ailiaLLMOpenModelFile(IntPtr net, string path);
    #else
        [DllImport(LIBRARY_NAME, EntryPoint = "ailiaLLMOpenModelFileA", CharSet=CharSet.Ansi)]
        public static extern int ailiaLLMOpenModelFile(IntPtr net, string path);
    #endif

    #if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
        [DllImport(LIBRARY_NAME, EntryPoint = "ailiaLLMOpenTemplateFileW", CharSet=CharSet.Unicode)]
        public static extern int ailiaLLMOpenTemplateFile(IntPtr net, string path);
    #else
        [DllImport(LIBRARY_NAME, EntryPoint = "ailiaLLMOpenTemplateFileA", CharSet=CharSet.Ansi)]
        public static extern int ailiaLLMOpenTemplateFile(IntPtr net, string path);
    #endif

    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaLLMSetPrompt(IntPtr net, IntPtr messages, uint messages_len);

    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaLLMGenerate(IntPtr net, ref bool done);

    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaLLMGetDeltaTextSize(IntPtr net, ref uint len);

    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaLLMGetDeltaText(IntPtr net, IntPtr text, uint len);

    [DllImport(LIBRARY_NAME)]
    public static extern void ailiaLLMDestroy(IntPtr net);
}
} // namespace ailiaLLM
