using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Assert = UnityEngine.Assertions.Assert;
using ILib;
using ILib.Contents;
using ILib.Managed;
using UnityEngine.TestTools;
using System;
using ILib.Caller;
using System.Threading.Tasks;
using System.Threading;

public class ContentsTest
{

	class Tester : IDisposable, IHasDispatcher
	{
		GameObject m_obj;
		public ContentsController Controller { get; private set; }

		public IDispatcher Dispatcher => Controller.Dispatcher;

		public Tester()
		{
			EventCall call = new EventCall();
			m_obj = new GameObject("Tester");
			Controller = m_obj.AddComponent<ContentsController>();
		}

		public IEnumerator Close()
		{
			var task = Controller.Shutdown(); ;
			while (!task.IsCompleted) yield return null;
		}

		public void Dispose()
		{
			GameObject.Destroy(m_obj);
		}

	}

	class Counter
	{
		Dictionary<string, int> m_dic = new Dictionary<string, int>();
		public void Add(string key)
		{
			var val = Get(key);
			m_dic[key] = val + 1;
			Debug.Log(key + ":" + (val + 1));

		}
		public int Get(string key)
		{
			int val = 0;
			m_dic.TryGetValue(key, out val);
			return val;
		}
		public void Clear(string key)
		{
			m_dic.Remove(key);
		}
		public void Clear()
		{
			m_dic.Clear();
		}
		public IEnumerator Wait(string key, int target)
		{
			while (Get(key) < target) yield return null;
		}
	}

	class MainTestParam : ContentParam<MainTestContent>
	{
		public float BootWait { get; set; }
		public Counter Counter { get; set; } = new Counter();
	}

	class MainTestContent : Content<MainTestParam>, IHasManagedHolder
	{
		ManagedHolder IHasManagedHolder.Managed => Managed;

		public enum Event
		{
			Switch,
			Append,
		}

		protected override async Task OnBoot()
		{
			if (Param.BootWait > 0)
			{
				await Task.Delay((int)(Param.BootWait * 1000f));
				Param.Counter.Add("Boot");
			}
			else
			{
				Param.Counter.Add("Boot");
			}
		}

		protected override Task OnEnable()
		{
			Param.Counter.Add("Enable");
			return Util.Successed;
		}

		protected override Task OnRun()
		{
			Param.Counter.Add("Run");
			return Util.Successed;
		}

		[Handle(Event.Switch)]
		void OnSwitch(Counter counter)
		{
			Switch(new MainTestParam { Counter = counter });
		}

		[Handle(Event.Append)]
		void OnAppend(IContentParam prm)
		{
			Append(prm);
		}

		public async Task<T> RunModal<T>(Func<T> ret)
		{
			ModalParam param = new ModalParam();
			param.GetResult = () => (object)ret();
			return (T)await Modal<object, ModalContent>(param);
		}

	}

	class SubParam1 : ContentParam<SubContent1>
	{
		public Counter Counter { get; set; } = new Counter();
	}


	class SubContent1 : Content<SubParam1>
	{
		public enum Event
		{
			Count,
			Suspend,
			Resume,
			Shutdown,
		}

		[Handle(Event.Count)]
		void OnEventCount(string key)
		{
			Param.Counter.Add(key);
		}

		[Handle(Event.Suspend)]
		void OnEventSuspend()
		{
			Suspend();
		}

		[Handle(Event.Resume)]
		void OnEventResume()
		{
			Resume();
		}

	}

	class ModalParam : ContentParam<ModalContent>
	{
		public Func<object> GetResult;
	}

	class ModalContent : Content<ModalParam>, IModalContent<object>
	{

		protected override Task OnBoot()
		{
			return Task.Delay(1000);
		}

		public Task<object> GetModalResult(CancellationToken token)
		{
			return Task.FromResult(Param.GetResult());
		}

	}

