# [ilib-serv-injector](https://github.com/yazawa-ichio/ilib-contents)

Unity Contents Package.

リポジトリ https://github.com/yazawa-ichio/ilib-contents

## 概要

プロジェクト全体のコンテンツを管理するツリーベースのステートマシーンです。  
コンテンツ間の追加・終了のフック等を追加出来るため、Unityのシーン管理に紐づけたり、DIコンテナと連動させたり出来ます。

# 使用方法


## コンテンツを作成する
コンテンツは`Content`クラスを継承して実装します。  
`Content`クラスには、起動時のBoot処理やコンテンツが有効になった際のイベント等のフックを追加できます。  
`Content<T>`を継承する事で明示的にパラメータを起動時に渡すことが出来ます。  

```csharp
using ILib.Contents;
using ILib.Caller;

public class GameManager : Content
{
	protected override async Task OnBoot()
	{
		// 共通で利用するシーンを読み込む
		await SceneManager.LoadSceneAsync("Common").ToTask();
	}

	protected override async Task OnShutdown()
	{
		// 共通で利用するシーンを破棄する
		await SceneManager.UnloadSceneAsync("Common").ToTask();
	}

	protected override void OnCompleteRun()
	{
		//子供に起動シーンを追加
		Append<BootScene>(null);
	}

	//再起動イベント
	[Handle(SystemEvent.Reboot)]
	void OnReboot()
	{
		// 現在のGameManagerのShutdown後に新しいGameManagerのBootが始まる
		Switch<GameManager>(Param);
	}
}
```

## コンテンツの起動
コンテンツは`ContentsController`コンポーネントで制御されます。  
最初に実行するコンテンツを設定して`Boot`関数を実行します。  
起動するコンテンツが一つしかない場合は、`ContentsController.Boot<GameManager>(null)`のようにも指定できます。  

```csharp
using UnityEngine;
using ILib.Contents;
//起動スクリプト
public class Main : MonoBehaviour
{
	void Start()
	{
		var obj = new GameObject(typeof(ContentsController).Name);
		GameObject.DontDestroyOnLoad(obj);
		var controller = obj.AddComponent<ContentsController>();
		//起動時に実行するコンテンツを選択
		BootParam param = new BootParam();
		// セーブデータの管理などゲームサイクルに関わらないコンテンツ
		param.Add<SystemManager>();
		// ゲームのメインのサイクルを管理するコンテンツ
		param.Add<GameManager>();
		//並列起動しない
		param.ParallelBoot = false;
		controller.Boot(param);
	}
}
```

## コンテンツの操作
コンテンツのツリーの操作一覧です。

### 追加 `Append<T>`
自分の子供にコンテンツを追加します。

### 終了 `Shutdown<T>`
自身を終了します。  
子のコンテンツがある場合、それらも終了します。  
終了時、親のリストから自身が削除されます。  

### 変更 `Switch<T>`
自身を終了し、実行者の親の子供を追加します（=Paraent.Append）

### 停止 `Suspend`
コンテンツを停止します。  
子のコンテンツがある場合、それらも停止します。  
停止時は`Disable`イベントが実行されます。  
実行したコンテンツだけ`OnSuspend`が呼ばれます。  

### 再開 `Resume`
停止状態のコンテンツを再開します。  
子のコンテンツがある場合、それらも再開します。  
再開時は`Enable`イベントが実行されます。  
実行したコンテンツだけ`OnRun`と`OnCompleteRun`が呼ばれます。  

## 共通処理を挟む
コンテンツの共通処理は`Module`クラスと、interfaceや共通の基底クラスを作る事で対応できます。  
`Module`は`ContentsController`か`Content`に追加できます。  `Module`の有効範囲は、追加したインスタンスの子供にのみ有効です。

```csharp
using UnityEngine.SceneManagement;
using ILib.Contents;
//別に公開しているilib-servinjectパッケージの実装です。
using ILib.ServInject;

// ゲームのシーンの共通実装
public abstract class GameScene : Content, IUnitySceneContent
{
	//DIコンテナとの連携
	[Inject]
	public IResourceLoader Loader { get; set; }
	[Inject]
	public ISystemUI SystemUI { get; set; }
	[Inject]
	public ISound Sound { get; set; }

	public virtual string GetUnitySceneName() => "";
}

//コンテンツに紐づけてUnityのシーンを読むためのクラス
public interface IUnitySceneContent {
	string GetUnitySceneName();
}

//Unityのシーンに紐づけるモジュール
public class UnitySceneModule : Module
{
	//イベントを実行するタイミングはTypeで指定する
	public override ModuleType Type => ModuleType.PreBoot | ModuleType.Shutdown;
	public override async Task OnPreBoot (Content content)
	{
		if (content is IUnitySceneContent scene)
		{
			string name = scene.GetUnitySceneName();
			if (string.IsNullOrEmpty(name)) return;
			await SceneManager.LoadSceneAsync(name).ToTask();
		}
	}

	public override async Task OnShutdown(Content content)
	{
		if (content is IUnitySceneContent scene)
		{
			string name = scene.GetUnitySceneName();
			if (string.IsNullOrEmpty(name)) return;
			await SceneManager.UnloadSceneAsync(name).ToTask();
		}
	}
}

//DIコンテナでサービスを注入するモジュール
public class ServiceInstallModule : Module
{
	public override ModuleType Type => ModuleType.PreBoot;

	public override Task OnPreBoot(Content content)
	{
		//OnBoot前に登録済みサービスが注入される
		ServInjector.Inject(content);
		return Util.Successed;
	}
}
```

