using System;

namespace ILib.Managed
{
	public class ActionDisposer : IDisposable
	{
		Action m_Action;

		public ActionDisposer(Action action) => m_Action = action;

		public void Dispose()
		{
			m_Action?.Invoke();
			m_Action = null;
		}

	}
}