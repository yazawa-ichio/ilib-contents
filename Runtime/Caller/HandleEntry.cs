using System;
using System.Collections.Generic;
using System.Reflection;

namespace ILib.Caller
{
	internal class HandleEntry
	{
		static Dictionary<Type, HandleEntry> s_Dic = new Dictionary<Type, HandleEntry>();

		public static HandleEntry Get(Type type)
		{
			HandleEntry entry = null;
			if (!s_Dic.TryGetValue(type, out entry))
			{
				s_Dic[type] = entry = new HandleEntry(type);
			}
			return entry;
		}

		public Dictionary<string, MethodInfo> Methods = new Dictionary<string, MethodInfo>();
		public Dictionary<string, ParameterInfo> Parameters = new Dictionary<string, ParameterInfo>();
		object[] m_Prm = new object[1];

		public HandleEntry(Type type)
		{
			var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			foreach (var method in methods)
			{
				foreach (var handle in method.GetCustomAttributes<HandleAttribute>(true))
				{
					this.Methods[handle.Key] = method;
					var prm = method.GetParameters();
					if (prm == null || prm.Length == 0)
					{
						Parameters[handle.Key] = null;
					}
					else if (prm.Length == 1)
					{
						Parameters[handle.Key] = prm[0];
					}
				}
			}
		}

		public bool Invoke(object instance, string key, object prm)
		{
			MethodInfo method = null;
			if (Methods.TryGetValue(key, out method))
			{
				if (Parameters[key] == null && prm == null)
				{
					method.Invoke(instance, null);
				}
				else if (prm != null && Parameters[key].ParameterType.IsAssignableFrom(prm.GetType()))
				{
					m_Prm[0] = prm;
					method.Invoke(instance, m_Prm);
				}
				return true;
			}
			return false;
		}

	}

}