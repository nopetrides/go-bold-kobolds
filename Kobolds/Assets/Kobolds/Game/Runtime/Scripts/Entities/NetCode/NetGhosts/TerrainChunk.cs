using Unity.Entities;
using Unity.Mathematics;

namespace Kobolds.NetCode
{
    /// <summary>
    /// ECS component for terrain chunk entities. Stores chunk coordinate and metadata.
    /// </summary>
    public struct TerrainChunk : IComponentData
    {
        // The coordinate of this chunk in chunk space
        public int3 ChunkCoord;
        // Optional: deterministic seed for this chunk (can be derived from world seed + coord)
        public int ChunkSeed;
    }

    /// <summary>
    /// DynamicBuffer element for storing chunk density data (0-255 per voxel).
    /// </summary>
    public struct DensityByte : IBufferElementData
    {
        // Density value for a single voxel (0-255)
        public byte Value;
    }
} 