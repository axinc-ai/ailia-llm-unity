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
	bool logging = true;

	/****************************************************************
	 * モデル
	 */

	/**
	* \~japanese
	* @brief インスタンスを作成します。
	* @param n_ctx コンテキスト長（0でモデルのデフォルト）
	* @return
	*   成功した場合はtrue、失敗した場合はfalseを返す。
	*   
	* \~english
	* @brief   Create a instance.
    * @param n_ctx Context length for model (0 is model default）
	* @return
	*   If this function is successful, it returns  true  , or  false  otherwise.
	*/
	public bool Create(uint n_ctx = 0){
		if (net != IntPtr.Zero){
			Close();
		}

		int status = AiliaLLM.ailiaLLMCreate(ref net, n_ctx);
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
	* @param model_path          モデルファイルへのパス。
	* @return
	*   成功した場合はtrue、失敗した場合はfalseを返す。
	*   
	* \~english
	* @brief   Open a model.
	* @param model_path          Path for model
	* @return
	*   If this function is successful, it returns  true  , or  false  otherwise.
	*/
	public bool Open(string model_path){
		if (net == IntPtr.Zero){
			return false;
		}

		int status = 0;
		
		status = AiliaLLM.ailiaLLMOpenModelFile(net, model_path);
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
	* @brief プロンプトを設定します。
	* @param messages          プロンプトメッセージ。
	* @return
	*   成功した場合はtrue、失敗した場合はfalseを返す。
	*   
	* \~english
	* @brief   Set prompt messages.
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

		if (status != 0){
			if (logging)
			{
				Debug.Log("ailiaLLMSetPrompt failed " + status);
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
		byte[] text_split = new byte [len - 1]; // NULLも時の削除
		for (int i = 0; i < len - 1; i++){
			text_split[i] = text[i];
		}
		return System.Text.Encoding.UTF8.GetString(text_split);
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
		done = (done_uint == 1);
		if (status != 0){
			if (logging)
			{
				Debug.Log("ailiaLLMGenerate failed " + status);
			}
			return false;
		}
		return true;
	}
}
} // namespace ailiaLLM
