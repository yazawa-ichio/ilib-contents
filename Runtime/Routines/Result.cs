namespace ILib.Routines
{
	/// <summary>
	/// Routineによる結果を渡す際に使用します。
	/// </summary>
	public interface IHasResult
	{
		object Value { get; }
		bool Next { get; }
	}

	/// <summary>
	/// 指定の型の結果をRoutineで返す際に利用します。
	/// </summary>
	public interface IHasResult<T> : IHasResult
	{
		new T Value { get; }
	}

	/// <summary>
	/// T型のRoutineの結果を保持しています
	/// </summary>
	public class Result<T> : IHasResult<T>
	{
		public static IHasResult<T> Create(T obj, bool next = true) => new Result<T>(obj, next);
		public T Value { get; private set; }
		public bool Next { get; private set; }
		object IHasResult.Value => Value;
		public Result(T value, bool next)
		{
			Value = value;
			Next = next;
		}
	}

	/// <summary>
	/// 自信をそのままIHasResultとして扱いたい場合に継承します。
	/// </summary>
	public class ResultBase<T> : IHasResult<T> where T : ResultBase<T>
	{
		T IHasResult<T>.Value => (T)this;
		object IHasResult.Value => this;
		public virtual bool Next { get { return true; } set { } }
	}

}
