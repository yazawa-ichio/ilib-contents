
namespace ILib
{
	using Caller;


	public static class DispatcherExtensions
	{
		public static bool Message(this IHasDispatcher self, object key) => self.Dispatcher.Message(key);
		public static bool Message<T>(this IHasDispatcher self, object key, T prm) => self.Dispatcher.Message(key, prm);
		public static bool Broadcast(this IHasDispatcher self, object key) => self.Dispatcher.Broadcast(key);
		public static bool Broadcast<T>(this IHasDispatcher self, object key, T prm) => self.Dispatcher.Broadcast(key, prm);
	}

}
