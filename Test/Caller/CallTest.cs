using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using NUnit.Framework;
using Assert = UnityEngine.Assertions.Assert;

using ILib;
using ILib.Caller;

public class CallTest
{
	enum TestEvent
	{
		Event1,
		Event2,
		Event3,
		Event4,
	}

	[Test]
	public void MessageTest1()
	{
		EventCall call = new EventCall();
		int count = 0;
		var path = call.Subscribe(TestEvent.Event1, () => count++);
		//イベントを発行
		Assert.IsTrue(call.Message(TestEvent.Event1));
		Assert.AreEqual(count, 1);
		//イベントを発行
		Assert.IsTrue(call.Message(TestEvent.Event1));
		Assert.AreEqual(count, 2);
		//登録していないイベント
		Assert.IsFalse(call.Message(TestEvent.Event2));
		Assert.AreEqual(count, 2);
		//パスを解除
		path.Dispose();
		Assert.IsFalse(call.Message(TestEvent.Event1));
		Assert.AreEqual(count, 2);
	}

	[Test]
	public void MessageTest2()
	{
		EventCall call = new EventCall();
		int ret = 0;
		int count = 0;
		var path = call.Subscribe(TestEvent.Event1, (int val) => { ret = val; count++; });
		//イベントを発行
		Assert.IsTrue(call.Message(TestEvent.Event1, 5));
		Assert.AreEqual(ret, 5);
		Assert.AreEqual(count, 1);

		//イベントを発行
		Assert.IsTrue(call.Message(TestEvent.Event1, 7));
		Assert.AreEqual(ret, 7);
		Assert.AreEqual(count, 2);

		//単発イベントでは発火しない
		Assert.IsFalse(call.Message(TestEvent.Event1));
		Assert.AreEqual(ret, 7);
		Assert.AreEqual(count, 2);

		//違う方のイベントでは発火しない
		Assert.IsFalse(call.Message(TestEvent.Event1, 0.4f));
		Assert.AreEqual(ret, 7);
		Assert.AreEqual(count, 2);

		//登録していないイベント
		Assert.IsFalse(call.Message(TestEvent.Event2, 4));
		Assert.AreEqual(count, 2);

		//パスを解除
		path.Dispose();
		Assert.IsFalse(call.Message(TestEvent.Event1, 22));
		Assert.AreEqual(ret, 7);
		Assert.AreEqual(count, 2);
	}

	[Test]
	public void MessageTest3()
	{
		EventCall call = new EventCall();
		int count1 = 0;
		int count2 = 0;
		int count3 = 0;
		var path1 = call.Subscribe(TestEvent.Event1, () => count1++);
		var path2 = call.Subscribe(TestEvent.Event1, () => count2++);
		var path3 = call.Subscribe(TestEvent.Event1, () => count3++);

		//メッセージは一番最初に登録された物が優先される
		for (int i = 0; i < 5; i++)
		{
			Assert.IsTrue(call.Message(TestEvent.Event1));
			Assert.AreEqual(count1, 1 + i);
			Assert.AreEqual(count2, 0);
			Assert.AreEqual(count3, 0);
		}
		//一つ解除する
		path1.Dispose();
		//ブロードキャストは全ての登録イベントに発火する
		for (int i = 0; i < 5; i++)
		{
			Assert.IsTrue(call.Broadcast(TestEvent.Event1));
			Assert.AreEqual(count1, 5);
			Assert.AreEqual(count2, 1 + i);
			Assert.AreEqual(count3, 1 + i);
		}
		path2.Dispose();
		path3.Dispose();
		//全て解除すると発火されない
		for (int i = 0; i < 5; i++)
		{
			Assert.IsFalse(call.Broadcast(TestEvent.Event1));
			Assert.AreEqual(count1, 5);
			Assert.AreEqual(count2, 5);
			Assert.AreEqual(count3, 5);
		}
	}


	[Test]
	public void MessageTest4()
	{
		/*
		EventCall call = new EventCall();
		var trigger = call.Trigger<string>(TestEvent.Event1);
		string val = "";
		int valLength = 0;
		trigger.Add((str) => val = str).Select(x => x.Length).Add(x => valLength = x);
		
		//トリガーとして受け取れる
		Assert.IsTrue(call.Message(TestEvent.Event1, "test"));
		Assert.AreEqual(val, "test");
		Assert.AreEqual(valLength, "test".Length);
		*/
	}

	[Test]
	public void SubCallTest()
	{
		EventCall root = new EventCall();
		EventCall call1 = root.SubCall();
		EventCall call2 = root.SubCall();
		EventCall call3 = root.SubCall();
		EventCall call4 = call1.SubCall();
		EventCall call5 = call4.SubCall();
		int ret = 0;
		var path1 = call1.Subscribe(TestEvent.Event1, () => ret = 1);
		call2.Subscribe(TestEvent.Event1, () => ret = 2);
		call3.Subscribe(TestEvent.Event1, () => ret = 3);
		call4.Subscribe(TestEvent.Event1, () => ret = 4);
		call5.Subscribe(TestEvent.Event1, () => ret = 5);

		//メッセージが伝播してcall1優先される
		Assert.IsTrue(root.Message(TestEvent.Event1));
		Assert.AreEqual(ret, 1);

		//メッセージが優先される
		Assert.IsTrue(call2.Message(TestEvent.Event1));
		Assert.AreEqual(ret, 2);
		Assert.IsTrue(call3.Message(TestEvent.Event1));
		Assert.AreEqual(ret, 3);

		//パスがなくなったので、サブコールのイベントが呼ばれる
		path1.Dispose();
		Assert.IsTrue(root.Message(TestEvent.Event1));
		Assert.AreEqual(ret, 4);

		//子のサブコールも解放される
		call1.Dispose();
		Assert.IsFalse(call5.Message(TestEvent.Event1));
		Assert.AreEqual(ret, 4);

		Assert.IsTrue(root.Message(TestEvent.Event1));
		Assert.AreEqual(ret, 2);

	}

	class EventHolder
	{
		public string value;
		[Handle(TestEvent.Event1)]
		void Event1() => value = "event1";
		[Handle(TestEvent.Event2)]
		void Event2(string val) => value = val;
		[Handle(TestEvent.Event3)]
		void Event3(int val) => value += val.ToString();
	}

	[Test]
	public void BindTest()
	{
		EventHolder holder = new EventHolder();
		EventCall call = new EventCall();
		var handle1 = call.Bind(holder);

		//イベント
		Assert.IsTrue(call.Message(TestEvent.Event1));
		Assert.AreEqual(holder.value, "event1");
		//型が違う
		Assert.IsFalse(call.Message(TestEvent.Event1, "test"));
		Assert.AreNotEqual(holder.value, "test");
		//イベントを発火
		Assert.IsTrue(call.Message(TestEvent.Event2, "event2"));
		Assert.AreEqual(holder.value, "event2");

		//再びバインドするため二回イベント呼ばれる
		call.Bind(holder);
		holder.value = "";
		Assert.IsTrue(call.Broadcast(TestEvent.Event3, 123));
		Assert.AreEqual(holder.value, "123123");

		//一つ目のバインドを解除するので一回だけ呼ばれる
		handle1.Dispose();
		holder.value = "";
		Assert.IsTrue(call.Broadcast(TestEvent.Event3, 123));
		Assert.AreEqual(holder.value, "123");

	}

}