### Moduleを追加する


```csharp
using ILib.Contents;
using ILib.Caller;

public class GameManager : Content
{
	protected override async Task OnBoot()
	{
		Modules.Add<UnitySceneModule>();
		Modules.Add<ServiceInstallModule>();
~~~
~~~
	}
}
```

## モーダル機能
モーダル機能は特定のコンテンツを実行しつつ、その結果を待ちたい場合に利用できます。  
モーダル実行中は、モーダルを実行したコンテンツに対して`Switch`(変更)と`Append`(追加)が出来なくなります。  
モーダルを実行するには、`IModalContent<T>`を実装する必要があります。  

モーダルコンテンツは単純なコンテンツ機能の拡張であるため、コンテンツが持つ共通処理やモジュールのフックが利用出来るのが利点です。

```csharp
using ILib.Contents;

// この例はSystemUI.InputDialogを直接呼べばいいので少し不適切だが
// より複雑なケースでコンテンツ間のモジュール機能などが利用できるため便利な場合がある
public class InputScene : GameScene<InputDialogParam>, IModalContent<string> {
	public async Task<string> GetModalResult(CancellationToken token)
	{
		return await SystemUI.InputDialog(Param)
	}
}

public class TitleScene : GameScene
{
	public override async Task OnRun()
	{
		if (string.IsEmpty(Save.UserName))
		{
			var prm = new InputDialogParam("ユーザー名")
			Save.UserName = await Modal<string, InputS
			cene>(prm);
		}
	}
}
```

## コンテンツ間のイベント
コンテンツ間で処理を行いたい場合、`EventCall`クラスを利用します。  
イベントの登録は`EventCall`の`Subscribe`関数を利用するか。 
イベントの発火先のクラスのメソッドに`Handler`属性を設定します。  
本来、`EventCall.Bind`を実行する必要がありますが、コンテンツは生成時に登録されます。  
`Suspend`で停止中はイベントを受けつけなくなります。  

```csharp
using ILib.Contents;
using ILib.Caller;
//イベントの発火元
public class SelectScene : GameScene
{
	public event Event
	{
		OK,
		NO,
		Param,
	}
	public override async Task OnRun()
	{
		var ret = await SystemUI.Conform("選択");
		if (ret)
		{
			EventCall.Broadcast(Event.OK);
		}
		else
		{
			EventCall.Broadcast(Event.NO);
		}
	}
}
//イベントの受け取り先
public class HogeScene : GameScene
{
	public void OnCompleteRun()
	{
		Append<SelectScene>(null);
		//手動で設定。
		Managed.Manage(EventCall.Subscribe(SelectScene.Event.NO, () => {});
		//例として上記のコードを書きましたが、コンテンツでは基本的にHandler経由で設定することを推奨しています。
	}
	//属性での指定
	[Handler(SelectScene.Event.OK)]
	void OnOK() { }
	//引数も受け取れる
	[Handler(SelectScene.Event.Param)]
	void OnParam(string prm) { }
}
```

## コンテンツのリソースを管理する
コンテンツには、コンテンツ内でのみ有効なオブジェクト等を管理する`ManagedHolder`機能があります。  
コンテンツが持つ`Managed`インスタンスに`IDisposable`を実装したクラスを追加すると、コンテンツが削除された際に自動で`Dispose`が実行されます。  
また、非同期で解放処理を行いたい場合は`IManagedAsyncDisposable`インタフェースを実装する事で、コンテンツの終了時に非同期で実行されます。  

```csharp
using ILib.Contents;
public class TitleScene : GameScene
{
	public override async Task OnRun()
	{
		var asset = Loader.Load("Asset")
		//このコンテンツの終了時にDisposeが実行される
		Managed.Manage(asset);
		//事前に解放する事も出来る
		Managed.Unmanage(asset);

		//UnityのComponentを追加して、コンテンツの破棄に合わせてDestoryも出来る
		Managed.ManageComponent(Controller.AddComponent<Hoge>());

	}
}
```

## Unityのイベントサイクルに合わせる
コンテンツからは`Routine`関数を利用すると、エラーのハンドリングが可能なコルーチンを実行できます。  
`Controller`でも実行できますが、`Routine`関数を利用する事でコンテンツの終了時に自動コルーチンが停止します。  

```csharp
using ILib.Contents;
public class GameLoopScene : GameScene
{
	Routine m_Loop;
	protected override Task OnEnable()
	{
		m_Loop = Routine(Loop());
		return Util.Successed;
	}
	protected override Task OnDisable()
	{
		m_Loop.Cancel();
		return Util.Successed;
	}
	IEnumerator Loop()
	{
		while (true)
		{
			//Update
			yield return null;
		}
	}
}
```

## [コンテンツのフック一覧(WIP)](https://yazawa-ichio.github.io/ilib-unity-project/api/ILib.Contents.Content.html)

後々まとめます。

## [モジュールのフック一覧(WIP)](https://yazawa-ichio.github.io/ilib-unity-project/api/ILib.Contents.Module.html)

後々まとめます。

## LICENSE

https://github.com/yazawa-ichio/ilib-contents/blob/master/LICENSE