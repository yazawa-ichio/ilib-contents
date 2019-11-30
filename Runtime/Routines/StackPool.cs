using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ILib.Routines
{

	internal class StackPool
	{
		static Stack<Stack<IEnumerator>> s_Pool = new Stack<Stack<IEnumerator>>(2);

		public static Stack<IEnumerator> Borrow()
		{
			if (s_Pool.Count > 0) return s_Pool.Pop();
			return new Stack<IEnumerator>(2);
		}

		public static void Return(Stack<IEnumerator> stack)
		{
			if (s_Pool.Count > 8) return;
			s_Pool.Push(stack);
		}

	}

}
