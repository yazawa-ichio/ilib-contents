using System;

namespace ILib.Caller
{
	public class HandleAttribute : Attribute
	{
		public string Key { get; private set; }
		public HandleAttribute(object obj) { Key = EventCall.ToKey(obj); }
	}

}