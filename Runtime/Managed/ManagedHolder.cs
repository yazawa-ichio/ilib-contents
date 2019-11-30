using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace ILib.Managed
{
	/// <summary>
	/// 解放可能なリソースを管理します。
	/// </summary>
	public class ManagedHolder : IManagedAsyncDisposable
	{
		bool m_Disposed = false;
		List<IDisposable> m_Disposable = new List<IDisposable>();
		List<IManagedAsyncDisposable> m_AsnycDisposable = new List<IManagedAsyncDisposable>();
		CancellationTokenSource m_Cancellation = new CancellationTokenSource();

		public CancellationToken CancellationToken => m_Cancellation.Token;

		public IEnumerable<T> Get<T>()
		{
			foreach (var item in m_Disposable)
			{
				if (item is T ret) yield return ret;
			}
			foreach (var item in m_AsnycDisposable)
			{
				if (item is T ret) yield return ret;
			}
		}

		public T Manage<T>(T disposable) where T :IDisposable
		{
			if (m_Disposed)
			{
				disposable?.Dispose();
				return disposable;
			}
			m_Disposable.Add(disposable);
			return disposable;
		}

		public GameObject Manage(GameObject obj)
		{
			if (m_Disposed)
			{
				GameObject.Destroy(obj);
				return obj;
			}
			m_Disposable.Add(new GameObjectDisposer(obj));
			return obj;
		}

		public IDisposable Manage(Action action)
		{
			if (m_Disposed)
			{
				action?.Invoke();
				return null;
			}
			var ret = new ActionDisposer(action);
			m_Disposable.Add(ret);
			return ret;
		}

		public Task<T> Manage<T>(Func<CancellationToken, Task<T>> func)
		{
			return func(m_Cancellation.Token);
		}

		public CancellationTokenSource Manage(CancellationTokenSource source)
		{
			m_Cancellation.Token.Register(() =>
			{
				if (!source.IsCancellationRequested)
				{
					source.Cancel();
				}
			});
			return source;
		}

		public CancellationToken Manage(CancellationToken token)
		{
			if (token == CancellationToken.None) return CancellationToken;
			return CancellationTokenSource.CreateLinkedTokenSource(token, m_Cancellation.Token).Token;
		}

		public T ManageAsync<T>(T disposable) where T : IManagedAsyncDisposable
		{
			if (m_Disposed)
			{
				disposable?.DisposeAsync();
				return disposable;
			}
			m_AsnycDisposable.Add(disposable);
			return disposable;
		}

		public T ManageComponent<T>(T component) where T : Component
		{
			if (m_Disposed)
			{
				Component.Destroy(component);
				return component;
			}
			m_Disposable.Add(new ComponentDisposer(component));
			return component;
		}

		public T Unmanage<T>(T disposable, bool doDispose = true) where T : IDisposable
		{
			if (m_Disposed) return disposable;
			m_Disposable.Remove(disposable);
			if (doDispose)
			{
				disposable?.Dispose();
			}
			return disposable;
		}

		public async void Unmanage(IManagedAsyncDisposable disposable, bool doDispose = true)
		{
			if (m_Disposed) return;
			m_AsnycDisposable.Remove(disposable);
			try
			{
				await disposable.DisposeAsync();
			}
			finally { }
		}

		public async Task DisposeAsync()
		{
			if (m_Disposed) return;
			m_Disposed = true;
			List<Exception> errors = new List<Exception>();
			foreach (var disposable in m_Disposable)
			{
				try
				{
					disposable?.Dispose();
				}
				catch (Exception ex)
				{
					errors.Add(ex);
				}
			}
			m_Disposable.Clear();
			try
			{
				await Task.WhenAll(m_AsnycDisposable.Select(x => x.DisposeAsync()));
			}
			catch (Exception ex)
			{
				errors.Add(ex);
			}
			try
			{
				m_Cancellation.Cancel();
			}
			catch (Exception ex)
			{
				errors.Add(ex);
			}
			m_Cancellation.Dispose();
			if (errors.Count > 0)
			{
				throw errors[0];
			}
		}

	}
}
