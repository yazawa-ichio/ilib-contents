using System;
using System.Collections.Generic;

namespace ILib.Caller
{
	using Log = Contents.ContentsLog;

	[Obsolete("EventCallを使用してください")]
	public class Call : EventCall { }

	/// <summary>
	/// 登録したイベントの呼び出し手続きを行うクラスです。
	/// 階層構造を持つことが出来ます。
	/// </summary>
	public class EventCall : IDisposable, IDispatcher
	{
		/// <summary>
		/// 実際に利用する文字列型のキーに変換します。
		/// </summary>
		public static string ToKey(object key)
		{
			if (key is string)
			{
				return key.ToString();
			}
			else
			{
				return key.GetType().FullName + "-" + key.ToString();
			}
		}

		EventCall m_Parent;
		List<EventCall> m_Calls;
		List<PathBase> m_Paths = new List<PathBase>();
		int m_RemoveLock = 0;
		List<PathBase> m_Removes = new List<PathBase>();
		bool m_Enabled = true;
		object m_Locker = new object();

		public bool InvokeBeforChild { get; set; } = true;

		/// <summary>
		/// イベントの発火が可能か？
		/// false時はイベントが発火されません。
		/// </summary>
		public bool Enabled { get { return m_Enabled && !Disposed; } set { m_Enabled = value; } }

		/// <summary>
		/// 解放済みか？
		/// 解放後はイベントの発火および登録が出来なくなります。
		/// </summary>
		public bool Disposed { get; private set; }

		~EventCall()
		{
			Dispose();
		}

		/// <summary>
		/// 子のコールを生成します。
		/// </summary>
		public EventCall SubCall()
		{
			Log.Trace("[ilib-event] create subcall.");
			EventCall call = new EventCall();
			lock (m_Locker)
			{
				call.m_Parent = this;
				if (m_Calls == null)
				{
					m_Calls = new List<EventCall>(4);
				}
				for (int i = 0; i < m_Calls.Count; i++)
				{
					if (m_Calls[i] == null)
					{
						m_Calls[i] = call;
						return call;
					}
				}
				m_Calls.Add(call);
			}
			return call;
		}

		void Remove(EventCall call)
		{
			Log.Trace("[ilib-event] remove call.");
			lock (m_Locker)
			{
				//要素の長さは変えない
				var index = m_Calls.IndexOf(call);
				if (index >= 0)
				{
					m_Calls[index] = call;
				}
			}
		}

		/// <summary>
		/// イベントの解除を行います。
		/// </summary>
		public void Dispose()
		{
			Log.Trace("[ilib-event] dispose.");
			m_Parent?.Remove(this);
			m_Paths?.Clear();
			if (m_Calls == null) return;
			lock (m_Locker)
			{
				foreach (var call in m_Calls.ToArray())
				{
					if (call != this) call.Dispose();
				}
				m_Calls.Clear();
			}
		}


		/// <summary>
		/// イベントを登録します。
		/// 解除する場合は、返り値のオブジェクトを解放してください。
		/// </summary>
		public IDisposable Subscribe(object key, Action action)
		{
			var _key = ToKey(key);
			Log.Debug("[ilib-event] subscribe key:{0}", _key);
			var path = new Path(this, _key, () =>
			{
				action();
				return true;
			});
			m_Paths.Add(path);
			return path;
		}

		/// <summary>
		/// イベントを登録します。
		/// 解除する場合は、返り値のオブジェクトを解放してください。
		/// </summary>
		[Obsolete("Subscribeに置き換えてください")]
		public IPath Path(object key, Action action)
		{
			return Subscribe(key, action) as IPath;
		}

		/// <summary>
		/// イベントを登録します。
		/// 解除する場合は、返り値のオブジェクトを解放してください。
		/// </summary>
		public IDisposable Subscribe<T>(object key, Action<T> action)
		{
			var _key = ToKey(key);
			Log.Debug("[ilib-event] subscribe key:{0}({1})", _key, typeof(T));
			var path = new Path<T>(this, _key, (item) =>
			{
				action(item);
				return true;
			});
			m_Paths.Add(path);
			return path;
		}

