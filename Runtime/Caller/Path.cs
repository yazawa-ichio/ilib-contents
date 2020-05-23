using System;
using System.Collections;
using System.Collections.Generic;

namespace ILib.Caller
{
	/// <summary>
	/// Callに登録したイベントのハンドラーです。
	/// </summary>
	public interface IPath : IDisposable
	{
		string Key { get; }
		bool Enabled { get; set; }
	}

	internal abstract class PathBase : IPath
	{
		bool m_Disposed;
		EventCall m_Parent;

		public string Key { get; private set; }

		public Type Type { get; protected set; }

		public bool Enabled { get; set; } = true;

		internal PathBase(EventCall call, string key)
		{
			Key = key;
			m_Parent = call;
		}

		protected virtual void Dispose(bool disposing)
		{
		}

		public void Dispose()
		{
			if (!m_Disposed)
			{
				m_Disposed = true;
				m_Parent.Remove(this);
				Dispose(true);
			}
		}

		public abstract bool Invoke(object obj);
	}

	internal class Path : PathBase
	{
		Func<bool> m_Func;

		internal Path(EventCall call, string key, Func<bool> func) : base(call, key)
		{
			Type = null;
			m_Func = func;
		}

		public override bool Invoke(object obj)
		{
			if (!Enabled) return false;
			if (obj == null && m_Func != null)
			{
				return m_Func.Invoke();
			}
			return false;
		}

		protected override void Dispose(bool disposing)
		{
			m_Func = null;
		}

	}

	internal class Path<T> : PathBase
	{
		Func<T, bool> m_Func;

		internal Path(EventCall call, string key, Func<T, bool> func) : base(call, key)
		{
			Type = typeof(T);
			m_Func = func;
		}

		public override bool Invoke(object obj)
		{
			if (!Enabled) return false;

			if (m_Func == null) return false;

			if (typeof(T).IsClass && obj == null)
			{
				return m_Func.Invoke(default(T));
			}
			else if (obj is T)
			{
				return m_Func.Invoke((T)obj);
			}
			return false;
		}

		protected override void Dispose(bool disposing)
		{
			m_Func = null;
		}

	}

	internal class HandlePath : PathBase
	{
		Func<object, bool> m_Func;
		EventHandle m_Handle;

		internal HandlePath(EventCall call, string key, Func<object, bool> func, Type type, EventHandle handle) : base(call, key)
		{
			Type = type;
			m_Func = func;
			m_Handle = handle;
		}

		public override bool Invoke(object obj)
		{
			if (!Enabled || !m_Handle.Enabled) return false;
			if (m_Func == null) return false;
			return m_Func.Invoke(obj);
		}

		protected override void Dispose(bool disposing)
		{
			m_Func = null;
		}

	}

}