using Unity.NetCode;
using UnityEngine.Scripting;

namespace Kobolds.NetCode
{
	/// <summary>
	///     Bootstraps the client & server connection for quick testing
	/// </summary>
	[Preserve]
	public class NetCodeGameBootstrap : ClientServerBootstrap
	{
		public override bool Initialize(string defaultWorldName)
		{
			AutoConnectPort = 7979;
			return base.Initialize(defaultWorldName);
		}
	}
}