		/// <summary>
		/// イベントを登録します。
		/// 解除する場合は、返り値のオブジェクトを解放してください。
		/// </summary>
		[Obsolete("Subscribeに置き換えてください")]
		public IPath Path<T>(object key, Action<T> action)
		{
			return Subscribe(key, action) as IPath;
		}

		/// <summary>
		/// 指定のオブジェクトをイベントを登録します。
		/// イベントはHandle属性を利用して設定します。
		/// 解除する場合は、返り値のオブジェクトを解放してください。
		/// </summary>
		public EventHandle Bind(object handler)
		{
			if (Disposed) return new EventHandle();
			Log.Debug("[ilib-event] Bind {0}", handler);
			EventHandle handle = new EventHandle();
			var entry = HandleEntry.Get(handler.GetType());
			foreach (var kvp in entry.Methods)
			{
				var key = kvp.Key;
				var method = kvp.Value;
				var parameter = entry.Parameters[key];
				PathBase path = null;
				var type = parameter?.ParameterType ?? null;
				path = new HandlePath(this, key, (obj) => entry.Invoke(handler, key, obj), type, handle);
				handle.Add(path);
				m_Paths.Add(path);
			}
			return handle;
		}

		/// <summary>
		/// イベントを実行します。
		/// イベントを受け取ったパスがあるとそこで処理が終わります。
		/// イベントが受け取られたかは返り値で判断できます。
		/// </summary>
		public bool Message(object key) => Disposed ? false : Message(ToKey(key));

		/// <summary>
		/// イベントを実行します。
		/// イベントを受け取ったパスがあるとそこで処理が終わります。
		/// イベントが受け取られたかは返り値で判断できます。
		/// </summary>
		public bool Message(string key)
		{
			if (Disposed) return false;

			Log.Trace("[ilib-event] Message {0}", key);

			if (InvokeBeforChild)
			{
				if (MessageImpl(key)) return true;
			}
			if (m_Calls != null)
			{
				for (int i = 0; i < m_Calls.Count; i++)
				{
					EventCall call = m_Calls[i];
					if (call != null && call.Message(key)) return true;
				}
			}
			if (!InvokeBeforChild)
			{
				if (MessageImpl(key)) return true;
			}
			return false;
		}

		bool MessageImpl(string key)
		{
			if (!Enabled) return false;
			bool ret = false;
			try
			{
				m_RemoveLock++;
				for (int i = 0, count = m_Paths.Count; i < count; i++)
				{
					PathBase path = m_Paths[i];
					if (m_Removes != null && m_Removes.Contains(path)) continue;
					if (path.Key == key && path.Type == null)
					{
						if (path.Invoke(null))
						{
							ret = true;
							break;
						}
					}
				}
			}
			finally
			{
				m_RemoveLock--;
			}
			FlushRemove();
			return ret;
		}

		/// <summary>
		/// 引数ありでイベントを実行します。
		/// イベントを受け取ったパスがあるとそこで処理が終わります。
		/// イベントが受け取られたかは返り値で判断できます。
		/// </summary>
		public bool Message<T>(object key, T prm) => Disposed ? false : Message(ToKey(key), prm);

		/// <summary>
		/// 引数ありでイベントを実行します。
		/// イベントを受け取ったパスがあるとそこで処理が終わります。
		/// イベントが受け取られたかは返り値で判断できます。
		/// </summary>
		public bool Message<T>(string key, T prm)
		{
			if (Disposed) return false;

			Log.Trace("[ilib-event] Message {0}({1})", key, typeof(T));

			if (InvokeBeforChild)
			{
				if (MessageImpl(key, prm)) return true;
			}
			if (m_Calls != null)
			{
				for (int i = 0; i < m_Calls.Count; i++)
				{
					EventCall call = m_Calls[i];
					if (call != null && call.Message(key, prm)) return true;
				}
			}
			if (!InvokeBeforChild)
			{
				if (MessageImpl(key, prm)) return true;
			}
			return false;
		}

		bool MessageImpl<T>(string key, T prm)
		{
			if (!Enabled) return false;
			bool ret = false;
			try
			{
				m_RemoveLock++;
				for (int i = 0, count = m_Paths.Count; i < count; i++)
				{
					PathBase path = m_Paths[i];
					if (m_Removes != null && m_Removes.Contains(path)) continue;
					if (path.Key == key && path.Type == typeof(T))
					{
						if (path.Invoke(prm))
						{
							ret = true;
							break;
						}
					}
				}
			}
			finally
			{
				m_RemoveLock--;
			}
			FlushRemove();
			return ret;
		}

