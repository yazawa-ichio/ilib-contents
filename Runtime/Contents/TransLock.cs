using System;

namespace ILib.Contents
{

	internal class TransLock
	{
		TransLockFlag m_Flag;

		public bool IsLock() => m_Flag != 0;

		public bool IsLock(TransLockFlag flag) => (m_Flag & flag) > 0;

		public IDisposable Lock(TransLockFlag flag)
		{
			if (IsLock(TransLockFlag.Shutdown)) throw new InvalidOperationException("already Shutdown AppContext");
			if (IsLock(flag)) throw new InvalidOperationException("already locked:" + flag);
			return new LockState(this, flag);
		}

		public class LockState : IDisposable
		{
			TransLock m_TransLock;
			TransLockFlag m_Flag;
			public LockState(TransLock transLock, TransLockFlag flag)
			{
				m_TransLock = transLock;
				m_Flag = flag;
			}

			public void Dispose()
			{
				m_TransLock.m_Flag = m_TransLock.m_Flag & m_Flag;
				m_TransLock = null;
			}
		}

	}
}
