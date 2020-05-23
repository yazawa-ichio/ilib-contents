using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ILib.Managed
{
	public interface IHasManagedHolder
	{
		ManagedHolder Managed { get; }
	}

	public static class IHasManagedHolderExtensions
	{
		public static T Manage<T>(this IHasManagedHolder self, T disposable) where T : IDisposable
		{
			return self.Managed.Manage(disposable);
		}

		public static GameObject Manage(this IHasManagedHolder self, GameObject obj)
		{
			return self.Managed.Manage(obj);
		}

		public static IDisposable Manage(this IHasManagedHolder self, Action action)
		{
			return self.Managed.Manage(action);
		}

		public static Task<T> Manage<T>(this IHasManagedHolder self, Func<CancellationToken, Task<T>> func)
		{
			return self.Managed.Manage(func);
		}

		public static CancellationTokenSource Manage(this IHasManagedHolder self, CancellationTokenSource source)
		{
			return self.Managed.Manage(source);
		}

		public static CancellationToken Manage(this IHasManagedHolder self, CancellationToken token)
		{
			return self.Managed.Manage(token);
		}

		public static T ManageAsync<T>(this IHasManagedHolder self, T disposable) where T : IManagedAsyncDisposable
		{
			return self.Managed.ManageAsync(disposable);
		}

		public static T ManageComponent<T>(this IHasManagedHolder self, T component) where T : Component
		{
			return self.Managed.ManageComponent(component);
		}

		public static T Unmanage<T>(this IHasManagedHolder self, T disposable, bool doDispose = true) where T : IDisposable
		{
			return self.Managed.Unmanage(disposable, doDispose);
		}

		public static void Unmanage(this IHasManagedHolder self, IManagedAsyncDisposable disposable, bool doDispose = true)
		{
			self.Managed.Unmanage(disposable, doDispose);
		}

	}
}