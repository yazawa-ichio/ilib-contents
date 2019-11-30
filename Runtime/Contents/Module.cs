using System.Collections;
using System.Threading.Tasks;

namespace ILib.Contents
{

	/// <summary>
	/// アプリケーションの共通で行いたい処理を実装します。
	/// Typeで指定した関数しか実行されません。
	/// </summary>
	public abstract class Module
	{

		/// <summary>
		/// Typeに登録したイベントのみ実行されます。
		/// </summary>
		public abstract ModuleType Type { get; }

		/// <summary>
		/// コンテンツの初期化直前のイベントです。
		/// コンテンツ毎に一度しか実行されません。
		/// </summary>
		public virtual Task OnPreBoot(Content content) => Util.Successed;
		/// <summary>
		/// コンテンツの初期化直後のイベントです。
		/// コンテンツ毎に一度しか実行されません。
		/// </summary>
		public virtual Task OnBoot(Content content) => Util.Successed;
		/// <summary>
		/// コンテンツの終了直前のイベントです。
		/// コンテンツ毎に一度しか実行されません。
		/// </summary>
		public virtual Task OnPreShutdown(Content content) => Util.Successed;
		/// <summary>
		/// コンテンツの終了直後のイベントです。
		/// コンテンツ毎に一度しか実行されません。
		/// </summary>
		public virtual Task OnShutdown(Content content) => Util.Successed;
		/// <summary>
		/// コンテンツの実行直前のイベントです。
		/// サスペンドから復帰する際にも呼ばれます。
		/// </summary>
		public virtual Task OnPreRun(Content content) => Util.Successed;
		/// <summary>
		/// コンテンツの実行時のイベントです。
		/// サスペンドから復帰する際にも呼ばれます。
		/// </summary>
		public virtual Task OnRun(Content content) => Util.Successed;
		/// <summary>
		/// コンテンツの停止直前のイベントです。
		/// </summary>
		public virtual Task OnPreSuspend(Content content) => Util.Successed;
		/// <summary>
		/// コンテンツの停止時のイベントです。
		/// </summary>
		public virtual Task OnSuspend(Content content) => Util.Successed;
		/// <summary>
		/// コンテンツの有効化直前のイベントです。
		/// 親のコンテンツが有効になった際も実行されます。
		/// </summary>
		public virtual Task OnPreEnable(Content content) => Util.Successed;
		/// <summary>
		/// コンテンツの有効化直後のイベントです。
		/// 親のコンテンツが有効になった際も実行されます。
		/// </summary>
		public virtual Task OnEnable(Content content) => Util.Successed;
		/// <summary>
		/// コンテンツの無効化直前のイベントです。
		/// 親のコンテンツが無効になった際も実行されます。
		/// </summary>
		public virtual Task OnPreDisable(Content content) => Util.Successed;
		/// <summary>
		/// コンテンツの無効化直後のイベントです。
		/// 親のコンテンツが無効になった際も実行されます。
		/// </summary>
		public virtual Task OnDisable(Content content) => Util.Successed;
		/// <summary>
		/// コンテンツを遷移直前のイベントです。
		/// </summary>
		public virtual Task OnPreSwitch(Content prev, Content next) => Util.Successed;
		/// <summary>
		/// コンテンツを遷移イベントです。
		/// </summary>
		public virtual Task OnSwitch(Content prev, Content next) => Util.Successed;
		/// <summary>
		/// コンテンツを遷移完了イベントです。
		/// </summary>
		public virtual Task OnEndSwitch(Content prev, Content next) => Util.Successed;

	}

}
