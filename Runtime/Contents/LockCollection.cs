using System.Collections;
using System.Collections.Generic;

namespace ILib.Contents
{
	/// <summary>
	/// 主にUnityのコルーチンでコレクションを扱うため
	/// 実行中の要素の追加・削除を安全に行うためのコレクション
	/// </summary>
	public class LockCollection<T> : IEnumerable<T>
	{
		List<T> m_List;
		List<T> m_Remove;
		int m_Lock;

		public int Count => m_List != null ? m_List.Count : 0;

		public virtual void Add(T child)
		{
			if (m_List == null) m_List = new List<T>(4);
			m_List.Add(child);
		}

		public virtual void Remove(T child)
		{
			if (m_Lock > 0)
			{
				if (m_Remove == null) m_Remove = new List<T>(2);
				m_Remove.Add(child);
			}
			else
			{
				m_List.Remove(child);
			}
		}

		void Lock()
		{
			m_Lock++;
		}

		void Unlock()
		{
			m_Lock--;
			if (m_Lock > 0 || m_Remove == null) return;
			for (int i = 0; i < m_Remove.Count; i++)
			{
				m_List.Remove(m_Remove[i]);
			}
			m_Remove.Clear();
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Iterator(this);
		IEnumerator IEnumerable.GetEnumerator() => new Iterator(this);

		class Iterator : IEnumerator<T>
		{
			public T Current { get; private set; }
			object IEnumerator.Current => Current;
			LockCollection<T> m_Owner;
			int m_Index;
			bool m_Locked;

			public Iterator(LockCollection<T> owner) => m_Owner = owner;

			public void Dispose()
			{
				if (m_Locked)
				{
					m_Owner.Unlock();
					m_Locked = false;
				}
				m_Owner = null;
				Current = default;
			}

			public bool MoveNext()
			{

				if (m_Owner == null) return false;
				var list = m_Owner.m_List;
				if (list == null || m_Index >= list.Count) return false;
				if (!m_Locked)
				{
					m_Owner.Lock();
					m_Locked = true;
				}
				while (m_Index < list.Count)
				{
					Current = list[m_Index++];
					if (m_Owner.m_Remove == null || !m_Owner.m_Remove.Contains(Current))
					{
						return true;
					}
				}
				return false;
			}

			public void Reset()
			{
				throw new System.NotImplementedException();
			}
		}


	}
}
