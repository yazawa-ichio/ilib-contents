using System;

namespace ILib.Contents
{
	public interface IContentParam
	{
		Type GetContentType();
	}

	public class SimpleParam : IContentParam
	{
		public static SimpleParam Create<T>(object value = null)
		{
			return new SimpleParam(typeof(T), value);
		}

		public Type ContentType { get; private set; }

		public object Value { get; private set; }

		public Type GetContentType() => ContentType;

		public SimpleParam(Type type, object value)
		{
			ContentType = type;
			Value = value;
		}
	}

	public abstract class ContentParam<T> : IContentParam where T : Content
	{
		public virtual Type GetContentType() => typeof(T);
	}
}
