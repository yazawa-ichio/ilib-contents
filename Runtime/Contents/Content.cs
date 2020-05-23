using System.Collections;
using System.Collections.Generic;
using System;

namespace ILib.Contents
{
	using Caller;
	using Routines;
	using System.Threading.Tasks;
	using System.Threading;
	using ILib.Managed;
	using Log = Contents.ContentsLog;

	public class Content<T> : Content
	{
		protected new T Param => (T)base.Param;
	}

	public interface IModalContent
	{
	}

	public interface IModalContent<T> : IModalContent
	{
		Task<T> GetModalResult(CancellationToken token);
	}

	[UnityEngine.Scripting.Preserve]
	public abstract partial class Content
	{
		Content m_Parent;

		/// <summary>
		/// 親のコンテンツです。
		/// </summary>
		protected Content Parent { get; private set; }

		/// <summary>
		/// 所属するコントローラーです。
		/// </summary>
		protected ContentsController Controller { get; private set; }

		/// <summary>
		/// リソースのホルダーです
		/// </summary>
		protected ManagedHolder Managed { get; private set; } = new ManagedHolder();

		LockCollection<Content> m_Children = new LockCollection<Content>();

		public bool HasChildren => m_Children.Count > 0;

		bool m_Shutdown;
		TransLock m_TransLock = new TransLock();
		EventHandle m_CallHandle;

		/// <summary>
		/// モーダルコンテンツです。
		/// </summary>
		protected bool IsModalContent => this is IModalContent;

		/// <summary>
		/// モーダルコンテンツを待っています。
		/// </summary>
		protected bool IsWaitModalContent
		{
			get
			{
				foreach (var c in m_Children)
				{
					if (c.IsModalContent)
					{
						return true;
					}
				}
				return false;
			}
		}

		protected object Param { get; private set; }

		/// <summary>
		/// 実行中か？
		/// </summary>
		protected bool Running { get; private set; }

		/// <summary>
		/// 自身と子を対象とするモジュールです。
		/// </summary>
		public ModuleCollection Modules { get; private set; }

		/// <summary>
		/// イベントの発火装置です
		/// </summary>
		protected IDispatcher Dispatcher => Controller.Dispatcher;

		/// <summary>
		/// イベントの発火装置です
		/// </summary>
		protected EventCall EventCall => Controller.EventCall;

		/// <summary>
		/// 起動処理です。
		/// </summary>
		protected virtual Task OnBoot() => Util.Successed;
		/// <summary>
		/// 有効時の処理です。
		/// </summary>
		protected virtual Task OnEnable() => Util.Successed;
		/// <summary>
		/// 実行時の処理です。
		/// </summary>
		protected virtual Task OnRun() => Util.Successed;
		/// <summary>
		/// 実行処理が完了した際の処理です。
		/// </summary>
		protected virtual void OnCompleteRun() { }
		/// <summary>
		/// 停止時の処理です。
		/// </summary>
		protected virtual Task OnSuspend() => Util.Successed;
		/// <summary>
		/// 無効時の処理です。
		/// </summary>
		protected virtual Task OnDisable() => Util.Successed;
		/// <summary>
		/// 終了直前の処理です。
		/// </summary>
		protected virtual void OnPreShutdown() { }
		/// <summary>
		/// 終了時の処理です。
		/// </summary>
		protected virtual Task OnShutdown() => Util.Successed;
		/// <summary>
		/// 例外をハンドリングします
		/// </summary>
		protected virtual bool IsHandleException => true;

		/// <summary>
		/// 自身の子にコンテンツを追加します。
		/// </summary>
		public Task<Content> Append(IContentParam prm)
		{
			return Append(prm.GetContentType(), prm);
		}

		/// <summary>
		/// 自身の子にコンテンツを追加します。
		/// </summary>
		public Task<Content> Append<T>(object prm)
		{
			return Append(typeof(T), prm);
		}

