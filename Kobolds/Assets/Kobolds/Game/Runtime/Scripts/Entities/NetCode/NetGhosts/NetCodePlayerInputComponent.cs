using Unity.Mathematics;
using Unity.NetCode;

namespace Kobolds.NetComponents
{
	public struct NetCodePlayerInputComponent : IInputComponentData
	{
		public float2 InputVector;
	}
}