using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ILib
{
	using System;
	using System.Runtime.CompilerServices;
	using System.Threading.Tasks;
	using Routines;

	public interface IRoutine : IDisposable
	{
		bool IsRunning { get; }
		bool Restartable { get; }
		void Start();
		void Cancel();
	}

	/// <summary>
	/// Unityのコルーチンのラッパーです。
	/// 完了と例外を取得できます。
	/// </summary>
	public class Routine : RoutineBase
	{
		TaskCompletionSource<bool> m_Task = new TaskCompletionSource<bool>();

		public Routine(MonoBehaviour behaviour, IEnumerator routine) : base(behaviour, routine) { }

		Action<bool, Exception> m_Observe;

		protected override void Fail(Exception ex)
		{
			m_Task.SetException(ex);
			m_Observe?.Invoke(false, ex);
			m_Observe = null;
		}

		protected override void Success()
		{
			m_Task.SetResult(true);
			m_Observe?.Invoke(true, null);
			m_Observe = null;
		}

		public void Observe(Action<bool, Exception> action)
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

		/// <summary>
		/// Unityのコルーチンのラッパーです。
		/// 完了と例外を取得できます。
		/// </summary>
		public static Routine Start(MonoBehaviour owner, IEnumerator routine) => new Routine(owner, routine);

		/// <summary>
		/// Unityのコルーチンのラッパーです。
		/// IHasResult[T]型を返すイテレータをコンストラクタで指定してください。
		/// 値が返された時点で完了扱いとなります。
		/// </summary>
		public static TaskRoutine<T> Task<T>(MonoBehaviour owner, IEnumerator routine) => new TaskRoutine<T>(owner, routine);


		public TaskAwaiter GetAwaiter()
		{
			return (m_Task.Task as Task).GetAwaiter();
		}


	}

}