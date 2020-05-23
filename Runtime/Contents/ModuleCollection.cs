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
		ContentsController m_Controller;
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
			m_Controller = parent.m_Controller;
		}

		internal void Set(ContentsController controller)
		{
			m_Controller = controller;
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
			child.Controller = m_Controller;
			UpdateType();
		}

		public override void Remove(Module child)
		{
			Log.Debug("[ilib-content]remove module {0}", child);
			base.Remove(child);
			UpdateType();
		}

		async Task Iterate(ModuleType type, Func<Module, Task> func)
		{
			if (m_Parent != null)
			{
				await m_Parent.Iterate(type, func);
			}
			foreach (var module in this)
			{
				if (module.Type.HasFlag(type))
				{
					var task = func(module);
					if (task != null) await task;
				}
			}
		}

		public Task OnPreBoot(Content content) => Iterate(ModuleType.PreBoot, x => x.OnPreBoot(content));

		public Task OnBoot(Content content) => Iterate(ModuleType.Boot, x => x.OnBoot(content));

		public Task OnPreShutdown(Content content) => Iterate(ModuleType.PreShutdown, x => x.OnPreShutdown(content));

		public Task OnShutdown(Content content) => Iterate(ModuleType.Shutdown, x => x.OnShutdown(content));

		public Task OnPreRun(Content content) => Iterate(ModuleType.PreRun, x => x.OnPreRun(content));

		public Task OnRun(Content content) => Iterate(ModuleType.Run, x => x.OnRun(content));

		public Task OnPreSuspend(Content content) => Iterate(ModuleType.PreSuspend, x => x.OnPreSuspend(content));

		public Task OnSuspend(Content content) => Iterate(ModuleType.Suspend, x => x.OnSuspend(content));

		public Task OnPreEnable(Content content) => Iterate(ModuleType.PreEnable, x => x.OnPreEnable(content));

		public Task OnEnable(Content content) => Iterate(ModuleType.Enable, x => x.OnEnable(content));

		public Task OnPreDisable(Content content) => Iterate(ModuleType.PreDisable, x => x.OnPreDisable(content));

		public Task OnDisable(Content content) => Iterate(ModuleType.Disable, x => x.OnDisable(content));

		public Task OnPreSwitch(Content prev, Content next) => Iterate(ModuleType.PreSwitch, x => x.OnPreSwitch(prev, next));

		public Task OnSwitch(Content prev, Content next) => Iterate(ModuleType.Switch, x => x.OnSwitch(prev, next));

		public Task OnEndSwitch(Content prev, Content next) => Iterate(ModuleType.EndSwitch, x => x.OnEndSwitch(prev, next));

	}

}