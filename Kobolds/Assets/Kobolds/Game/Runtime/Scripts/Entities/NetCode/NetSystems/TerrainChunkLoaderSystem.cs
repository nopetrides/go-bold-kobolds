using Kobolds.NetCode;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;
using Unity.Collections;
using Unity.Burst;
using Kobolds.Utilities;
using Kobolds.Rpc;
using UnityEngine; // For Debug.Log

namespace Kobolds.NetSystems
{
    /// <summary>
    /// Server system responsible for loading and unloading terrain chunks around players.
    /// </summary>
    // Correct attribute for server-only execution in this NetCode version
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct TerrainChunkLoaderSystem : ISystem
    {
        // ComponentLookup for accessing TerrainSettings singleton
        private ComponentLookup<TerrainSettings> _terrainSettingsLookup;
        // Map to find server-side TerrainChunk entities by their chunk coordinate
        // This map is populated when chunks are loaded on the server.
        private NativeHashMap<int3, Entity> _chunkEntityMap;
        // BufferLookup for DensityByte buffers (read-only for RequestChunkRPC handling)
        private BufferLookup<DensityByte> _densityBufferLookup; // Added for RequestChunkRPC handling

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Initialize the chunk entity map
            _chunkEntityMap = new NativeHashMap<int3, Entity>(1024, Allocator.Persistent);

            // Require PlayerChunkLoader to exist in the world before running
            state.RequireForUpdate<PlayerChunkLoader>();
            // Get lookup for TerrainSettings (singleton) (read-only)
            _terrainSettingsLookup = state.GetComponentLookup<TerrainSettings>(true);
             // Get buffer lookup for DensityByte (read-only for sending data)
            _densityBufferLookup = state.GetBufferLookup<DensityByte>(true); // Initialized here

             // Query for incoming RequestChunkRPCs
            state.RequireForUpdate<RequestChunkRPC>(); // Added for RequestChunkRPC handling
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Access the singleton terrain settings
            if (!SystemAPI.HasSingleton<TerrainSettings>())
            {
                // Terrain settings not available yet, skip update
                Debug.LogWarning("[TerrainChunkLoaderSystem] TerrainSettings singleton not found.");
                return;
            }

            TerrainSettings terrainSettings = SystemAPI.GetSingleton<TerrainSettings>();
            int chunkSize = terrainSettings.ChunkSize;
            int loadRadius = terrainSettings.ChunkLoadRadius;
            int globalSeed = terrainSettings.GlobalSeed;

            // Use an EntityCommandBuffer for structural changes in jobs
            var ecbSystem = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged);

            // --- Initialize PlayerChunkLoader for new players --- (Existing logic)
            // Add PlayerChunkLoader to new player entities that connect and are in game
            var newPlayerQuery = SystemAPI.QueryBuilder()
                .WithNone<PlayerChunkLoader>() // Target entities without PlayerChunkLoader
                .WithAll<NetworkId, NetworkStreamInGame>() // Ensure it's a connected player entity that is in game
                .Build();

            var newPlayerEntities = newPlayerQuery.ToEntityArray(Allocator.Temp);
            var newPlayerNetworkIds = newPlayerQuery.ToComponentDataArray<NetworkId>(Allocator.Temp);

            for (int i = 0; i < newPlayerEntities.Length; i++)
            {
                Entity playerEntity = newPlayerEntities[i];
                NetworkId networkId = newPlayerNetworkIds[i];

                // Create and initialize the PlayerChunkLoader component
                // Use Allocator.Persistent for NativeHashSets that live with the entity
                var newPlayerChunkLoader = new PlayerChunkLoader
                {
                    LoadedChunks = new NativeHashSet<int3>(0, Allocator.Persistent),
                    RequestedChunks = new NativeHashSet<int3>(0, Allocator.Persistent),
                    // Initialize positions to MaxValue to ensure the initial position check triggers loading
                    CurrentChunkPosition = int3.zero,
                    LastChunkPosition = int3.zero
                };
                // Use ECB to add the component
                ecb.AddComponent(playerEntity, newPlayerChunkLoader);
                Debug.Log($"[TerrainChunkLoaderSystem] Initialized PlayerChunkLoader for new player {networkId.Value}");
            }