		/// <summary>
		/// 自身の子にコンテンツを追加します。
		/// </summary>
		public Task<Content> Append(Type type, object prm)
		{
			if (IsWaitModalContent)
			{
				throw new InvalidOperationException("wait modal content do not use Append");
			}
			Log.Debug("[ilib-content]start Append(Type:{0},param:{1})", type, prm);
			var content = (Content)Activator.CreateInstance(type);
			m_Children.Add(content);
			return !IsHandleException ? content.Boot(Controller, this, prm) : Handle(content.Boot(Controller, this, prm));
		}

		/// <summary>
		/// モーダルとして子のコンテンツを追加します。
		/// 追加したコンテンツの結果を待ちます。
		/// </summary>
		public Task<TResult> Modal<TResult>(IContentParam prm, CancellationToken token = default)
		{
			return Modal<TResult>(prm.GetContentType(), prm, token);
		}

		/// <summary>
		/// モーダルとして子のコンテンツを追加します。
		/// 追加したコンテンツの結果を待ちます。
		/// </summary>
		public Task<TResult> Modal<TResult, UContent>(object prm = null, CancellationToken token = default) where UContent : Content, IModalContent<TResult>
		{
			return Modal<TResult>(typeof(UContent), prm);
		}

		/// <summary>
		/// モーダルとして子のコンテンツを追加します。
		/// 追加したコンテンツの結果を待ちます。
		/// </summary>
		/// <summary>
		/// モーダルとして子のコンテンツを追加します。
		/// 追加したコンテンツの結果を待ちます。
		/// </summary>
		public Task<TResult> Modal<TResult>(Type type, object prm = null, CancellationToken token = default)
		{
			return IsHandleException ? ModalImpl<TResult>(type, prm, token) : Handle(ModalImpl<TResult>(type, prm, token));
		}

		async Task<TResult> ModalImpl<TResult>(Type type, object prm = null, CancellationToken token = default)
		{

			if (!typeof(IModalContent<TResult>).IsAssignableFrom(type))
			{
				throw new ArgumentException($"not modal content {type}", nameof(type));
			}

			if (IsWaitModalContent)
			{
				throw new InvalidOperationException("wait modal content do not use Modal");
			}

			token = Managed.Manage(token);

			Log.Debug("[ilib-content]start Modal<{0}>(Type:{1},param:{2})", typeof(TResult), type, prm);

			var content = (Content)Activator.CreateInstance(type);
			m_Children.Add(content);
			await content.Boot(Controller, this, prm);
			token.ThrowIfCancellationRequested();

			Log.Trace("[ilib-content]run GetModalResult");
			var ret = await (content as IModalContent<TResult>).GetModalResult(token);
			token.ThrowIfCancellationRequested();

			Log.Trace("[ilib-content]shutdown Modal");
			await content.DoShutdown();
			token.ThrowIfCancellationRequested();

			return ret;
		}

		/// <summary>
		/// 停止後の復帰処理を行います。
		/// </summary>
		public Task Resume() => !IsHandleException ? DoRun() : Handle(DoRun());

		/// <summary>
		/// 停止処理を開始します。
		/// </summary>
		public Task Suspend() => !IsHandleException ? DoSuspend() : Handle(DoSuspend());

		/// <summary>
		/// 終了処理を開始します。
		/// </summary>
		public Task Shutdown() => !IsHandleException ? DoShutdown() : Handle(DoShutdown());

		/// <summary>
		/// 終了処理と指定コンテンツへの遷移を開始します。
		/// </summary>
		public Task<Content> Switch(IContentParam prm) => Switch(prm.GetContentType(), prm);

		/// <summary>
		/// 終了処理と指定コンテンツへの遷移を開始します。
		/// </summary>
		public Task<Content> Switch<T>(object prm = null) => Switch(typeof(T), prm);

		/// <summary>
		/// 終了処理と指定コンテンツへの遷移を開始します。
		/// </summary>
		public Task<Content> Switch(Type type, object prm = null)
		{
			if (IsWaitModalContent)
			{
				throw new InvalidOperationException("wait modal content. not use Switch");
			}
			if (IsModalContent)
			{
				throw new InvalidOperationException("modal content not use Switch. use ResultModal.");
			}
			return !IsHandleException ? DoSwitch(type, prm) : Handle(DoSwitch(type, prm));
		}

