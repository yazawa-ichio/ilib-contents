using System;
using System.Collections;
using System.Collections.Generic;

namespace ILib
{
	using Routines;
	using System.Runtime.CompilerServices;
	using System.Threading.Tasks;
	using UnityEngine;

	public class NoResultTaskRoutineException : Exception
	{
		public NoResultTaskRoutineException(string message) : base(message)
		{
		}
	}

	/// <summary>
	/// Unityのコルーチンのラッパーです。
	/// IHasResult[T]型を返すイテレータをコンストラクタで指定してください。
	/// 値が返された時点で完了扱いとなります。
	/// </summary>
	public class TaskRoutine<T> : RoutineBase
	{
		TaskCompletionSource<T> m_Task = new TaskCompletionSource<T>();

		Action<T, Exception> m_Observe;

		public TaskRoutine(MonoBehaviour behaviour, IEnumerator routine) : base(behaviour, routine) { }

		protected override bool Result(IHasResult result)
		{
			var ret = result as IHasResult<T>;
			if (ret != null)
			{
				m_Task.SetResult(ret.Value);
				m_Observe?.Invoke(ret.Value, null);
				m_Observe = null;
				return true;
			}
			return false;
		}

		protected override void Fail(Exception ex)
		{
			m_Task.SetException(ex);
			m_Observe?.Invoke(default, ex);
			m_Observe = null;
		}

		protected override void Success()
		{
			Fail(new NoResultTaskRoutineException("not result task."));
		}

		public void Observe(Action<T, Exception> action)
		{
			if (m_Task.Task.IsCompleted)
			{
				action?.Invoke(m_Task.Task.Result, m_Task.Task.Exception);
			}
			else
			{
				m_Observe += action;
			}
		}

		public TaskAwaiter<T> GetAwaiter()
		{
			return m_Task.Task.GetAwaiter();
		}


	}
}