	class AsyncDispose : IManagedAsyncDisposable
	{
		public bool Disposed = false;

		public async Task DisposeAsync()
		{
			await Task.Delay(1000);
			Disposed = true;
		}
	}


	public IEnumerator WrapTask(Task task)
	{
		while (!task.IsCompleted) yield return null;
	}

	[UnityTest]
	public IEnumerator BootTest1()
	{
		using (var tester = new Tester())
		{
			var counter = new Counter();
			BootParam prm = new BootParam();
			prm.BootContents.Add(new MainTestParam { Counter = counter });
			tester.Controller.Boot(prm);
			yield return counter.Wait("Run", 1);
			Assert.AreEqual(counter.Get("Boot"), 1);
			Assert.AreEqual(counter.Get("Enable"), 1);
			Assert.AreEqual(counter.Get("Run"), 1);
			yield return tester.Close();
		}
	}
	[UnityTest]
	public IEnumerator BootTest2()
	{
		using (var tester = new Tester())
		{
			//逐次起動
			var counter = new Counter();
			BootParam prm = new BootParam();
			const int num = 5;
			for (int i = 0; i < num; i++)
			{
				prm.BootContents.Add(new MainTestParam { Counter = counter, BootWait = 0.5f });
			}
			tester.Controller.Boot(prm);
			for (int i = 1; i < num + 1; i++)
			{
				//少し待つ
				yield return null;
				yield return counter.Wait("Run", i);
				yield return null;
				//順番に起動する
				Assert.AreEqual(counter.Get("Boot"), i);
				Assert.AreEqual(counter.Get("Enable"), i);
				Assert.AreEqual(counter.Get("Run"), i);
			}
			yield return tester.Close();
		}
	}

	[UnityTest]
	public IEnumerator BootTest3()
	{
		using (var tester = new Tester())
		{
			var counter = new Counter();
			BootParam prm = new BootParam();
			prm.ParallelBoot = true;
			const int num = 5;
			for (int i = 0; i < num; i++)
			{
				prm.BootContents.Add(new MainTestParam { Counter = counter, BootWait = 0.5f });
			}
			tester.Controller.Boot(prm);
			yield return counter.Wait("Run", 1);
			//秒数指定なので少し待つ
			yield return null;
			yield return null;
			//同時に起動している
			Assert.AreEqual(counter.Get("Boot"), num);
			Assert.AreEqual(counter.Get("Enable"), num);
			Assert.AreEqual(counter.Get("Run"), num);
			yield return tester.Close();
		}
	}

	[UnityTest]
	public IEnumerator SwitchTest1()
	{
		using (var tester = new Tester())
		{
			var counter = new Counter();
			BootParam prm = new BootParam();
			prm.BootContents.Add(new MainTestParam { Counter = counter });
			yield return WrapTask(tester.Controller.Boot(prm));
			tester.Message(MainTestContent.Event.Switch, counter);
			yield return counter.Wait("Run", 2);
			yield return tester.Close();
		}
	}

	[UnityTest]
	public IEnumerator SwitchTest2()
	{
		using (var tester = new Tester())
		{
			var counter = new Counter();
			BootParam prm = new BootParam();
			prm.BootContents.Add(new MainTestParam { Counter = counter });
			yield return WrapTask(tester.Controller.Boot(prm));
			//Refからのスイッチ
			yield return WrapTask(tester.Controller.Get<MainTestContent>().Switch(new MainTestParam { Counter = counter, BootWait = 0.5f }));
			//ちゃんと待てる
			Assert.AreEqual(counter.Get("Boot"), 2);
			Assert.AreEqual(counter.Get("Enable"), 2);
			Assert.AreEqual(counter.Get("Run"), 2);
			yield return tester.Close();
		}
	}

