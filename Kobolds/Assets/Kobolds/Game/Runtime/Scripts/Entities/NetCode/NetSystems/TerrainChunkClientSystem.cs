using Kobolds.NetCode;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine; // For Debug.Log
using Kobolds.Rpc;
using Kobolds.Utilities;

namespace Kobolds.NetSystems
{
    /// <summary>
    /// Client system responsible for receiving terrain chunk data, generating meshes,
    /// and managing the corresponding TerrainChunkRenderer GameObjects.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct TerrainChunkClientSystem : ISystem
    {
        // EntityQuery for incoming ChunkDataRPCs
        private EntityQuery _chunkDataRpcQuery;
        // EntityQuery for incoming ChunkUnloadRPCs
        private EntityQuery _chunkUnloadRpcQuery;
        // ComponentLookup for TerrainSettings singleton
        private ComponentLookup<TerrainSettings> _terrainSettingsLookup;
        // ComponentLookup for TerrainChunk entities on the client (to check existence)
        private ComponentLookup<TerrainChunk> _terrainChunkLookup;
        // BufferLookup for DensityByte buffers on client-side TerrainChunk entities
        private BufferLookup<DensityByte> _densityBufferLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Create queries for the incoming RPCs
            _chunkDataRpcQuery = state.GetEntityQuery(typeof(ChunkDataRPC), typeof(ReceiveRpcCommandRequest));
            _chunkUnloadRpcQuery = state.GetEntityQuery(typeof(ChunkUnloadRPC), typeof(ReceiveRpcCommandRequest));

            // Get lookups for components (read-only for TerrainSettings)
            _terrainSettingsLookup = state.GetComponentLookup<TerrainSettings>(true);
            _terrainChunkLookup = state.GetComponentLookup<TerrainChunk>();
            _densityBufferLookup = state.GetBufferLookup<DensityByte>();

            // Require TerrainSettings to be present
            state.RequireForUpdate<TerrainSettings>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Access the singleton terrain settings
            // Ensure singleton exists before accessing
            if (!SystemAPI.HasSingleton<TerrainSettings>())
            {
                // Terrain settings not available yet, skip update
                return;
            }

            TerrainSettings terrainSettings = SystemAPI.GetSingleton<TerrainSettings>();
            int chunkSize = terrainSettings.ChunkSize;
            int globalSeed = terrainSettings.GlobalSeed; // Using the global seed from settings
            byte solidThreshold = terrainSettings.SolidThreshold;
            float noiseFrequency = 0.05f; // TODO: Make this configurable, potentially in TerrainSettings

            // Process incoming ChunkDataRPCs
            using (var chunkDataRpcEntities = _chunkDataRpcQuery.ToEntityArray(Allocator.Temp))
            {
                foreach (Entity rpcEntity in chunkDataRpcEntities)
                {
                    // Get the RPC data
                    ChunkDataRPC rpc = state.EntityManager.GetComponentData<ChunkDataRPC>(rpcEntity);
                    // Get the sender connection entity (for potential future use, e.g., acknowledgments)
                    // ReceiveRpcCommandRequest req = state.EntityManager.GetComponentData<ReceiveRpcCommandRequest>(rpcEntity); // Currently unused

                    Debug.Log($"<color=cyan>[TerrainChunkClientSystem] Received ChunkDataRPC for chunk {rpc.ChunkCoordinate}. Global Seed: {rpc.GlobalSeed}</color>");

                    // Check if a client-side TerrainChunk entity for this coordinate already exists
                    // TBD: Implement efficient lookup by coordinate if needed for performance.
                    Entity clientChunkEntity = Entity.Null;

                    // For simplicity, using a query for now. More robust: NativeHashMap managed by the system or manager.
                    // This part is not Burst-compiled as it involves EntityManager operations.
                    var terrainChunks = SystemAPI.QueryBuilder()
                        .WithAll<TerrainChunk>()
                        .Build();

                    var terrainChunkEntities = terrainChunks.ToEntityArray(Allocator.Temp);
                    var terrainChunkComponents = terrainChunks.ToComponentDataArray<TerrainChunk>(Allocator.Temp);

                    for (int i = 0; i < terrainChunkEntities.Length; i++)
                    {
                        if (terrainChunkComponents[i].ChunkCoord.Equals(rpc.ChunkCoordinate))
                        {
                            clientChunkEntity = terrainChunkEntities[i];
                            break;
                        }
                    }

                    terrainChunkEntities.Dispose();
                    terrainChunkComponents.Dispose();

                    if (clientChunkEntity == Entity.Null)
                    {
                        // Create a new client-side TerrainChunk entity
                        clientChunkEntity = state.EntityManager.CreateEntity();
                        state.EntityManager.AddComponentData(clientChunkEntity, new TerrainChunk
                        {
                            ChunkCoord = rpc.ChunkCoordinate,
                            // Client uses the global seed from the server provided in the RPC
                            ChunkSeed = rpc.GlobalSeed // Use the seed from RPC for deterministic generation
                        });

                        // Add the density buffer
                        state.EntityManager.AddBuffer<DensityByte>(clientChunkEntity);

                        Debug.Log($"<color=cyan>[TerrainChunkClientSystem] Created client-side entity for chunk {rpc.ChunkCoordinate}</color>");
                    }

                    // Get the density buffer
                    // We need a reference to the buffer itself, not just the lookup, for modification
                    var densityBuffer = state.EntityManager.GetBuffer<DensityByte>(clientChunkEntity);

                    // Deterministically generate density data on the client using the received seed and noise utility
                    densityBuffer.ResizeUninitialized(ChunkCoordinator.PointsPerChunk);

                    // Noise generation logic can be Burst-compiled
                    new GenerateDensityJob
                    {
                        DensityBuffer = densityBuffer.AsNativeArray(),
                        ChunkCoord = rpc.ChunkCoordinate,
                        GlobalSeed = rpc.GlobalSeed,
                        ChunkSize = ChunkCoordinator.ChunkSize,
                        NoiseFrequency = noiseFrequency
                    }.Run(); // Use .Run() to execute directly on the main thread or .Schedule() for a job


                    // Add tag to signal the GameObject manager system that the density is ready
                    // This tag indicates the density buffer has been populated/updated.
                    state.EntityManager.AddComponent<NeedsMeshGenerationTag>(clientChunkEntity);

                    // Destroy the processed RPC entity
                    state.EntityManager.DestroyEntity(rpcEntity);
                }
            }

			// Query for chunks with updated density (from server replication)
			var ecb = new EntityCommandBuffer(Allocator.TempJob);

			var updatedDensityQuery = SystemAPI.QueryBuilder()
				.WithAll<TerrainChunk>()
				.WithNone<NeedsMeshGenerationTag>() // Exclude entities that already have the tag
				.Build();

			// Set change filter for DensityByte buffer
			updatedDensityQuery.SetChangedVersionFilter(ComponentType.ReadWrite<DensityByte>());

			var updatedDensityEntities = updatedDensityQuery.ToEntityArray(Allocator.Temp);
			var updatedDensityChunks = updatedDensityQuery.ToComponentDataArray<TerrainChunk>(Allocator.Temp);

			for (int i = 0; i < updatedDensityEntities.Length; i++)
			{
				Entity entity = updatedDensityEntities[i];
				TerrainChunk chunk = updatedDensityChunks[i];

				// Add the tag to trigger mesh regeneration for this chunk using the ECB
				Debug.Log($"<color=cyan>[TerrainChunkClientSystem] Detected density change for chunk {chunk.ChunkCoord}. Adding NeedsMeshGenerationTag.</color>");
				ecb.AddComponent<NeedsMeshGenerationTag>(entity);
			}

			updatedDensityEntities.Dispose();
			updatedDensityChunks.Dispose();

            for (int i = 0; i < updatedDensityEntities.Length; i++)
            {
                Entity entity = updatedDensityEntities[i];
                TerrainChunk chunk = updatedDensityChunks[i];

                // Add the tag to trigger mesh regeneration for this chunk using the ECB
                Debug.Log($"<color=cyan>[TerrainChunkClientSystem] Detected density change for chunk {chunk.ChunkCoord}. Adding NeedsMeshGenerationTag.</color>");
                ecb.AddComponent<NeedsMeshGenerationTag>(entity); // Use ECB instead of EntityManager
            }

            updatedDensityEntities.Dispose();
            updatedDensityChunks.Dispose();

            // Process incoming ChunkUnloadRPCs
            using (var chunkUnloadRpcEntities = _chunkUnloadRpcQuery.ToEntityArray(Allocator.Temp))
            {
                foreach (Entity rpcEntity in chunkUnloadRpcEntities)
                {
                    // Get the RPC data
                    ChunkUnloadRPC rpc = state.EntityManager.GetComponentData<ChunkUnloadRPC>(rpcEntity);

                    Debug.Log($"<color=cyan>[TerrainChunkClientSystem] Received ChunkUnloadRPC for chunk {rpc.ChunkCoordinate}</color>");

                    // TBD: Find the client-side TerrainChunk entity by coordinate.
                    // A map (int3 -> Entity) would be efficient for this lookup.
                    Entity clientChunkEntity = Entity.Null;

                    // For simplicity, using a query for now.
                    var terrainChunks = SystemAPI.QueryBuilder()
                        .WithAll<TerrainChunk>()
                        .Build();

                    var terrainChunkEntities = terrainChunks.ToEntityArray(Allocator.Temp);
                    var terrainChunkComponents = terrainChunks.ToComponentDataArray<TerrainChunk>(Allocator.Temp);

                    for (int i = 0; i < terrainChunkEntities.Length; i++)
                    {
                        if (terrainChunkComponents[i].ChunkCoord.Equals(rpc.ChunkCoordinate))
                        {
                            clientChunkEntity = terrainChunkEntities[i];
                            break;
                        }
                    }

                    terrainChunkEntities.Dispose();
                    terrainChunkComponents.Dispose();

                    if (clientChunkEntity != Entity.Null)
                    {
                        // Add the cleanup tag to signal the GameObject manager system that this chunk needs cleanup
                        // before being destroyed
                        state.EntityManager.AddComponent<TerrainChunkCleanupTag>(clientChunkEntity);

                        Debug.Log($"<color=cyan>[TerrainChunkClientSystem] Marked chunk {rpc.ChunkCoordinate} for cleanup and destruction</color>");

                        // The TerrainChunkGameObjectManagerSystem will handle the GameObject cleanup
                        // and then we'll destroy the entity in the next frame
                        ecb.DestroyEntity(clientChunkEntity);
                    }

                    // Destroy the processed RPC entity
                    state.EntityManager.DestroyEntity(rpcEntity);
                }
            }

            // Playback the command buffer created for handling density changes
            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            // TODO: Refine GameObject management interaction (using tags/commands instead of direct entity lookup in Manager System).
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            // System cleanup if needed
        }