		/// <summary>
		/// イベントを実行します。
		/// 全ての登録されたイベントに対して実行を行います。
		/// 一つでもイベントが受け取られるとtrueを返します。
		/// </summary>
		public bool Broadcast(object key) => Disposed ? false : Broadcast(ToKey(key));

		/// <summary>
		/// イベントを実行します。
		/// 全ての登録されたイベントに対して実行を行います。
		/// 一つでもイベントが受け取られるとtrueを返します。
		/// </summary>
		public bool Broadcast(string key)
		{
			if (Disposed) return false;

			Log.Trace("[ilib-event] Broadcast {0}", key);

			bool ret = false;
			if (InvokeBeforChild)
			{
				ret |= BroadcastImpl(key);
			}
			if (m_Calls != null)
			{
				for (int i = 0; i < m_Calls.Count; i++)
				{
					EventCall call = m_Calls[i];
					if (call != null) ret |= call.Broadcast(key);
				}
			}
			if (!InvokeBeforChild)
			{
				ret |= BroadcastImpl(key);
			}
			return ret;
		}

		bool BroadcastImpl(string key)
		{
			if (!Enabled) return false;
			bool ret = false;
			try
			{
				m_RemoveLock++;
				for (int i = 0, count = m_Paths.Count; i < count; i++)
				{
					PathBase path = m_Paths[i];
					if (m_Removes != null && m_Removes.Contains(path)) continue;
					if (path.Key == key && path.Type == null)
					{
						ret |= path.Invoke(null);
					}
				}
			}
			finally
			{
				m_RemoveLock--;
			}
			FlushRemove();
			return ret;
		}

		/// <summary>
		/// 引数ありでイベントを実行します。
		/// 全ての登録されたイベントに対して実行を行います。
		/// 一つでもイベントが受け取られるとtrueを返します。
		/// </summary>
		public bool Broadcast<T>(object key, T prm) => Disposed ? false : Broadcast(ToKey(key), prm);

		/// <summary>
		/// 引数ありでイベントを実行します。
		/// 全ての登録されたイベントに対して実行を行います。
		/// 一つでもイベントが受け取られるとtrueを返します。
		/// </summary>
		public bool Broadcast<T>(string key, T prm)
		{
			if (Disposed) return false;

			Log.Trace("[ilib-event] Broadcast {0}({1})", key, typeof(T));

			bool ret = false;
			if (InvokeBeforChild)
			{
				ret |= BroadcastImpl(key, prm);
			}
			if (m_Calls != null)
			{
				for (int i = 0; i < m_Calls.Count; i++)
				{
					EventCall call = m_Calls[i];
					if (call != null) ret |= call.Broadcast(key, prm);
				}
			}
			if (!InvokeBeforChild)
			{
				ret |= BroadcastImpl(key, prm);
			}
			return ret;
		}

		bool BroadcastImpl<T>(string key, T prm)
		{
			if (!Enabled) return false;
			bool ret = false;
			try
			{
				m_RemoveLock++;
				for (int i = 0, count = m_Paths.Count; i < count; i++)
				{
					PathBase path = m_Paths[i];
					if (m_Removes != null && m_Removes.Contains(path)) continue;
					if (path.Key == key && path.Type == typeof(T))
					{
						ret |= path.Invoke(prm);
					}
				}
			}
			finally
			{
				m_RemoveLock--;
			}
			FlushRemove();
			return ret;
		}

		void FlushRemove()
		{
			if (m_RemoveLock == 0 && m_Removes != null && m_Removes.Count > 0)
			{
				foreach (var remove in m_Removes)
				{
					m_Paths.Remove(remove);
				}
				m_Removes.Clear();
			}
		}

		internal void Remove(PathBase path)
		{

			Log.Trace("[ilib-event] Remove Path {0}({1})", path.Key, path.Type);

			if (m_RemoveLock > 0)
			{
				if (m_Removes == null) m_Removes = new List<PathBase>();
				m_Removes.Add(path);
			}
			else
			{
				m_Paths.Remove(path);
			}
		}

	}

}