		bool HasModule(ModuleType type) => (type & Modules.Type) == type;

		void PreBoot(ContentsController controller, Content parent, object prm)
		{
			Param = prm;
			Controller = controller;
			m_Parent = parent;
			if (m_Parent != null)
			{
				Parent = m_Parent;
			}
			Modules = m_Parent != null ? new ModuleCollection(m_Parent.Modules) : new ModuleCollection(Controller.Modules);
			m_CallHandle = Controller.EventCall.Bind(this);
		}

		internal async Task<Content> Boot(ContentsController controller, Content parent, object prm)
		{
			PreBoot(controller, parent, prm);
			using (m_TransLock.Lock(TransLockFlag.Boot))
			{
				if (HasModule(ModuleType.PreBoot)) await Modules.OnPreBoot(this);
				await (OnBoot() ?? Util.Successed);
				if (HasModule(ModuleType.Boot)) await Modules.OnBoot(this);
				await DoRun();
			}

			return this;
		}

		async Task DoEnable()
		{
			Log.Trace("[ilib-content] DoEnable {0}", this);
			using (m_TransLock.Lock(TransLockFlag.EnableOrDisable))
			{
				if (Running) return;
				Running = true;
				m_CallHandle.Enabled = true;
				if (HasModule(ModuleType.PreEnable)) await Modules.OnPreEnable(this);
				await (OnEnable() ?? Util.Successed);
				foreach (var child in m_Children)
				{
					await child.DoEnable();
				}
				if (HasModule(ModuleType.Enable)) await Modules.OnEnable(this);
			}
		}

		async Task DoRun()
		{
			Log.Trace("[ilib-content] DoRun {0}", this);
			using (m_TransLock.Lock(TransLockFlag.RunOrSuspend))
			{
				if (m_Shutdown || Running) return;
				if (HasModule(ModuleType.PreRun)) await Modules.OnPreRun(this);
				await DoEnable();
				await (OnRun() ?? Util.Successed);
				if (HasModule(ModuleType.Run)) await Modules.OnRun(this);
				OnCompleteRun();
			}
		}

		async Task DoDisable()
		{
			Log.Trace("[ilib-content] DoDisable {0}", this);
			using (m_TransLock.Lock(TransLockFlag.EnableOrDisable))
			{
				if (!Running) return;
				Running = false;
				m_CallHandle.Enabled = false;
				if (HasModule(ModuleType.PreDisable)) await Modules.OnPreDisable(this);
				foreach (var child in m_Children)
				{
					await child.DoDisable();
				}
				await (OnDisable() ?? Util.Successed);
				if (HasModule(ModuleType.Disable)) await Modules.OnDisable(this);
			}
		}

		async Task DoSuspend()
		{
			Log.Trace("[ilib-content] DoSuspend {0}", this);
			using (m_TransLock.Lock(TransLockFlag.RunOrSuspend))
			{
				if (m_Shutdown || !Running) return;
				if (HasModule(ModuleType.PreSuspend)) await Modules.OnPreSuspend(this);
				await DoDisable();
				await (OnSuspend() ?? Util.Successed);
				if (HasModule(ModuleType.Suspend)) await Modules.OnSuspend(this);
			}
		}

		internal async Task DoShutdown()
		{
			if (m_Shutdown) throw new InvalidOperationException("already shutdown.");
			Log.Trace("[ilib-content] DoShutdown {0}", this);
			m_Shutdown = true;

			m_CallHandle?.Dispose();

			//解除は親に任せる
			m_Parent?.m_Children.Remove(this);
			using (m_TransLock.Lock(TransLockFlag.Shutdown))
			{
				OnPreShutdown();
				if (HasModule(ModuleType.PreShutdown)) await Modules.OnPreShutdown(this);
				foreach (var child in m_Children)
				{
					await child.DoShutdown();
				}
				await Managed.DisposeAsync();
				await (OnShutdown() ?? Util.Successed);
				if (HasModule(ModuleType.Shutdown)) await Modules.OnShutdown(this);
			}
		}

