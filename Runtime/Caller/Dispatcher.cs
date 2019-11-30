
namespace ILib.Caller
{

	public interface IDispatcher
	{
		bool Message(object key);
		bool Message<T>(object key, T prm);
		bool Broadcast(object key);
		bool Broadcast<T>(object key, T prm);
	}

	public class Dispatcher : IDispatcher
	{
		EventCall m_Call;
		public Dispatcher(EventCall call) => m_Call = call;
		public bool Broadcast(object key) => m_Call.Broadcast(key);
		public bool Broadcast<T>(object key, T prm) => m_Call.Broadcast(key, prm);
		public bool Message(object key) => m_Call.Message(key);
		public bool Message<T>(object key, T prm) => m_Call.Message(key, prm);
	}

	public interface IHasDispatcher
	{
		IDispatcher Dispatcher { get; }
	}

}
