using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;
using Unity.Collections;

namespace Kobolds.Rpc
{
    /// <summary>
    /// RPC from Client to Server to request terrain chunk data.
    /// </summary>
    [GhostComponent]
    public struct RequestChunkRPC : IRpcCommand
    {
        // The coordinate of the chunk being requested.
        public int3 ChunkCoordinate;
    }

    /// <summary>
    /// RPC from Server to Client to send terrain chunk data.
    /// Sends seed for deterministic generation on client.
    /// </summary>
    [GhostComponent]
    public struct ChunkDataRPC : IRpcCommand
    {
        // The coordinate of the chunk whose data is being sent.
        public int3 ChunkCoordinate;
        // The global seed used for deterministic generation of this chunk.
        public int GlobalSeed;
        // Note: Client will use the globalSeed and chunkCoordinate to regenerate density data locally.
        // For non-deterministic data, a NativeArray<byte> would be included here.
    }

    /// <summary>
    /// RPC from Client to Server to notify of terrain modification (digging/placing).
    /// </summary>
    [GhostComponent]
    public struct DigTerrainRPC : IRpcCommand
    {
        // The world position where the modification occurred.
        public float3 WorldPosition;
        // The new density value at the world position.
        public byte NewDensityValue;
    }

    /// <summary>
    /// RPC from Server to Client to instruct client to unload a specific chunk.
    /// </summary>
    [GhostComponent]
    public struct ChunkUnloadRPC : IRpcCommand
    {
        // The coordinate of the chunk to be unloaded.
        public int3 ChunkCoordinate;
    }
} 