using System;

namespace ILib.Contents
{
	[Flags]
	public enum ModuleType
	{
		None = 0,
		/// <summary>
		/// コンテンツの初期化直前のイベントです。
		/// コンテンツ毎に一度しか実行されません。
		/// </summary>
		PreBoot = 1,
		/// <summary>
		/// コンテンツの初期化直後のイベントです。
		/// コンテンツ毎に一度しか実行されません。
		/// </summary>
		Boot = 2,
		/// <summary>
		/// コンテンツの終了直前のイベントです。
		/// コンテンツ毎に一度しか実行されません。
		/// </summary>
		PreShutdown = 3,
		/// <summary>
		/// コンテンツの終了直後のイベントです。
		/// コンテンツ毎に一度しか実行されません。
		/// </summary>
		Shutdown = 4,
		/// <summary>
		/// コンテンツの実行直前のイベントです。
		/// サスペンドから復帰する際にも呼ばれます。
		/// </summary>
		PreRun = 5,
		/// <summary>
		/// コンテンツの実行時のイベントです。
		/// サスペンドから復帰する際にも呼ばれます。
		/// </summary>
		Run = 6,
		/// <summary>
		/// コンテンツの停止直前のイベントです。
		/// </summary>
		PreSuspend = 7,
		/// <summary>
		/// コンテンツの停止時のイベントです。
		/// </summary>
		Suspend = 8,
		/// <summary>
		/// コンテンツの有効化直前のイベントです。
		/// 親のコンテンツが有効になった際も実行されます。
		/// </summary>
		PreEnable = 9,
		/// <summary>
		/// コンテンツの有効化直後のイベントです。
		/// 親のコンテンツが有効になった際も実行されます。
		/// </summary>
		Enable = 10,
		/// <summary>
		/// コンテンツの無効化直前のイベントです。
		/// 親のコンテンツが無効になった際も実行されます。
		/// </summary>
		PreDisable = 11,
		/// <summary>
		/// コンテンツの無効化直後のイベントです。
		/// 親のコンテンツが無効になった際も実行されます。
		/// </summary>
		Disable = 12,
		/// <summary>
		/// コンテンツを遷移直前のイベントです。
		/// </summary>
		PreSwitch = 13,
		/// <summary>
		/// コンテンツを遷移イベントです。
		/// </summary>
		Switch = 14,
		/// <summary>
		/// コンテンツを遷移完了イベントです。
		/// </summary>
		EndSwitch = 15,
	}

}
