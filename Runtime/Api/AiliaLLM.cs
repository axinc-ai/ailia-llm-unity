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

    /****************************************************************
    * ライブラリ状態定義
    **/

    /**
    * \~japanese
    * @def AILIA_LLM_STATUS_SUCCESS
    * @brief 成功
    *
    * \~english
    * @def AILIA_LLM_STATUS_SUCCESS
    * @brief Successful
    */
    public const int AILIA_LLM_STATUS_SUCCESS = (0);
    /**
    * \~japanese
    * @def AILIA_LLM_STATUS_INVALID_ARGUMENT
    * @brief 引数が不正
    * @remark API呼び出し時の引数を確認してください。
    *
    * \~english
    * @def AILIA_LLM_STATUS_INVALID_ARGUMENT
    * @brief Incorrect argument
    * @remark Please check argument of called API.
    */
    public const int AILIA_LLM_STATUS_INVALID_ARGUMENT = (-1);
    /**
    * \~japanese
    * @def AILIA_LLM_STATUS_ERROR_FILE_API
    * @brief ファイルアクセスに失敗した
    * @remark 指定したパスのファイルが存在するか、権限を確認してください。
    *
    * \~english
    * @def AILIA_LLM_STATUS_ERROR_FILE_API
    * @brief File access failed.
    * @remark Please check file is exist or not, and check access permission.
    */
    public const int AILIA_LLM_STATUS_ERROR_FILE_API = (-2);
    /**
    * \~japanese
    * @def AILIA_LLM_STATUS_INVALID_VERSION
    * @brief 構造体バージョンが不正
    * @remark API呼び出し時に指定した構造体バージョンを確認し、正しい構造体バージョンを指定してください。
    *
    * \~english
    * @def AILIA_LLM_STATUS_INVALID_VERSION
    * @brief Incorrect struct version
    * @remark Please check struct version that passed with API and please pass correct struct version.
    */
    public const int AILIA_LLM_STATUS_INVALID_VERSION = (-3);
    /**
    * \~japanese
    * @def AILIA_LLM_STATUS_BROKEN
    * @brief 壊れたファイルが渡された
    * @remark モデルファイルが破損していないかを確認し、正常なモデルを渡してください。
    *
    * \~english
    * @def AILIA_LLM_STATUS_BROKEN
    * @brief A corrupt file was passed.
    * @remark Please check model file are correct or not, and please pass correct model.
    */
    public const int AILIA_LLM_STATUS_BROKEN = (-4);
    /**
    * \~japanese
    * @def AILIA_LLM_STATUS_MEMORY_INSUFFICIENT
    * @brief メモリが不足している
    * @remark メインメモリやVRAMの空き容量を確保してからAPIを呼び出してください。
    *
    * \~english
    * @def AILIA_LLM_STATUS_MEMORY_INSUFFICIENT
    * @brief Insufficient memory
    * @remark Please check usage of main memory and VRAM. And please call API after free memory.
    */
    public const int AILIA_LLM_STATUS_MEMORY_INSUFFICIENT = (-5);
    /**
    * \~japanese
    * @def AILIA_LLM_STATUS_THREAD_ERROR
    * @brief スレッドの作成に失敗した
    * @remark スレッド数などシステムリソースを確認し、リソースを開放してからAPIを呼び出してください。
    *
    * \~english
    * @def AILIA_LLM_STATUS_THREAD_ERROR
    * @brief Thread creation failed.
    * @remark Please check usage of system resource = (e.g. thread). And please call API after release system  resources.
    */
    public const int AILIA_LLM_STATUS_THREAD_ERROR = (-6);
    /**
    * \~japanese
    * @def AILIA_LLM_STATUS_INVALID_STATE
    * @brief 内部状態が不正
    * @remark APIドキュメントを確認し、呼び出し手順が正しいかどうかを確認してください。
    *
    * \~english
    * @def AILIA_LLM_STATUS_INVALID_STATE
    * @brief The internal status is incorrect.
    * @remark Please check API document and API call steps.
    */
    public const int AILIA_LLM_STATUS_INVALID_STATE = (-7);
    /**
    * \~japanese
    * @def AILIA_LLM_STATUS_CONTEXT_FULL
    * @brief コンテキスト長を超えました
    * @remark SetPromptに与えるコンテキスト長を削減してください。
    *
    * \~english
    * @def AILIA_LLM_STATUS_CONTEXT_FULL
    * @brief Exceeded the context length.
    * @remark Please reduce the context length given to SetPrompt.
    */
    public const int AILIA_LLM_STATUS_CONTEXT_FULL = (-8);
    /**
    * \~japanese
    * @def AILIA_LLM_STATUS_UNIMPLEMENTED
    * @brief 未実装
    * @remark
    * 指定した環境では未実装な機能が呼び出されました。エラー内容をドキュメント記載のサポート窓口までお問い合わせください。
    *
    * \~english
    * @def AILIA_LLM_STATUS_UNIMPLEMENTED
    * @brief Unimplemented error
    * @remark The called API are not available on current environment. Please contact support desk that described on
    * document.
    */
    public const int AILIA_LLM_STATUS_UNIMPLEMENTED = (-15);
    /**
    * \~japanese
    * @def AILIA_LLM_STATUS_OTHER_ERROR
    * @brief 不明なエラー
    * @remark その他のエラーが発生しました。
    *
    * \~english
    * @def AILIA_LLM_STATUS_OTHER_ERROR
    * @brief Unknown error
    * @remark The misc error has been occurred.
    */
    public const int AILIA_LLM_STATUS_OTHER_ERROR = (-128);

    /****************************************************************
    * チャットメッセージ
    **/
    [StructLayout(LayoutKind.Sequential)]
    public class AILIAChatMessage {
        /**
        * @brief Represent the role. (system, user, assistant)
        */
        public IntPtr role;
        /**
        * @brief Represent the content of the message.
        */
        public IntPtr content;
    }

    /**
    * \~japanese
    * @brief LLMオブジェクトを作成します。
    * @param llm LLMオブジェクトポインタへのポインタ
    * @return
    *   成功した場合は \ref AILIA_LLM_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   LLMオブジェクトを作成します。
    *
    * \~english
    * @brief Creates a LLM instance.
    * @param llm A pointer to the LLM instance pointer
    * @return
    *   If this function is successful, it returns  \ref AILIA_LLM_STATUS_SUCCESS , or an error code otherwise.
    * @details
    *   Creates a LLM instance.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaLLMCreate(ref IntPtr llm);

    /**
    * \~japanese
    * @brief モデルファイルを読み込みます。
    * @param llm LLMオブジェクトポインタへのポインタ
    * @param path GGUFファイルのパス
    * @param n_ctx コンテキスト長（0でモデルのデフォルト）
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   GGUFのモデルファイルを読み込みます。
    *
    * \~english
    * @brief Open model file.
    * @param llm A pointer to the LLM instance pointer
    * @param path Path for GGUF
    * @param n_ctx Context length for model (0 is model default）
    * @return
    *   If this function is successful, it returns  \ref AILIA_STATUS_SUCCESS , or an error code otherwise.
    * @details
    *   Open a model file for GGUF.
    */
    #if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
        [DllImport(LIBRARY_NAME, EntryPoint = "ailiaLLMOpenModelFileW", CharSet=CharSet.Unicode)]
        public static extern int ailiaLLMOpenModelFile(IntPtr llm, string path, uint n_ctx);
    #else
        [DllImport(LIBRARY_NAME, EntryPoint = "ailiaLLMOpenModelFileA", CharSet=CharSet.Ansi)]
        public static extern int ailiaLLMOpenModelFile(IntPtr llm, string path, uint n_ctx);
    #endif

    /**
    * \~japanese
    * @brief サンプリングのパラメータを設定します。
    * @param llm LLMオブジェクトポインタへのポインタ
    * @param top_k サンプリングする確率値の上位個数、デフォルト40
    * @param top_p サンプリングする確率値の範囲、デフォルト0.9（0.9〜1.0）
    * @param temp 温度パラメータ、デフォルト0.4
    * @param dist シード、デフォルト1234
    * @return
    *   成功した場合は \ref AILIA_LLM_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   LLMのサンプリングの設定を行います。ailiaLLMSetPromptの前に実行する必要があります。
    *
    * \~english
    * @brief Set the sampling parameter.
    * @param llm A pointer to the LLM instance pointer
    * @param top_k Sampling probability value's top number, default 40
    * @param top_p Sampling probability value range, default 0.9 (0.9 to 1.0)
    * @param temp Temperature parameter, default 0.4
    * @param dist Seed, default 1234 
    * @return
    *   If this function is successful, it returns  \ref AILIA_LLM_STATUS_SUCCESS , or an error code otherwise.
    * @details
    *  Set LLM sampling parameters. Must be run before ailiaLLMSetPrompt. 
    */
     [DllImport(LIBRARY_NAME)]
    public static extern int ailiaLLMSetSamplingParams(IntPtr llm, uint top_k, float top_p, float temp, uint dist);

    /**
    * \~japanese
    * @brief プロンプトを設定します。
    * @param llm LLMオブジェクトポインタへのポインタ
    * @param message メッセージの配列
    * @param message_cnt メッセージの数
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   LLMに問い合わせるプロンプトを設定します。
    *   ChatHistoryもmessageに含めてください。
    *
    * \~english
    * @brief Set the prompt.
    * @param llm A pointer to the LLM instance pointer
    * @param message Array of messages
    * @param message_cnt Number of messages
    * @return
    *   If this function is successful, it returns  \ref AILIA_STATUS_SUCCESS , or an error code otherwise.
    * @details
    *   Set the prompt to query the LLM.
    *   Please include ChatHistory in the message as well.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaLLMSetPrompt(IntPtr llm, IntPtr messages, uint messages_len);

    /**
    * \~japanese
    * @brief 生成を行います。
    * @param llm LLMオブジェクトポインタ
    * @param done 生成が完了したか
    * @return
    *   成功した場合は \ref AILIA_LLM_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   デコードした結果はailiaLLMGetDeltaText APIで取得します。
    *   ailiaLLMGenerateを呼び出すたびに1トークンずつデコードします。
    *   doneは0か1を取ります。doneが1の場合、生成完了となります。
    *
    * \~english
    * @brief Perform generate
    * @param llm A LLM instance pointer
    * @param done Generation complete?
    * @return
    *   If this function is successful, it returns  \ref AILIA_LLM_STATUS_SUCCESS , or an error code otherwise.
    * @details
    *   The decoded result is obtained through the ailiaLLMGetDeltaText API.
    *   Each call to ailiaLLMGenerate decodes one token at a time.
    *   The value of done is 0 or 1. If done is 1, the generation is complete.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaLLMGenerate(IntPtr llm, ref uint done);

    /**
    * \~japanese
    * @brief テキストの長さを取得します。(NULL文字含む)
    * @param llm   LLMオブジェクトポインタ
    * @param len  テキストの長さ
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    *
    * \~english
    * @brief Gets the size of text. (Include null)
    * @param llm   A LLM instance pointer
    * @param len  The length of text
    * @return
    *   If this function is successful, it returns  \ref AILIA_STATUS_SUCCESS , or an error code otherwise.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaLLMGetDeltaTextSize(IntPtr llm, ref uint len);

    /**
    * \~japanese
    * @brief テキストを取得します。
    * @param llm   LLMオブジェクトポインタ
    * @param text  テキスト(UTF8)
    * @param len バッファサイズ
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   ailiaLLMGenerate() を一度も実行していない場合は \ref AILIA_STATUS_INVALID_STATE が返ります。
    *
    * \~english
    * @brief Gets the decoded text.
    * @param llm   A LLM instance pointer
    * @param text  Text(UTF8)
    * @param len  Buffer size
    * @return
    *   If this function is successful, it returns  \ref AILIA_STATUS_SUCCESS , or an error code otherwise.
    * @details
    *   If  ailiaLLMGenerate()  is not run at all, the function returns  \ref AILIA_STATUS_INVALID_STATE .
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaLLMGetDeltaText(IntPtr llm, IntPtr text, uint len);

    /**
    * \~japanese
    * @brief トークンの数を取得します。
    * @param llm   LLMオブジェクトポインタ
    * @param cnt   トークンの数
    * @param text  テキスト(UTF8)
    * @return
    *   成功した場合は \ref AILIA_LLM_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    *
    * \~english
    * @brief Gets the count of token.
    * @param llm   A LLM instance pointer
    * @param cnt   The count of token
    * @param text  Text(UTF8)
    * @return
    *   If this function is successful, it returns  \ref AILIA_LLM_STATUS_SUCCESS , or an error code otherwise.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaLLMGetTokenCount(IntPtr llm, ref uint cnt, IntPtr text);

    /**
    * \~japanese
    * @brief プロンプトトークンの数を取得します。
    * @param llm   LLMオブジェクトポインタ
    * @param cnt   プロンプトトークンの数
    * @return
    *   成功した場合は \ref AILIA_LLM_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   ailiaLLMSetPromptを呼び出した後に呼び出し可能です。
    *
    * \~english
    * @brief Gets the count of prompt token.
    * @param llm   A LLM instance pointer
    * @param cnt   The count of prompt token
    * @return
    *   If this function is successful, it returns  \ref AILIA_LLM_STATUS_SUCCESS , or an error code otherwise.
    * @details
    *   It can be called after calling ailiaLLMSetPrompt.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaLLMGetPromptTokenCount(IntPtr llm, ref uint cnt);

    /**
    * \~japanese
    * @brief 生成したトークンの数を取得します。
    * @param llm   LLMオブジェクトポインタ
    * @param cnt   生成したトークンの数
    * @return
    *   成功した場合は \ref AILIA_LLM_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   ailiaLLMGenerateを呼び出した後に呼び出し可能です。
    *
    * \~english
    * @brief Gets the count of prompt token.
    * @param llm   A LLM instance pointer
    * @param cnt   The count of generated token
    * @return
    *   If this function is successful, it returns  \ref AILIA_LLM_STATUS_SUCCESS , or an error code otherwise.
    * @details
    *   It can be called after calling ailiaLLMGenerate.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaLLMGetGeneratedTokenCount(IntPtr llm, ref uint cnt);

    /**
    * \~japanese
    * @brief LLMオブジェクトを破棄します。
    * @param llm LLMオブジェクトポインタ
    *
    * \~english
    * @brief It destroys the LLM instance.
    * @param llm A LLM instance pointer
    */
    [DllImport(LIBRARY_NAME)]
    public static extern void ailiaLLMDestroy(IntPtr llm);
}
} // namespace ailiaLLM