            newPlayerEntities.Dispose();
            newPlayerNetworkIds.Dispose();


            // --- Process players who moved chunk --- (Existing logic with map updates)
            // Iterate over all player entities that have moved or just connected
            var playerQuery = SystemAPI.QueryBuilder()
                .WithAll<LocalTransform, PlayerChunkLoader, NetworkId, GhostOwner>()
                .Build();

            var playerEntities = playerQuery.ToEntityArray(Allocator.Temp);
            var playerTransforms = playerQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            var playerNetworkIds = playerQuery.ToComponentDataArray<NetworkId>(Allocator.Temp);
            var playerGhostOwners = playerQuery.ToComponentDataArray<GhostOwner>(Allocator.Temp);

            for (int playerIndex = 0; playerIndex < playerEntities.Length; playerIndex++)
            {
                Entity playerEntity = playerEntities[playerIndex];
                LocalTransform localTransform = playerTransforms[playerIndex];
                NetworkId networkId = playerNetworkIds[playerIndex];
                GhostOwner ghostOwner = playerGhostOwners[playerIndex];

                // Get the PlayerChunkLoader component (need to use RefRW since we'll modify it)
                var playerChunkLoader = SystemAPI.GetComponentRW<PlayerChunkLoader>(playerEntity);

                // Calculate the player's current chunk position
                int3 currentChunkPos = ChunkCoordinator.WorldToChunkCoord(localTransform.Position);

                // Check if the player has moved to a new chunk or is a newly initialized player loader
                // Initialized PlayerChunkLoader has lastChunkPosition = int3.MaxValue, ensuring this is true initially.
                if (!currentChunkPos.Equals(playerChunkLoader.ValueRO.LastChunkPosition))
                {
                    Debug.Log($"[TerrainChunkLoaderSystem] Player {networkId.Value} moved from {playerChunkLoader.ValueRO.LastChunkPosition} to {currentChunkPos}");

                    // Calculate the set of chunks that should be loaded around the player
                    // Estimate capacity based on load radius, allocator.Temp is for the temporary set
                    NativeHashSet<int3> neededChunks = new NativeHashSet<int3>(terrainSettings.ChunkLoadRadius * terrainSettings.ChunkLoadRadius * terrainSettings.ChunkLoadRadius * 8, Allocator.Temp);
                    for (int x = -loadRadius; x <= loadRadius; x++)
                    {
                        for (int y = -loadRadius; y <= loadRadius; y++)
                        {
                            for (int z = -loadRadius; z <= loadRadius; z++)
                            {
                                // Calculate the needed chunk coordinate
                                int3 neededChunkCoord = currentChunkPos + new int3(x, y, z);
                                // Add to the set of needed chunks
                                neededChunks.Add(neededChunkCoord);
                            }
                        }
                    }

                    // Determine chunks to unload (currently loaded but no longer needed)
                    using (var chunksToUnload = new NativeList<int3>(Allocator.Temp))
                    {
                        // Iterate over the player's currently loaded chunks
                        foreach (int3 loadedChunkCoord in playerChunkLoader.ValueRO.LoadedChunks)
                        {
                            // If a loaded chunk is not in the set of needed chunks, mark it for unloading
                            if (!neededChunks.Contains(loadedChunkCoord))
                            {
                                chunksToUnload.Add(loadedChunkCoord);
                                Debug.Log($"[TerrainChunkLoaderSystem] Player {networkId.Value}: Marking chunk {loadedChunkCoord} for unload.");
                            }
                        }

                        // Process chunks to unload
                        foreach (int3 unloadChunkCoord in chunksToUnload)
                        {
                            // Remove from the player's loaded chunks set
                            playerChunkLoader.ValueRW.LoadedChunks.Remove(unloadChunkCoord);
                            // TODO: Implement logic to destroy chunk entity if no other player needs it
                            // For now, just remove from the player's list and send RPC, but don't remove from m_ChunkEntityMap yet.

                            // Send ChunkUnloadRPC to the client using an EntityCommandBuffer
                            var rpcEntity = ecb.CreateEntity(); // Use ECB to create RPC entity in the job
                            ecb.AddComponent(rpcEntity, new ChunkUnloadRPC
                            {
                                ChunkCoordinate = unloadChunkCoord
                            });
                            // Tag the RPC to be sent to this specific player's connection
                            // Use GhostOwner.NetworkId to find the connection entity if needed, or directly playerEntity if it's the connection entity
                            // Assuming playerEntity *is* the connection entity for simplicity based on GoInGameServerSystem.
                            ecb.AddComponent(rpcEntity, new SendRpcCommandRequest
                            {
                                TargetConnection = playerEntity // Target the connection entity associated with the player
                            });
                             Debug.Log($"[TerrainChunkLoaderSystem] Player {networkId.Value}: Sent ChunkUnloadRPC for chunk {unloadChunkCoord}");
                        }
                    }

                    // Determine chunks to load (needed but not currently loaded or requested)
                    using (var chunksToLoad = new NativeList<int3>(Allocator.Temp))
                    {
                        // Iterate over the set of needed chunks
                        foreach (int3 neededChunkCoord in neededChunks)
                        {
                            // If a needed chunk is not loaded and not already requested by this player, mark it for loading
                            if (!playerChunkLoader.ValueRO.LoadedChunks.Contains(neededChunkCoord) && !playerChunkLoader.ValueRO.RequestedChunks.Contains(neededChunkCoord))
                            {
                                chunksToLoad.Add(neededChunkCoord);
                                playerChunkLoader.ValueRW.RequestedChunks.Add(neededChunkCoord); // Mark as requested immediately by this player
                                Debug.Log($"[TerrainChunkLoaderSystem] Player {networkId.Value}: Marking chunk {neededChunkCoord} as requested.");
                            }
                        }

                        // Process chunks to load/request
                        foreach (int3 loadChunkCoord in chunksToLoad)
                        {
                            Entity chunkEntity; // Declare variable outside condition
                            bool chunkExists = _chunkEntityMap.TryGetValue(loadChunkCoord, out chunkEntity); // Check if chunk entity already exists

                            if (!chunkExists)
                            {
                                // Chunk entity does not exist on the server yet, create it
                                Debug.Log($"[TerrainChunkLoaderSystem] Creating new chunk entity for {loadChunkCoord}...");
                                // Use ECB to create the entity in the job
                                chunkEntity = ecb.CreateEntity();
                                // Use ECB to add components and buffer to the new entity
                                ecb.AddComponent(chunkEntity, new TerrainChunk
                                {
                                    ChunkCoord = loadChunkCoord,
                                    ChunkSeed = globalSeed // Server holds the global seed
                                });
                                // Add the density buffer using ECB
                                var densityBuffer = ecb.AddBuffer<DensityByte>(chunkEntity);
                                // Resize and populate density buffer using FastNoiseLite
                                densityBuffer.ResizeUninitialized(ChunkCoordinator.PointsPerChunk);

                                // Calculate chunk-specific seed for noise
                                int chunkSeed = (int)(globalSeed + math.hash(loadChunkCoord)); // Consistent seed generation
                                float noiseFrequency = 0.05f; // Use same frequency as client

                                // Populate density buffer
                                for (int i = 0; i < ChunkCoordinator.PointsPerChunk; i++)
                                {
                                    int3 localPos = new int3(
                                       i % ChunkCoordinator.ChunkSize,
                                       (i / ChunkCoordinator.ChunkSize) % ChunkCoordinator.ChunkSize,
                                       i / (ChunkCoordinator.ChunkSize * ChunkCoordinator.ChunkSize)
                                   );
                                    int3 worldGridPos = loadChunkCoord * ChunkCoordinator.ChunkSize + localPos;

                                    float noiseValue = FastNoiseLite.GetNoise(new float3(worldGridPos), chunkSeed, noiseFrequency);
                                    byte density = (byte)math.clamp((noiseValue + 1.0f) * 127.5f, 0, 255);
                                    densityBuffer[i] = new DensityByte { Value = density };
                                }

                                // Add the newly created chunk entity to the map
                                _chunkEntityMap.Add(loadChunkCoord, chunkEntity);
                                Debug.Log($"[TerrainChunkLoaderSystem] Created and mapped chunk entity {chunkEntity.Index} for {loadChunkCoord}.");

                            } else {
                                // Chunk entity already exists on the server
                                Debug.Log($"[TerrainChunkLoaderSystem] Chunk entity for {loadChunkCoord} already exists ({chunkEntity.Index}).");
                                // Ensure it has a DensityBuffer if it existed but wasn't fully initialized (edge case)
                                if (!state.EntityManager.HasBuffer<DensityByte>(chunkEntity))
                                {
                                     Debug.LogError($"[TerrainChunkLoaderSystem] Existing chunk entity {chunkEntity.Index} for {loadChunkCoord} is missing DensityBuffer!");
                                     // Handle this error - potentially recreate or skip
                                }
                            }

                            // Add the chunk to this player's loaded set (regardless of whether it was newly created or already existed)
                            if (!playerChunkLoader.ValueRO.LoadedChunks.Contains(loadChunkCoord)) // Double check before adding
                            {
                                playerChunkLoader.ValueRW.LoadedChunks.Add(loadChunkCoord);
                                Debug.Log($"[TerrainChunkLoaderSystem] Player {networkId.Value}: Added chunk {loadChunkCoord} to loadedChunks.");
                            }
                            // Remove from requested set for this player (data is now considered available/loading)
                            playerChunkLoader.ValueRW.RequestedChunks.Remove(loadChunkCoord);
                             Debug.Log($"[TerrainChunkLoaderSystem] Player {networkId.Value}: Removed chunk {loadChunkCoord} from requestedChunks.");

                            // Send ChunkDataRPC to the client for the new/existing chunk
                            // This is done even for existing chunks to ensure the client gets the latest data if needed.
                            var rpcEntity = ecb.CreateEntity(); // Use ECB to create RPC entity in the job
                            ecb.AddComponent(rpcEntity, new ChunkDataRPC
                            {
                                ChunkCoordinate = loadChunkCoord,
                                GlobalSeed = globalSeed // Send the global seed for client-side regeneration
                            });
                             // Target the specific client connection entity
                             // Assuming playerEntity *is* the connection entity for simplicity.
                            ecb.AddComponent(rpcEntity, new SendRpcCommandRequest
                            {
                                TargetConnection = playerEntity // Target the connection entity
                            });
                            Debug.Log($"[TerrainChunkLoaderSystem] Player {networkId.Value}: Sent ChunkDataRPC for chunk {loadChunkCoord}");
                        }
                    }

                    // Update the player's last known chunk position
                    playerChunkLoader.ValueRW.LastChunkPosition = currentChunkPos;
                     Debug.Log($"[TerrainChunkLoaderSystem] Player {networkId.Value}: Updated lastChunkPosition to {currentChunkPos}.");
                }
            }