        // Burst-compatible job to generate density data
        // Can be scheduled on multiple threads for performance
        [BurstCompile]
        public struct GenerateDensityJob : IJob
        {
            // NativeArray for the density buffer (must match size of DensityBuffer)
            public NativeArray<DensityByte> DensityBuffer;
            // Chunk coordinate
            public int3 ChunkCoord;
            // Global seed for noise
            public int GlobalSeed;
            // Size of the chunk
            public int ChunkSize;
            // Frequency for noise generation
            public float NoiseFrequency;

            public void Execute()
            {
                // Calculate chunk-specific seed
                int chunkSeed = (int)(GlobalSeed + math.hash(ChunkCoord)); // Ensure this matches server calculation

                // Iterate through all voxel points in the chunk
                for (int i = 0; i < ChunkCoordinator.PointsPerChunk; i++)
                {
                     // Convert linear index to local 3D position within the chunk (0 to ChunkSize-1)
                     int3 localPos = new int3(
                        i % ChunkSize,
                        (i / ChunkSize) % ChunkSize,
                        i / (ChunkSize * ChunkSize)
                    );

                    // Calculate the world grid position of this voxel
                    int3 worldGridPos = ChunkCoord * ChunkSize + localPos;

                    // Generate noise value using the utility function
                    float noiseValue = FastNoiseLite.GetNoise(new float3(worldGridPos), chunkSeed, NoiseFrequency);

                    // Map noise value (-1 to 1) to density byte (0 to 255)
                    // This mapping can be adjusted to control terrain shape
                    byte density = (byte)math.clamp((noiseValue + 1.0f) * 127.5f, 0, 255); // Map [-1, 1] to [0, 255]

                    // TODO: Apply other terrain generation rules here (e.g., caves below certain Y, surfaces above)
                    // This simple example primarily uses noise.

                    // Assign the calculated density to the buffer
                    DensityBuffer[i] = new DensityByte { Value = density };
                }
            }
        }
    }
} 
