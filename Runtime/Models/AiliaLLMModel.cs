/* ailia LLM model class */
/* Copyright 2024 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Threading;
using System.Runtime.InteropServices;

namespace ailiaLLM{

public class AiliaLLMChatMessage{
	public string role;
	public string content;
}

public class AiliaLLMMediaData{
	public string media_type; // "image" or "audio"
	public string file_path;
	public byte[] data;
	public uint width;
	public uint height;
}

public class AiliaLLMMultimodalChatMessage{
	public string role;
	public string content;
	public List<AiliaLLMMediaData> media_data;
}

public class AiliaLLMModel : IDisposable
{
	// instance
	IntPtr net = IntPtr.Zero;
	bool context_full = false;
	bool logging = true;
	byte [] buf = new byte[0];
	string before_text = "";

	/****************************************************************
	 * モデル
	 */

	/**
	* \~japanese
	* @brief インスタンスを作成します。
	* @return
	*   成功した場合はtrue、失敗した場合はfalseを返す。
	*   
	* \~english
	* @brief   Create a instance.
	* @return
	*   If this function is successful, it returns  true  , or  false  otherwise.
	*/
	public bool Create(){
		if (net != IntPtr.Zero){
			Close();
		}

		int status = AiliaLLM.ailiaLLMCreate(ref net);
		if (status != 0){
			if (logging)
			{
				Debug.Log("ailiaLLMCreate failed " + status);
			}
			return false;
		}

		return true;
	}

	/**
	* \~japanese
	* @brief モデルファイルを開きます。
	* @param model_path    モデルファイルへのパス。
	* @param n_ctx         コンテキスト長（0でモデルのデフォルト）
	* @return
	*   成功した場合はtrue、失敗した場合はfalseを返す。
	*   
	* \~english
	* @brief   Open a model.
	* @param model_path    Path for model
    * @param n_ctx         Context length for model (0 is model default）
	* @return
	*   If this function is successful, it returns  true  , or  false  otherwise.
	*/
	public bool Open(string model_path, uint n_ctx = 0){
		if (net == IntPtr.Zero){
			return false;
		}

		int status = 0;
		
		status = AiliaLLM.ailiaLLMOpenModelFile(net, model_path, n_ctx);
		if (status != 0){
			if (logging)
			{
				Debug.Log("ailiaLLMOpenModelFile failed " + status);
			}
			return false;
		}

		return true;
	}

	/**
	* \~japanese
	* @brief マルチモーダルプロジェクタファイルを開きます。
	* @param mmproj_path    MMPROJファイルへのパス。
	* @return
	*   成功した場合はtrue、失敗した場合はfalseを返す。
	*   
	* \~english
	* @brief   Open a multimodal projector file.
	* @param mmproj_path    Path for MMPROJ file
	* @return
	*   If this function is successful, it returns  true  , or  false  otherwise.
	*/
	public bool OpenMultimodalProjector(string mmproj_path){
		if (net == IntPtr.Zero){
			return false;
		}

		int status = AiliaLLM.ailiaLLMOpenMultimodalProjectorFile(net, mmproj_path);
		if (status != 0){
			if (logging)
			{
				Debug.Log("ailiaLLMOpenMultimodalProjectorFile failed " + status);
			}
			return false;
		}

		return true;
	}

	/**
	* \~japanese
	* @brief マルチモーダル機能がサポートされているかを確認します。
	* @param vision_support 画像処理をサポートしているか
	* @param audio_support 音声処理をサポートしているか
	* @return
	*   成功した場合はtrue、失敗した場合はfalseを返す。
	*   
	* \~english
	* @brief Check if multimodal features are supported.
	* @param vision_support Whether image processing is supported
	* @param audio_support Whether audio processing is supported
	* @return
	*   If this function is successful, it returns  true  , or  false  otherwise.
	*/
	public bool GetMultimodalCapabilities(ref bool vision_support, ref bool audio_support){
		uint vision_uint = 0;
		uint audio_uint = 0;
		int status = AiliaLLM.ailiaLLMGetMultimodalCapabilities(net, ref vision_uint, ref audio_uint);
		if (status != 0){
			if (logging)
			{
				Debug.Log("ailiaLLMGetMultimodalCapabilities failed " + status);
			}
			return false;
		}
		vision_support = (vision_uint == 1);
		audio_support = (audio_uint == 1);
		return true;
	}

	/****************************************************************
	 * 開放する
	 */

	/**
	* \~japanese
	* @brief インスタンスを破棄します。
	* @details
	*   インスタンスを破棄し、初期化します。
	*   
	*  \~english
	* @brief   Destroys instance
	* @details
	*   Destroys and initializes the instance.
	*/
	public virtual void Close()
	{
		if (net != IntPtr.Zero){
			AiliaLLM.ailiaLLMDestroy(net);
			net = IntPtr.Zero;
		}
	}

	/**
	* \~japanese
	* @brief リソースを解放します。
	*   
	*  \~english
	* @brief   Release resources.
	*/
	public virtual void Dispose()
	{
		Dispose(true);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing){
			// release managed resource
		}
		Close(); // release unmanaged resource
	}

	~AiliaLLMModel(){
		Dispose(false);
	}

	/****************************************************************
	 * チャット
	 */

	/**
	* \~japanese
	* @brief サンプリングパラメータを設定します。
    * @param top_k サンプリングする確率値の上位個数、デフォルト40
    * @param top_p サンプリングする確率値の範囲、デフォルト0.9（0.9〜1.0）
    * @param temp 温度パラメータ、デフォルト0.4
    * @param dist シード、デフォルト1234
	* @return
	*   成功した場合はtrue、失敗した場合はfalseを返す。
	*   
	* \~english
	* @brief Set the sampling parameter.
    * @param top_k Sampling probability value's top number, default 40
    * @param top_p Sampling probability value range, default 0.9 (0.9 to 1.0)
    * @param temp Temperature parameter, default 0.4
    * @param dist Seed, default 1234 
	* @return
	*   If this function is successful, it returns  true  , or  false  otherwise.
	*/
	public bool SetSamplingParam(uint top_k, float top_p, float temp, uint dist){
		int status = AiliaLLM.ailiaLLMSetSamplingParams(net, top_k, top_p, temp, dist);
		if (status != 0){
			if (logging)
			{
				Debug.Log("ailiaLLMSetSamplingParams failed " + status);
			}
			return false;
		}
		return true;
	}

	/**
	* \~japanese
	* @brief Thinking（推論過程の出力）を有効または無効にします。
	* @param enable trueで有効、falseで無効（デフォルト：無効）
	* @return
	*   成功した場合はtrue、失敗した場合はfalseを返す。
	* @details
	*   Thinkingモデル（Gemma4等）の推論過程の出力を制御します。SetPromptの前に実行する必要があります。
	*
	* \~english
	* @brief Enable or disable thinking (reasoning output).
	* @param enable true to enable, false to disable (default: disabled)
	* @return
	*   If this function is successful, it returns  true  , or  false  otherwise.
	* @details
	*   Controls whether thinking models (e.g. Gemma4) output their reasoning process.
	*   Must be called before SetPrompt.
	*/
	public bool SetThinking(bool enable){
		int status = AiliaLLM.ailiaLLMSetThinking(net, enable ? 1u : 0u);
		if (status != 0){
			if (logging)
			{
				Debug.Log("ailiaLLMSetThinking failed " + status);
			}
			return false;
		}
		return true;
	}

	/**
	* \~japanese
	* @brief プロンプトを設定します。
	* @param messages          プロンプトメッセージ。
	* @return
	*   成功した場合はtrue、失敗した場合はfalseを返す。
	*   
	* \~english
	* @brief Set prompt messages.
	* @param messages          Prompt messages
	* @return
	*   If this function is successful, it returns  true  , or  false  otherwise.
	*/
	public bool SetPrompt(List<AiliaLLMChatMessage> messages)
	{
		List<GCHandle> handle_list = new List<GCHandle>();
		int len = messages.Count;
		byte[][] role_text_list = new byte [len][];
		byte[][] conntent_text_list = new byte [len][];
		AiliaLLM.AILIAChatMessage [] message_list = new AiliaLLM.AILIAChatMessage[len];
		for (int i = 0; i< len; i++){
			AiliaLLM.AILIAChatMessage message = new AiliaLLM.AILIAChatMessage();

			role_text_list[i] = System.Text.Encoding.UTF8.GetBytes(messages[i].role+"\u0000");
			GCHandle role_handle = GCHandle.Alloc(role_text_list[i], GCHandleType.Pinned);
			IntPtr role_input = role_handle.AddrOfPinnedObject();

			conntent_text_list[i] = System.Text.Encoding.UTF8.GetBytes(messages[i].content+"\u0000");
			GCHandle content_handle = GCHandle.Alloc(conntent_text_list[i], GCHandleType.Pinned);
			IntPtr content_input = content_handle.AddrOfPinnedObject();

			message.role = role_input;
			message.content = content_input;
			message_list[i] = message;

			handle_list.Add(role_handle);
			handle_list.Add(content_handle);
		}

		int size = Marshal.SizeOf(typeof(AiliaLLM.AILIAChatMessage)) * message_list.Length;
		IntPtr ptr = Marshal.AllocHGlobal(size);

		int status = 0;

		try
		{
			for (int i = 0; i < message_list.Length; i++)
			{
				IntPtr offset = new IntPtr(ptr.ToInt64() + i * Marshal.SizeOf(typeof(AiliaLLM.AILIAChatMessage)));
				Marshal.StructureToPtr(message_list[i], offset, false);
			}

			status = AiliaLLM.ailiaLLMSetPrompt(net, ptr, (uint)len);
		}
		finally
		{
			Marshal.FreeHGlobal(ptr);
		}

		for (int i = 0; i < handle_list.Count; i++){
			handle_list[i].Free();
		}

		context_full = false;
		buf = new byte[0];
		before_text = "";

		if (status != 0){
			if (logging)
			{
				Debug.Log("ailiaLLMSetPrompt failed " + status);
			}
			if (status == AiliaLLM.AILIA_LLM_STATUS_CONTEXT_FULL){
				context_full = true;
			}
			return false;
		}

		return true;
	}

	/**
	* \~japanese
	* @brief ツール（関数）の定義を設定します。
	* @param tools_json OpenAI互換のツール定義JSON配列。nullまたは空文字列で解除します。
	* @return
	*   成功した場合はtrue、失敗した場合はfalseを返す。
	* @details
	*   例: [{"type":"function","function":{"name":"get_weather","description":"...","parameters":{...}}}]
	*   設定したツールは次回のSetPrompt時にチャットテンプレート経由でプロンプトへ展開され、
	*   生の出力はParseResponseでツール呼び出しとして構造化できます。Gemma 4などツール呼び出し対応モデルで使用できます。
	*   ツール設定中、role "assistant" のcontentは生の出力（GetDeltaTextの連結）、role "tool" のcontentはツールの実行結果として
	*   解釈されます。toolメッセージは直前のassistantのツール呼び出しと順序で対応付けられます。ツール未設定でtoolロールを渡すと失敗します。
	*
	* \~english
	* @brief Set the tool (function) definitions.
	* @param tools_json OpenAI-compatible JSON array of tool definitions. null or an empty string clears the tools.
	* @return
	*   If this function is successful, it returns  true  , or  false  otherwise.
	* @details
	*   Example: [{"type":"function","function":{"name":"get_weather","description":"...","parameters":{...}}}]
	*   The tools are rendered into the prompt on the next SetPrompt, and the raw output can be parsed into
	*   tool calls with ParseResponse. Available for models supporting tool calling (e.g. Gemma 4).
	*   While tools are set, the content of role "assistant" is the raw output (concatenation of GetDeltaText) and
	*   the content of role "tool" is the tool result, matched to the tool calls of the preceding assistant message by order.
	*   A tool message while no tools are set fails.
	*/
	public bool SetTools(string tools_json)
	{
		int status = 0;
		if (string.IsNullOrEmpty(tools_json)){
			status = AiliaLLM.ailiaLLMSetTools(net, IntPtr.Zero);
		} else {
			byte[] tools_bytes = System.Text.Encoding.UTF8.GetBytes(tools_json + "\u0000");
			GCHandle tools_handle = GCHandle.Alloc(tools_bytes, GCHandleType.Pinned);
			status = AiliaLLM.ailiaLLMSetTools(net, tools_handle.AddrOfPinnedObject());
			tools_handle.Free();
		}
		if (status != 0){
			if (logging)
			{
				Debug.Log("ailiaLLMSetTools failed " + status);
			}
			return false;
		}
		return true;
	}

	/**
	* \~japanese
	* @brief モデルの生の出力テキストを解析し、OpenAI互換のassistantメッセージJSONに変換します。
	* @param text GetDeltaTextで取得したテキストを連結した生の出力
	* @return
	*   {"role":"assistant","content":"...","reasoning_content":"...","tool_calls":[{"id":"call_0","type":"function","function":{"name":"...","arguments":"{...}"}}]} 形式のJSON。失敗時は空文字列。
	* @details
	*   reasoning_contentはThinkingの出力が含まれる場合のみ、tool_callsはツール呼び出しが含まれる場合のみ出力されます。
	*   解析器はSetPromptで生成されます。SetPrompt前、またはSetTools / SetThinking後にSetPromptを呼んでいない場合、
	*   およびテキストがツール呼び出し構文と一致しない場合（生成途中のテキストを含む）は失敗します。
	*
	* \~english
	* @brief Parses the raw output text of the model into an OpenAI-compatible assistant message JSON.
	* @param text Raw output, i.e. the concatenation of the text obtained with GetDeltaText
	* @return
	*   JSON in the form {"role":"assistant","content":"...","reasoning_content":"...","tool_calls":[{"id":"call_0","type":"function","function":{"name":"...","arguments":"{...}"}}]}. Empty string on failure.
	* @details
	*   reasoning_content is present only when the text contains thinking output, and tool_calls only when it contains tool calls.
	*   The parser is built by SetPrompt. The call fails before SetPrompt, after SetTools / SetThinking without a new SetPrompt,
	*   and when the text does not match the tool call syntax (including an unfinished output).
	*/
	public string ParseResponse(string text)
	{
		if (text == null){
			return "";
		}
		byte[] text_bytes = System.Text.Encoding.UTF8.GetBytes(text + "\u0000");
		GCHandle text_handle = GCHandle.Alloc(text_bytes, GCHandleType.Pinned);
		IntPtr text_ptr = text_handle.AddrOfPinnedObject();

		uint len = 0;
		int status = AiliaLLM.ailiaLLMParseResponseSize(net, text_ptr, ref len);
		if (status != 0){
			text_handle.Free();
			if (logging)
			{
				Debug.Log("ailiaLLMParseResponseSize failed " + status);
			}
			return "";
		}
		byte[] json = new byte [len];
		GCHandle handle = GCHandle.Alloc(json, GCHandleType.Pinned);
		IntPtr output = handle.AddrOfPinnedObject();
		status = AiliaLLM.ailiaLLMParseResponse(net, text_ptr, output, len);
		handle.Free();
		text_handle.Free();
		if (status != 0){
			if (logging)
			{
				Debug.Log("ailiaLLMParseResponse failed " + status);
			}
			return "";
		}
		return System.Text.Encoding.UTF8.GetString(json, 0, (int)len - 1); // NULLの削除
	}

	/**
	* \~japanese
	* @brief マルチモーダルプロンプトを設定します。
	* @param messages          マルチモーダルプロンプトメッセージ。
	* @return
	*   成功した場合はtrue、失敗した場合はfalseを返す。
	*   
	* \~english
	* @brief Set multimodal prompt messages.
	* @param messages          Multimodal prompt messages
	* @return
	*   If this function is successful, it returns  true  , or  false  otherwise.
	*/
	public bool SetMultimodalPrompt(List<AiliaLLMMultimodalChatMessage> messages)
	{
		List<GCHandle> handle_list = new List<GCHandle>();
		int len = messages.Count;
		byte[][] role_text_list = new byte [len][];
		byte[][] content_text_list = new byte [len][];
		AiliaLLM.AILIALLMMultimodalChatMessage [] message_list = new AiliaLLM.AILIALLMMultimodalChatMessage[len];
		
		// Prepare media data arrays
		List<byte[][]> media_type_lists = new List<byte[][]>();
		List<byte[][]> media_path_lists = new List<byte[][]>();
		List<AiliaLLM.AILIALLMMediaData[]> media_data_arrays = new List<AiliaLLM.AILIALLMMediaData[]>();
		
		for (int i = 0; i < len; i++){
			AiliaLLM.AILIALLMMultimodalChatMessage message = new AiliaLLM.AILIALLMMultimodalChatMessage();

			// Set role and content
			role_text_list[i] = System.Text.Encoding.UTF8.GetBytes(messages[i].role+"\u0000");
			GCHandle role_handle = GCHandle.Alloc(role_text_list[i], GCHandleType.Pinned);
			IntPtr role_input = role_handle.AddrOfPinnedObject();

			content_text_list[i] = System.Text.Encoding.UTF8.GetBytes(messages[i].content+"\u0000");
			GCHandle content_handle = GCHandle.Alloc(content_text_list[i], GCHandleType.Pinned);
			IntPtr content_input = content_handle.AddrOfPinnedObject();

			message.role = role_input;
			message.content = content_input;

			handle_list.Add(role_handle);
			handle_list.Add(content_handle);

			// Handle media data
			if (messages[i].media_data != null && messages[i].media_data.Count > 0){
				int media_count = messages[i].media_data.Count;
				message.media_count = (uint)media_count;
				
				byte[][] media_type_list = new byte[media_count][];
				byte[][] media_path_list = new byte[media_count][];
				AiliaLLM.AILIALLMMediaData[] media_array = new AiliaLLM.AILIALLMMediaData[media_count];
				
				for (int j = 0; j < media_count; j++){
					AiliaLLM.AILIALLMMediaData media = new AiliaLLM.AILIALLMMediaData();
					
					// Media type
					media_type_list[j] = System.Text.Encoding.UTF8.GetBytes(messages[i].media_data[j].media_type+"\u0000");
					GCHandle type_handle = GCHandle.Alloc(media_type_list[j], GCHandleType.Pinned);
					media.media_type = type_handle.AddrOfPinnedObject();
					handle_list.Add(type_handle);
					
					// File path
					if (!string.IsNullOrEmpty(messages[i].media_data[j].file_path)){
						media_path_list[j] = System.Text.Encoding.UTF8.GetBytes(messages[i].media_data[j].file_path+"\u0000");
						GCHandle path_handle = GCHandle.Alloc(media_path_list[j], GCHandleType.Pinned);
						media.file_path = path_handle.AddrOfPinnedObject();
						handle_list.Add(path_handle);
					} else {
						media.file_path = IntPtr.Zero;
					}
					
					// Raw data
					if (messages[i].media_data[j].data != null && messages[i].media_data[j].data.Length > 0){
						GCHandle data_handle = GCHandle.Alloc(messages[i].media_data[j].data, GCHandleType.Pinned);
						media.data = data_handle.AddrOfPinnedObject();
						media.data_size = (uint)messages[i].media_data[j].data.Length;
						handle_list.Add(data_handle);
					} else {
						media.data = IntPtr.Zero;
						media.data_size = 0;
					}
					
					media.width = messages[i].media_data[j].width;
					media.height = messages[i].media_data[j].height;
					
					media_array[j] = media;
				}
				
				media_type_lists.Add(media_type_list);
				media_path_lists.Add(media_path_list);
				media_data_arrays.Add(media_array);
				
				// Allocate and set media_data pointer
				int media_size = Marshal.SizeOf(typeof(AiliaLLM.AILIALLMMediaData)) * media_count;
				IntPtr media_ptr = Marshal.AllocHGlobal(media_size);
				
				for (int j = 0; j < media_count; j++){
					IntPtr offset = new IntPtr(media_ptr.ToInt64() + j * Marshal.SizeOf(typeof(AiliaLLM.AILIALLMMediaData)));
					Marshal.StructureToPtr(media_array[j], offset, false);
				}
				
				message.media_data = media_ptr;
			} else {
				message.media_count = 0;
				message.media_data = IntPtr.Zero;
			}
			
			message_list[i] = message;
		}

		int size = Marshal.SizeOf(typeof(AiliaLLM.AILIALLMMultimodalChatMessage)) * message_list.Length;
		IntPtr ptr = Marshal.AllocHGlobal(size);

		int status = 0;

		try
		{
			for (int i = 0; i < message_list.Length; i++)
			{
				IntPtr offset = new IntPtr(ptr.ToInt64() + i * Marshal.SizeOf(typeof(AiliaLLM.AILIALLMMultimodalChatMessage)));
				Marshal.StructureToPtr(message_list[i], offset, false);
			}

			status = AiliaLLM.ailiaLLMSetMultimodalPrompt(net, ptr, (uint)len);
		}
		finally
		{
			// Free allocated memory
			Marshal.FreeHGlobal(ptr);
			for (int i = 0; i < message_list.Length; i++){
				if (message_list[i].media_data != IntPtr.Zero){
					Marshal.FreeHGlobal(message_list[i].media_data);
				}
			}
		}

		for (int i = 0; i < handle_list.Count; i++){
			handle_list[i].Free();
		}

		context_full = false;
		buf = new byte[0];
		before_text = "";

		if (status != 0){
			if (logging)
			{
				Debug.Log("ailiaLLMSetMultimodalPrompt failed " + status);
			}
			if (status == AiliaLLM.AILIA_LLM_STATUS_CONTEXT_FULL){
				context_full = true;
			}
			return false;
		}

		return true;
	}

	/**
	* \~japanese
	* @brief 生成を実行します。
	* @param done    生成が終了したかどうか。
	* @return
	*   成功した場合はtrue、失敗した場合はfalseを返す。
	*   
	* \~english
	* @brief   Perform encode
	* @param done    Is done generation
	* @return
	*   If this function is successful, it returns array of tokens  , or  empty array  otherwise.
	*/
	public bool Generate(ref bool done)
	{
		uint done_uint = 0;
		int status = AiliaLLM.ailiaLLMGenerate(net, ref done_uint);
		context_full = false;
		done = (done_uint == 1);
		if (status != 0){
			if (logging)
			{
				Debug.Log("ailiaLLMGenerate failed " + status);
			}
			if (status == AiliaLLM.AILIA_LLM_STATUS_CONTEXT_FULL){
				context_full = true;
			}
			return false;
		}
		return true;
	}

	/**
	* \~japanese
	* @brief 生成結果のテキストを取得します。
	* @return
	*   テキストを返します。
	*   
	* \~english
	* @brief   Set prompt messages.
	* @return
	*   It returns  text.
	*/
	public string GetDeltaText()
	{
		uint len = 0;
		int status = AiliaLLM.ailiaLLMGetDeltaTextSize(net, ref len);
		if (status != 0){
			if (logging)
			{
				Debug.Log("ailiaLLMGetDeltaTextSize failed " + status);
			}
			return "";
		}
		byte[] text = new byte [len];
		GCHandle handle = GCHandle.Alloc(text, GCHandleType.Pinned);
		IntPtr output = handle.AddrOfPinnedObject();
		status = AiliaLLM.ailiaLLMGetDeltaText(net, output, len);
		handle.Free();
		if (status != 0){
			if (logging)
			{
				Debug.Log("ailiaLLMGetDeltaText failed " + status);
			}
			return "";
		}
		
		byte[] new_buf = new byte [buf.Length + len - 1];
		for (int i = 0; i < buf.Length; i++){
			new_buf[i] = buf[i];
		}
		for (int i = 0; i < len - 1; i++){ // NULLの削除
			new_buf[buf.Length + i] = text[i];
		}
		buf = new_buf;

		string decoded_text = System.Text.Encoding.UTF8.GetString(buf); // Unicode Decode Errorは発生しない
		string delta_text = "";
		if (decoded_text.Length > before_text.Length){
			delta_text = decoded_text.Substring(before_text.Length);
		}
		before_text = decoded_text;
		return delta_text;
	}

	/**
	* \~japanese
	* @brief コンテキスト長の上限に達したかどうかを取得します。
	* @return
	*   上限に達した場合はtrue、達していない場合はfalse。
	*
	* \~english
	* @brief   Check if the context length limit has been reached.
	* @return
	*   True if the limit is reached, false otherwise.
	*/
	public bool ContextFull()
	{
		return context_full;
	}

	/**
	* \~japanese
	* @brief プロンプトのトークンの数を取得します。
	* @return
	*   プロンプトのトークン数。失敗時は0。
	*
	* \~english
	* @brief   Gets the number of prompt tokens.
	* @return
	*   Number of prompt tokens. 0 if failed.
	*/
	public uint PromptTokenCount(){
		if (net == IntPtr.Zero){
			return 0;
		}
		uint count = 0;
		int status = AiliaLLM.ailiaLLMGetPromptTokenCount(net, ref count);
		if (status != 0){
			if (logging)
			{
				Debug.Log("ailiaLLMGetPromptTokenCount failed " + status);
			}
			return 0;
		}
		return count;
	}

	/**
	* \~japanese
	* @brief 生成したトークンの数を取得します。
	* @return
	*   生成したトークン数。失敗時は0。
	*
	* \~english
	* @brief   Gets the number of tokens generated.
	* @return
	*   Number of tokens generated. 0 if failed.
	*/
	public uint GeneratedTokenCount(){
		if (net == IntPtr.Zero){
			return 0;
		}
		uint count = 0;
		int status = AiliaLLM.ailiaLLMGetGeneratedTokenCount(net, ref count);
		if (status != 0){
			if (logging)
			{
				Debug.Log("ailiaLLMGetGeneratedTokenCount failed " + status);
			}
			return 0;
		}
		return count;
	}
}
} // namespace ailiaLLM