            playerEntities.Dispose();
            playerTransforms.Dispose();
            playerNetworkIds.Dispose();
            playerGhostOwners.Dispose();

            // --- Process incoming RequestChunkRPCs --- (New Logic)
            // Needs to be a separate job/system if running in parallel to avoid structural changes within the ForEach above.
            // Running as a separate job here for illustration.
            var chunkEntityMapJob = _chunkEntityMap; // Copy for job access
            var densityBufferLookupJob = _densityBufferLookup; // Copy for job access
            var globalSeedJob = globalSeed; // Copy for job access

            // Use a CommandBuffer to create RPC entities from within a job
            var rpcEcbSystem = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var rpcEcb = rpcEcbSystem.CreateCommandBuffer(state.WorldUnmanaged);

            // Query for RequestChunkRPC entities
            var requestChunkRpcQuery = SystemAPI.QueryBuilder()
                .WithAll<RequestChunkRPC, ReceiveRpcCommandRequest>()
                .Build();

            var requestChunkRpcEntities = requestChunkRpcQuery.ToEntityArray(Allocator.Temp);
            var requestChunkRpcs = requestChunkRpcQuery.ToComponentDataArray<RequestChunkRPC>(Allocator.Temp);
            var receiveRpcCommandRequests = requestChunkRpcQuery.ToComponentDataArray<ReceiveRpcCommandRequest>(Allocator.Temp);

