using Unity.Entities;

namespace Kobolds.NetCode
{
	/// <summary>
	///     Singleton component for global terrain configuration.
	/// </summary>
	public struct TerrainSettings : IComponentData
	{
		// The global seed for deterministic terrain generation across all chunks
		public int GlobalSeed;

		// The size of each chunk along one axis (e.g., 8 for 8x8x8 chunks)
		public int ChunkSize;

		// The chunk load radius around each player (in chunks). Can be modified at runtime.
		public int ChunkLoadRadius;

		// The density threshold that separates solid (>= Threshold) from air (< Threshold)
		public byte SolidThreshold;
	}
}