	[UnityTest]
	public IEnumerator AppendTest1()
	{
		using (var tester = new Tester())
		{
			var counter = new Counter();
			BootParam prm = new BootParam();
			prm.BootContents.Add(new MainTestParam { Counter = counter });
			yield return WrapTask(tester.Controller.Boot(prm));
			tester.Message<IContentParam>(MainTestContent.Event.Append, new MainTestParam { Counter = counter });
			tester.Message<IContentParam>(MainTestContent.Event.Append, new MainTestParam { Counter = counter });
			tester.Message<IContentParam>(MainTestContent.Event.Append, new MainTestParam { Counter = counter });
			tester.Message<IContentParam>(MainTestContent.Event.Append, new MainTestParam { Counter = counter });
			Debug.Log("counter");
			yield return counter.Wait("Run", 5);
			Debug.Log("counter.Wait(#un, 5);");
			yield return tester.Close();
		}
	}


	[UnityTest]
	public IEnumerator ModalTest1()
	{
		using (var tester = new Tester())
		{
			BootParam prm = new BootParam();
			var counter = new Counter();
			prm.BootContents.Add(new MainTestParam { Counter = counter });
			yield return WrapTask(tester.Controller.Boot(prm));

			var content = tester.Controller.Get<MainTestContent>();
			var modalRet = content.RunModal(() => 124);
			yield return WrapTask(modalRet);
			Assert.AreEqual(124, modalRet.Result);

			modalRet = content.RunModal<int>(() => throw new Exception("error"));
			yield return WrapTask(modalRet);
			Assert.IsNotNull(modalRet.Exception);

		}
	}

	[UnityTest]
	public IEnumerator ModalTest2()
	{
		using (var tester = new Tester())
		{
			BootParam prm = new BootParam();
			var counter = new Counter();
			prm.BootContents.Add(new MainTestParam { Counter = counter });
			yield return WrapTask(tester.Controller.Boot(prm));


			var content = tester.Controller.Get<MainTestContent>();
			var modalRet = content.RunModal(() => true);

			Exception error = null;
			bool ret = false;
			try
			{
				content.Append<SubContent1>(new SubParam1
				{
					Counter = counter
				}).ContinueWith(x => ret = true);
			}
			catch (Exception ex)
			{
				error = ex;
			}
			//モーダル中の追加はエラーになる
			Assert.IsNotNull(error);
			Assert.IsFalse(ret);

			error = null;

			try
			{
				content.Switch<SubContent1>(new SubParam1
				{
					Counter = counter
				}).ContinueWith(x => ret = true);
			}
			catch (Exception ex)
			{
				error = ex;
			}
			//モーダル中の変更はエラーになる
			Assert.IsNotNull(error);
			Assert.IsFalse(ret);

			yield return WrapTask(modalRet);
			Assert.AreEqual(true, modalRet.Result);



		}
	}

	[UnityTest]
	public IEnumerator MangedTest()
	{
		using (var tester = new Tester())
		{
			var counter = new Counter();
			BootParam prm = new BootParam();
			prm.BootContents.Add(new MainTestParam { Counter = counter });
			yield return WrapTask(tester.Controller.Boot(prm));
			var manded = tester.Controller.Get<IHasManagedHolder>();

			bool dispose = false;
			// disposed時にアクションを発火するように登録
			var action = manded.Manage(() => dispose = true);
			Assert.IsFalse(dispose);
			//Unmanageで解放される
			manded.Unmanage(action);
			Assert.IsTrue(dispose);

			//再登録
			dispose = false;
			manded.Manage(() => dispose = true);
			//非同期を登録
			var asyncDispose = new AsyncDispose();
			manded.ManageAsync(asyncDispose);
			tester.Message(MainTestContent.Event.Switch, counter);

			//直ぐには実行されない
			Assert.IsFalse(asyncDispose.Disposed);

			yield return counter.Wait("Run", 2);
			yield return tester.Close();
			Assert.IsTrue(dispose);
			Assert.IsTrue(asyncDispose.Disposed);
		}
	}

}