            for (int i = 0; i < requestChunkRpcEntities.Length; i++)
            {
                Entity rpcEntity = requestChunkRpcEntities[i];
                RequestChunkRPC rpc = requestChunkRpcs[i];
                ReceiveRpcCommandRequest req = receiveRpcCommandRequests[i];

                // Get the connection entity that sent the RPC
                Entity requestingConnection = req.SourceConnection;

                Debug.Log($"<color=yellow>[TerrainChunkLoaderSystem] Received RequestChunkRPC for chunk {rpc.ChunkCoordinate} from connection {requestingConnection.Index}</color>");

                // Try to find the server-side TerrainChunk entity for the requested coordinate
                if (chunkEntityMapJob.TryGetValue(rpc.ChunkCoordinate, out Entity chunkEntity))
                {
                    // Check if the entity has a density buffer (avoid using EntityManager directly)
                    if (densityBufferLookupJob.HasBuffer(chunkEntity))
                    {
                        // Chunk exists and has data, send ChunkDataRPC back to the requesting client
                        var sendRpcEntity = rpcEcb.CreateEntity(); // Use ECB to create entity in a job
                        rpcEcb.AddComponent(sendRpcEntity, new ChunkDataRPC
                        {
                            ChunkCoordinate = rpc.ChunkCoordinate,
                            GlobalSeed = globalSeedJob // Send the global seed
                        });
                         // Target the specific client connection entity that sent the request
                        rpcEcb.AddComponent(sendRpcEntity, new SendRpcCommandRequest
                        {
                            TargetConnection = requestingConnection // Target the requesting connection
                        });
                        Debug.Log($"<color=yellow>[TerrainChunkLoaderSystem] Responding to RequestChunkRPC for chunk {rpc.ChunkCoordinate} to connection {requestingConnection.Index}</color>");

                    }
                    else
                    {
                        // Chunk entity exists but is missing density data (shouldn't happen if loading logic is correct)
                        Debug.LogError($"[TerrainChunkLoaderSystem] Received RequestChunkRPC for chunk {rpc.ChunkCoordinate} (Entity {chunkEntity.Index}) but DensityBuffer is missing!");
                    }
                }
                else
                {
                    // Chunk does not exist on the server yet. Log a warning or trigger loading.
                    Debug.LogWarning($"[TerrainChunkLoaderSystem] Received RequestChunkRPC for chunk {rpc.ChunkCoordinate} that is not currently loaded/mapped on the server.");
                    // TODO: Potentially trigger loading for this chunk for the requesting player here.
                }

                // Destroy the processed RPC entity using ECB
                rpcEcb.DestroyEntity(rpcEntity);
            }

            requestChunkRpcEntities.Dispose();
            requestChunkRpcs.Dispose();
            receiveRpcCommandRequests.Dispose();

            // Playback the command buffers to apply changes to the EntityManager
            ecb.Playback(state.EntityManager);
            rpcEcb.Playback(state.EntityManager);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            // Dispose NativeHashSets on all PlayerChunkLoader components when the system is destroyed.
            // This is crucial for ISystemStateComponentData containing NativeContainers.
            var playerChunkLoaderQuery = SystemAPI.QueryBuilder()
                .WithAll<PlayerChunkLoader>()
                .Build();

            var playerChunkLoaders = playerChunkLoaderQuery.ToComponentDataArray<PlayerChunkLoader>(Allocator.Temp);

            for (int i = 0; i < playerChunkLoaders.Length; i++)
            {
                var playerChunkLoader = playerChunkLoaders[i];
                playerChunkLoader.Dispose();
            }

            playerChunkLoaders.Dispose();

            // Dispose of the NativeHashMap when the system is destroyed
            if (_chunkEntityMap.IsCreated)
            {
                _chunkEntityMap.Dispose();
            }
        }
    }
} 
