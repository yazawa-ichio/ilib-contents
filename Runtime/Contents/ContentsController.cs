using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ILib.Contents
{
	using Caller;
	using Routines;
	using System.Threading.Tasks;
	using Log = Contents.ContentsLog;

	/// <summary>
	/// アプリケーションの全体の制御を行います。
	/// 個々の制御はContentを継承したクラスに記述します。
	/// </summary>
	public class ContentsController : MonoBehaviour, IHasDispatcher
	{
		EventCall m_EventCall = new EventCall();
		IDispatcher m_Dispatcher;
		Content m_Root;

		/// <summary>
		/// コンテンツを跨ぐ機能を提供するモジュールを管理します
		/// </summary>
		public ModuleCollection Modules { get; } = new ModuleCollection();

		/// <summary>
		/// ルートコンテンツ
		/// </summary>
		public Content Root => m_Root;

		/// <summary>
		/// イベントの
		/// </summary>
		public EventCall EventCall => m_EventCall;

		/// <summary>
		/// コンテンツに実行されるイベントの発火装置
		/// </summary>
		public IDispatcher Dispatcher => m_Dispatcher != null ? m_Dispatcher : m_Dispatcher = new Dispatcher(m_EventCall);

		/// <summary>
		/// コンテンツの例外をハンドリングします
		/// </summary>
		public event Func<Exception, bool> OnException;

		/// <summary>
		/// コントローラーを起動します。
		/// BootParamで指定したコンテンツが起動します。
		/// </summary>
		public Task Boot(BootParam param)
		{
			return Boot<RootContent>(param);
		}

		/// <summary>
		/// 指定したコンテンツでコントローラーを起動します。
		/// </summary>
		public Task Boot<T>(object prm) where T : Content, new()
		{
			if (m_Root != null) throw new InvalidOperationException("already boot ContentsController");
			m_Root = new T();
			Modules.Set(this);
			return m_Root.Boot(this, null, prm);
		}

		/// <summary>
		/// コントローラーを終了します。
		/// </summary>
		public async Task Shutdown()
		{
			await m_Root.DoShutdown();
			m_Root = null;
		}

		/// <summary>
		/// 指定のタイプのコンテンツを取得します。
		/// </summary>
		public T Get<T>()
		{
			return m_Root.Get<T>();
		}

		/// <summary>
		/// 指定のタイプのコンテンツをすべて取得します。
		/// </summary>
		public IEnumerable<T> GetAll<T>()
		{
			return m_Root.GetAll<T>();
		}

		void OnDestroy()
		{
			m_EventCall.Dispose();
			m_EventCall = null; ;
			OnDestroyEvent();
		}

		protected virtual void OnDestroyEvent() { }

		/// <summary>
		/// 例外をスローします
		/// </summary>
		public bool ThrowException(Exception ex)
		{
			if (!HandleException(ex))
			{
				if (OnException != null && OnException(ex))
				{
					return true;
				}
				Log.Exception(ex);
			}
			return false;
		}

		protected virtual bool HandleException(Exception ex) => false;

	}



}