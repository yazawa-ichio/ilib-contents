using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ILib.Routines
{
	using Log = Contents.ContentsLog;

	public abstract class RoutineBase : CustomYieldInstruction, IDisposable
	{
		public bool IsRunning => m_Coroutine != null;
		public sealed override bool keepWaiting => IsRunning;

		MonoBehaviour m_Behaviour;
		IEnumerator m_Current;
		Func<IEnumerator> m_Routine;
		Coroutine m_Coroutine;

		public RoutineBase(MonoBehaviour behaviour, IEnumerator routine, bool autoStart = true)
		{
			m_Behaviour = behaviour;
			m_Current = routine;
			if (autoStart) Start();
		}

		public RoutineBase(MonoBehaviour behaviour, Func<IEnumerator> routine, bool autoStart = true)
		{
			m_Behaviour = behaviour;
			m_Routine = routine;
			if (autoStart) Start();
		}

		protected abstract void Success();
		protected abstract void Fail(Exception ex);
		protected virtual bool Result(IHasResult result) => false;

		public void Start()
		{
			if (m_Coroutine != null) throw new InvalidOperationException("すでに実行中です。完了後に実行してください。");
			if (m_Current == null)
			{
				if (m_Routine == null) throw new InvalidOperationException("すでに実行済みです");
				m_Current = m_Routine();
			}
			Log.Trace("[ilib-routine] start");
			m_Coroutine = m_Behaviour.StartCoroutine(RoutineImpl());
		}

		public void Cancel()
		{
			if (m_Behaviour == null) return;
			m_Behaviour.StopCoroutine(m_Coroutine);
			m_Behaviour = null;
			m_Coroutine = null;
			Log.Debug("[ilib-routine] cancel");
		}


		IEnumerator RoutineImpl()
		{
			Stack<IEnumerator> enumerators = null;
			var cur = m_Current;
			bool moveNext = true;
			Exception error = null;
			m_Current = null;
			while (cur != null || (enumerators != null && enumerators.Count > 0))
			{
				try
				{
					moveNext = cur.MoveNext();
				}
				catch (Exception ex)
				{
					error = ex;
					break;
				}
				if (!moveNext)
				{
					cur = Next(enumerators);
					continue;
				}
				var obj = cur.Current;
				if (obj is IHasResult)
				{
					var ret = obj as IHasResult;
					if (Result(ret))
					{
						m_Coroutine = null;
						yield break;
					}
					else if(ret.Next)
					{
						continue;
					}
				}
				if (obj is IEnumerator)
				{
					if (enumerators == null) enumerators = StackPool.Borrow();
					enumerators.Push(cur);
					cur = (IEnumerator)obj;
				}
				else
				{
					yield return obj;
				}
			}
			m_Coroutine = null;
			if (enumerators != null)
			{
				StackPool.Return(enumerators);
				enumerators = null;
			}
			if (error != null)
			{
				Log.Warning("[ilib-routine] handle error {0}", error);
				Fail(error);
			}
			else
			{
				Success();
			}
		}

		IEnumerator Next(Stack<IEnumerator> enumerators)
		{
			if (enumerators != null && enumerators.Count > 0)
			{
				return enumerators.Pop();
			}
			return null;
		}

		void IDisposable.Dispose()
		{
			Cancel();
		}

	}

}
