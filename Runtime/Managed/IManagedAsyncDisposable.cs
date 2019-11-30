using System.Threading.Tasks;

namespace ILib.Managed
{
	public interface IManagedAsyncDisposable
	{
		Task DisposeAsync();
	}
}
