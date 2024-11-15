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
}
} // namespace ailiaLLM
