using System.Linq;
using System.Threading.Tasks;

namespace ILib.Contents
{
	public class CombineModule : Module
	{
		Module[] m_Modules;

		public CombineModule(Module[] modules) => m_Modules = modules;

		public override ModuleType Type
		{
			get
			{
				var type = ModuleType.None;
				foreach (var m in m_Modules)
				{
					type |= m.Type;
				}
				return type;
			}
		}

		public override Task OnPreBoot(Content content) => Task.WhenAll(m_Modules.Select(x => x.OnPreBoot(content)));

		public override Task OnBoot(Content content) => Task.WhenAll(m_Modules.Select(x => x.OnBoot(content)));

		public override Task OnPreShutdown(Content content) => Task.WhenAll(m_Modules.Select(x => x.OnPreShutdown(content)));

		public override Task OnShutdown(Content content) => Task.WhenAll(m_Modules.Select(x => x.OnShutdown(content)));

		public override Task OnPreRun(Content content) => Task.WhenAll(m_Modules.Select(x => x.OnPreRun(content)));

		public override Task OnRun(Content content) => Task.WhenAll(m_Modules.Select(x => x.OnRun(content)));

		public override Task OnPreSuspend(Content content) => Task.WhenAll(m_Modules.Select(x => x.OnPreSuspend(content)));

		public override Task OnSuspend(Content content) => Task.WhenAll(m_Modules.Select(x => x.OnSuspend(content)));

		public override Task OnPreEnable(Content content) => Task.WhenAll(m_Modules.Select(x => x.OnPreEnable(content)));

		public override Task OnEnable(Content content) => Task.WhenAll(m_Modules.Select(x => x.OnEnable(content)));

		public override Task OnPreDisable(Content content) => Task.WhenAll(m_Modules.Select(x => x.OnPreDisable(content)));

		public override Task OnDisable(Content content) => Task.WhenAll(m_Modules.Select(x => x.OnDisable(content)));

		public override Task OnPreSwitch(Content prev, Content next) => Task.WhenAll(m_Modules.Select(x => x.OnPreSwitch(prev, next)));

		public override Task OnSwitch(Content prev, Content next) => Task.WhenAll(m_Modules.Select(x => x.OnSwitch(prev, next)));

		public override Task OnEndSwitch(Content prev, Content next) => Task.WhenAll(m_Modules.Select(x => x.OnEndSwitch(prev, next)));
	}

}
