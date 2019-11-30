using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace ILib.Contents
{
	using System.Threading.Tasks;
	using Log = ContentsLog;

	/// <summary>
	/// モジュールのコレクションです。
	/// 親が指定されている場合、親のモジュールも実行されます。
	/// </summary>
	public class ModuleCollection : LockCollection<Module>
	{
		ModuleCollection m_Parent;
		ModuleType m_CollectionType = ModuleType.None;

		public ModuleType Type
		{
			get
			{
				if (m_Parent == null) return m_CollectionType;
				return m_Parent.Type | m_CollectionType;
			}
		}

		public ModuleCollection() { }

		public ModuleCollection(ModuleCollection parent)
		{
			m_Parent = parent;
		}

		void UpdateType()
		{
			ModuleType type = ModuleType.None;
			foreach (var module in this)
			{
				type |= module.Type;
			}
			m_CollectionType = type;
		}

		public void Add<T>() where T : Module, new()
		{
			Add(new T());
		}

		public void Remove<T>() where T : Module
		{
			var module = Get<T>();
			if (module != null) Remove(module);
		}

		public T Get<T>() where T : Module
		{
			return this.FirstOrDefault(x => x is T) as T;
		}

		public IEnumerable<T> GetModules<T>() where T : Module
		{
			return this.Select(x => x as T).Where(x => x != null);
		}

		public override void Add(Module child)
		{
			Log.Debug("[ilib-content]add module {0}", child);
			base.Add(child);
			UpdateType();
		}

		public override void Remove(Module child)
		{
			Log.Debug("[ilib-content]remove module {0}", child);
			base.Remove(child);
			UpdateType();
		}

		IEnumerable<Task> Iterate(ModuleType type, Func<Module, Task> func)
		{
			if (m_Parent != null)
			{
				foreach (var task in m_Parent.Iterate(type, func))
				{
					yield return task;
				}
			}
			foreach (var module in this)
			{
				if (module.Type.HasFlag(type))
				{
					var task = func(module);
					if (task != null) yield return task;
				}
			}
		}

		public Task OnPreBoot(Content content) => Task.WhenAll(Iterate(ModuleType.PreBoot, x => x.OnPreBoot(content)));

		public Task OnBoot(Content content) => Task.WhenAll(Iterate(ModuleType.Boot, x => x.OnBoot(content)));

		public Task OnPreShutdown(Content content) => Task.WhenAll(Iterate(ModuleType.PreShutdown, x => x.OnPreShutdown(content)));

		public Task OnShutdown(Content content) => Task.WhenAll(Iterate(ModuleType.Shutdown, x => x.OnShutdown(content)));

		public Task OnPreRun(Content content) => Task.WhenAll(Iterate(ModuleType.PreRun, x => x.OnPreRun(content)));

		public Task OnRun(Content content) => Task.WhenAll(Iterate(ModuleType.Run, x => x.OnRun(content)));

		public Task OnPreSuspend(Content content) => Task.WhenAll(Iterate(ModuleType.PreSuspend, x => x.OnPreSuspend(content)));

		public Task OnSuspend(Content content) => Task.WhenAll(Iterate(ModuleType.Suspend, x => x.OnSuspend(content)));

		public Task OnPreEnable(Content content) => Task.WhenAll(Iterate(ModuleType.PreEnable, x => x.OnPreEnable(content)));

		public Task OnEnable(Content content) => Task.WhenAll(Iterate(ModuleType.Enable, x => x.OnEnable(content)));

		public Task OnPreDisable(Content content) => Task.WhenAll(Iterate(ModuleType.PreDisable, x => x.OnPreDisable(content)));

		public Task OnDisable(Content content) => Task.WhenAll(Iterate(ModuleType.Disable, x => x.OnDisable(content)));

		public Task OnPreSwitch(Content prev, Content next) => Task.WhenAll(Iterate(ModuleType.PreSwitch, x => x.OnPreSwitch(prev, next)));

		public Task OnSwitch(Content prev, Content next) => Task.WhenAll(Iterate(ModuleType.Switch, x => x.OnSwitch(prev, next)));

		public Task OnEndSwitch(Content prev, Content next) => Task.WhenAll(Iterate(ModuleType.EndSwitch, x => x.OnEndSwitch(prev, next)));

	}

}
