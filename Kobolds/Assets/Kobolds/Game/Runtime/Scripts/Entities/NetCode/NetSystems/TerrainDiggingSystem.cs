using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Collections;
using Unity.Burst;
using UnityEngine; // For Debug.Log
using Kobolds.Utilities;
using Kobolds.Rpc;
using System.Collections.Generic;
using Kobolds.NetCode; // Required for List from ChunkCoordinator

namespace Kobolds.NetSystems
{
    /// <summary>
    /// Server system responsible for handling terrain modification (digging/placing).
    /// Processes DigTerrainRPCs and updates chunk density data on the server.
    /// </summary>
    // Correct attribute for server-only execution in this NetCode version
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct TerrainDiggingSystem : ISystem
    {
        // Query for incoming DigTerrainRPCs
        private EntityQuery _mDigTerrainRpcQuery;
        // BufferLookup for DensityByte buffers on server-side TerrainChunk entities
        private BufferLookup<DensityByte> _mDensityBufferLookup;
        // Map to find server-side TerrainChunk entities by their chunk coordinate
        // TBD: This map needs to be populated by the TerrainChunkLoaderSystem and shared.
        private NativeHashMap<int3, Entity> _mChunkEntityMap; // Currently an unpopulated local map

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Create query for incoming DigTerrainRPCs
            _mDigTerrainRpcQuery = state.GetEntityQuery(typeof(DigTerrainRPC), typeof(ReceiveRpcCommandRequest));

            // Get buffer lookup for DensityByte (writable on server)
            _mDensityBufferLookup = state.GetBufferLookup<DensityByte>();

            // TBD: Get the shared m_ChunkEntityMap from TerrainChunkLoaderSystem or a shared resource.
            // For now, initializing an empty one, which means lookups will fail.
            _mChunkEntityMap = new NativeHashMap<int3, Entity>(1, Allocator.Persistent); // Initialize with minimal capacity as it won't be populated here

            // Require DensityByte buffer to be present for modification
            state.RequireForUpdate<DensityByte>();

            // Require DigTerrainRPC to be present to update
            state.RequireForUpdate<DigTerrainRPC>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Ensure the chunk entity map is created before using it
            if (!_mChunkEntityMap.IsCreated)
            {
                 Debug.LogError("[TerrainDiggingSystem] Chunk entity map is not created!");
                 return;
            }

            // TBD: Access the shared m_ChunkEntityMap from TerrainChunkLoaderSystem
            // Currently using the local empty map initialized in OnCreate, which is incorrect.
            // For proper functionality, this system needs a reference to the map managed by TerrainChunkLoaderSystem.

            // Process incoming DigTerrainRPCs
            using (var digRpcEntities = _mDigTerrainRpcQuery.ToEntityArray(Allocator.TempJob))
            {
                foreach (Entity rpcEntity in digRpcEntities)
                {
                    // Get the RPC data
                    DigTerrainRPC rpc = state.EntityManager.GetComponentData<DigTerrainRPC>(rpcEntity);
                    // Get the sender connection entity (for potential future use)
                    ReceiveRpcCommandRequest req = state.EntityManager.GetComponentData<ReceiveRpcCommandRequest>(rpcEntity);

                    Debug.Log($"<color=yellow>[TerrainDiggingSystem] Received DigTerrainRPC at world pos {rpc.WorldPosition} with density {rpc.NewDensityValue}</color>");

                    // Find all chunk coordinates that own this world position (for boundary updates)
                    List<int3> owningChunks = ChunkCoordinator.GetOwningChunks(rpc.WorldPosition);

                    // Iterate over each owning chunk
                    foreach (int3 owningChunkCoord in owningChunks)
                    {
                        // Try to find the server-side TerrainChunk entity for this coordinate using the shared map
                        // Note: Currently using an unpopulated local map, so this lookup will likely fail.
                        if (_mChunkEntityMap.TryGetValue(owningChunkCoord, out Entity chunkEntity))
                        {
                            // Ensure the entity is valid and has a density buffer
                            if (state.EntityManager.Exists(chunkEntity) && state.EntityManager.HasBuffer<DensityByte>(chunkEntity))
                            {
                                // Get the density buffer (writable)
                                var densityBuffer = state.EntityManager.GetBuffer<DensityByte>(chunkEntity);

                                // Calculate the local index within the chunk's density buffer
                                int localIndex = ChunkCoordinator.WorldToChunkLocalIndex(owningChunkCoord, rpc.WorldPosition);

                                // Validate the local index (should be within 0-511 for 8x8x8 chunks)
                                if (localIndex >= 0 && localIndex < ChunkCoordinator.PointsPerChunk)
                                {
                                    // Update the density value at the local index
                                    byte oldDensity = densityBuffer[localIndex].Value;
                                    densityBuffer[localIndex] = new DensityByte { Value = rpc.NewDensityValue };

                                    Debug.Log($"<color=yellow>[TerrainDiggingSystem] Updated density in chunk {owningChunkCoord} at local index {localIndex} from {oldDensity} to {rpc.NewDensityValue}</color>");

                                    // Mark the DynamicBuffer as dirty to ensure it's replicated to clients
                                    // This is crucial for NetCode to know the buffer needs to be sent.
                                    state.EntityManager.SetComponentData(chunkEntity, new NeedsMeshGenerationTag()); // Add a dirty tag (Need to create this tag)
                                    // Or, depending on NetCode version, simply modifying the buffer might be enough if it's a Ghost component.
                                    // If it's a GhostField of type DynamicBuffer<T>, modifying it should mark the ghost as dirty.
                                    // Let's assume modifying is enough for now, but add a TODO for a potential dirty tag if needed.
                                    // TODO: Verify if modifying DensityByte buffer automatically marks the Ghost as dirty for replication.
                                    // If not, a separate Dirty tag/component is needed for the chunk entity.

                                }
                                else
                                {
                                    Debug.LogError($"[TerrainDiggingSystem] Invalid local index {localIndex} calculated for world pos {rpc.WorldPosition} in chunk {owningChunkCoord}. Skipping density update.");
                                }
                            }
                             else
                            {
                                Debug.LogWarning($"[TerrainDiggingSystem] Chunk entity found in map ({owningChunkCoord}), but entity does not exist or missing DensityBuffer. Index: {chunkEntity.Index}");
                            }
                        }
                        else
                        {
                            // This can happen if a player tries to dig in a chunk that hasn't been loaded on the server yet.
                            // Since we're using a local unpopulated map, this will always happen.
                            Debug.LogWarning($"<color=red>[TerrainDiggingSystem] Received DigTerrainRPC for chunk {owningChunkCoord} that is not in the server's entity map. Skipping.</color>");
                            // TODO: When map sharing is implemented, this indicates a legitimate case of digging in an unloaded chunk.
                            // The server should potentially trigger loading for this chunk.
                        }
                    }

                    // Destroy the processed RPC entity
                    state.EntityManager.DestroyEntity(rpcEntity);
                }
            }
            // Important: Disposal of digRpcEntities list occurs automatically
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            // Dispose of the NativeHashMap when the system is destroyed
            // Note: If the map is shared, disposal should be handled by the managing system.
            // Since this is a local map for now, dispose it here.
            if (_mChunkEntityMap.IsCreated)
            {
                _mChunkEntityMap.Dispose();
            }
        }

        // TBD: Implement sharing of the m_ChunkEntityMap with TerrainChunkLoaderSystem.
        // This could involve: Singleton map component, SystemAPI.GetSingletonRW<SharedChunkMap>,
        // or passing the map reference during system creation/initialization.
        // For now, the local map will not function correctly for lookups.
    }
} 
