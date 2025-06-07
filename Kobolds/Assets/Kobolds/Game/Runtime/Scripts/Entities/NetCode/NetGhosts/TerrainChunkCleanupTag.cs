using Unity.Entities;

namespace Kobolds.NetCode
{
	/// <summary>
	///     Tag component added to a client-side TerrainChunk entity when it's about to be destroyed
	///     and needs its GameObject renderer to be cleaned up.
	/// </summary>
	public struct TerrainChunkCleanupTag : IComponentData
	{
	}
}
