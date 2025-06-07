using Unity.Entities;

namespace Kobolds.NetCode
{
	/// <summary>
	///     Tag component added to a client-side TerrainChunk entity when its density data is ready for mesh generation.
	/// </summary>
	public struct NeedsMeshGenerationTag : IComponentData
	{
	}
}
