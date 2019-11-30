using System;
using System.Collections;
using System.Collections.Generic;

namespace ILib.Caller
{

	/// <summary>
	/// Callに登録したオブジェクトのハンドルです。
	/// </summary>
	public class EventHandle : IDisposable
	{
		List<IPath> m_Paths = new List<IPath>();

		public bool Enabled { set; get; } = true;

		internal void Add(IPath path) => m_Paths.Add(path);

		public void SetEnabled(object key, bool enabled)
		{
			var _key = EventCall.ToKey(key);
			foreach (var path in m_Paths)
			{
				if (path.Key == _key)
				{
					path.Enabled = enabled;
				}
			}
		}

		public void Dispose()
		{
			foreach (var path in m_Paths)
			{
				path.Dispose();
			}
		}
	}

}