		async Task<Content> DoSwitch(Type type, object prm)
		{
			Log.Trace("[ilib-content] DoSwitch {0} > {1}, prm:{2}", this, type, prm);

			var next = (Content)Activator.CreateInstance(type);
			if (m_Parent != null)
			{
				m_Parent.m_Children.Add(next);
			}
			next.PreBoot(Controller, m_Parent, prm);

			if (HasModule(ModuleType.PreSwitch)) await Modules.OnPreSwitch(this, next);

			await DoShutdown();

			if (next.HasModule(ModuleType.Switch)) await next.Modules.OnSwitch(this, next);

			using (next.m_TransLock.Lock(TransLockFlag.Boot))
			{
				if (next.HasModule(ModuleType.PreBoot)) await next.Modules.OnPreBoot(next);

				await (next.OnBoot() ?? Util.Successed);

				if (next.HasModule(ModuleType.Boot)) await next.Modules.OnBoot(next);

				if (next.HasModule(ModuleType.EndSwitch)) await next.Modules.OnEndSwitch(this, next);

				await (next.DoRun() ?? Util.Successed);
			}
			//遷移先を送信
			return next;
		}


		/// <summary>
		/// 子に登録されている指定のタイプを取得します。
		/// </summary>
		public T Get<T>(bool recursive = true)
		{
			foreach (var child in m_Children)
			{
				if (child is T ret) return ret;
			}
			if (recursive)
			{
				foreach (var child in m_Children)
				{
					var ret = child.Get<T>(recursive);
					if (ret != null)
					{
						return ret;
					}
				}
			}
			return default;
		}

		/// <summary>
		/// 子に登録されている指定のタイプをすべて取得します。
		/// </summary>
		public IEnumerable<T> GetAll<T>(bool recursive = true)
		{
			foreach (var child in m_Children)
			{
				if (child is T ret) yield return ret;
				if (recursive)
				{
					foreach (var c in child.GetAll<T>())
					{
						yield return c;
					}
				}
			}
		}


		protected Routine Routine(IEnumerator enumerator)
		{
			var routine = Controller.Routine(enumerator);
			Managed.Manage(routine);
			return routine;
		}

		protected TaskRoutine<T> TaskRoutine<T>(IEnumerator enumerator)
		{
			var routine = Controller.TaskRoutine<T>(enumerator);
			Managed.Manage(routine);
			return routine;
		}

		protected async Task<T> Handle<T>(Task<T> task, bool handleException = true)
		{
			try
			{
				return await task;
			}
			catch (Exception ex)
			{
				if (handleException) ThrowException(ex);
				throw ex;
			}
		}

		protected async Task Handle(Task task, bool handleException = true)
		{
			try
			{
				await task;
			}
			catch (Exception ex)
			{
				if (handleException) ThrowException(ex);
				throw ex;
			}
		}

		/// <summary>
		/// 例外をスローします。
		/// ハンドリングされない場合、親へと投げられます。
		/// </summary>
		/// <param name="exception"></param>
		protected bool ThrowException(Exception exception)
		{
			Log.Debug("[ilib-content] this:{0}, ThrowException:{1}", this, exception);
			bool ret = false;
			try
			{
				ret = HandleException(exception);
			}
			catch (Exception e)
			{
				if (m_Parent != null)
				{
					m_Parent.ThrowException(exception);
				}
				else
				{
					Controller.ThrowException(exception);
				}
				//別のErrorなのでスローする
				throw e;
			}
			if (!ret)
			{
				if (m_Parent != null)
				{
					return m_Parent.ThrowException(exception);
				}
				else
				{
					return Controller.ThrowException(exception);
				}
			}
			return false;
		}

		protected virtual bool HandleException(Exception ex)
		{
			return false;
		}

	}
}