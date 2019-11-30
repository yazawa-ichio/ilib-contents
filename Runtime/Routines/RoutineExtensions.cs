using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ILib
{
	public interface IHasRoutineOwner
	{
		MonoBehaviour RoutineOwner { get; }
	}

	public static class RoutineExtensions
	{
		/// <summary>
		/// Unityのコルーチンのラッパーです。
		/// 完了と例外を取得できます。
		/// </summary>
		public static ILib.Routine Routine(this MonoBehaviour behaviour, IEnumerator routine)
		{
			return ILib.Routine.Start(behaviour, routine);
		}

		/// <summary>
		/// Unityのコルーチンのラッパーです。
		/// IHasResult[T]型を返すイテレータをコンストラクタで指定してください。
		/// 値が返された時点で完了扱いとなります。
		/// </summary>
		public static ILib.TaskRoutine<T> TaskRoutine<T>(this MonoBehaviour behaviour, IEnumerator routine)
		{
			return ILib.Routine.Task<T>(behaviour, routine);
		}
		
		/// <summary>
		/// Unityのコルーチンのラッパーです。
		/// 完了と例外を取得できます。
		/// </summary>
		public static ILib.Routine Routine(this IHasRoutineOwner self, IEnumerator routine)
		{
			return ILib.Routine.Start(self.RoutineOwner, routine);
		}
		
		/// <summary>
		/// Unityのコルーチンのラッパーです。
		/// IHasResult[T]型を返すイテレータをコンストラクタで指定してください。
		/// 値が返された時点で完了扱いとなります。
		/// </summary>
		public static ILib.TaskRoutine<T> TaskRoutine<T>(this IHasRoutineOwner self, IEnumerator routine)
		{
			return ILib.Routine.Task<T>(self.RoutineOwner, routine);
		}
		

	}
}
