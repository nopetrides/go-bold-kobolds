using Unity.NetCode;

namespace Kobolds.Rpc
{
	/// <summary>
	///     Very simple Rpc for testing commands
	/// </summary>
	public struct SimpleRpc : IRpcCommand
	{
		public int Value;
	}
}
