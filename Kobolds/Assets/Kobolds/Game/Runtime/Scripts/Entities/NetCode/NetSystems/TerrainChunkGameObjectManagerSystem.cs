using System.Collections.Generic;
using Kobolds.NetCode;
using Kobolds.Rendering.MonoBehaviours;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Kobolds.Rendering
{
	/// <summary>
	///     Client-side system that manages the creation, updating, and destruction of TerrainChunkRenderer GameObjects
	///     based on the state of ECS TerrainChunk entities.
	///     Runs on the main thread (PresentationSystemGroup).
	/// </summary>
	[UpdateInGroup(typeof(PresentationSystemGroup))]
	public partial class TerrainChunkGameObjectManagerSystem : SystemBase
	{
		// Query for TerrainChunk entities that need their mesh generated/updated
		private EntityQuery _chunksNeedingMeshQuery;

		// Query for TerrainChunk entities that are being destroyed (for cleanup)
		private EntityQuery _chunksBeingDestroyedQuery;

		// ComponentLookup for accessing TerrainSettings singleton
		private ComponentLookup<TerrainSettings> _terrainSettingsLookup;

		// BufferLookup for DensityByte buffers
		private BufferLookup<DensityByte> _densityBufferLookup;

		// Map to link ECS entities to their corresponding TerrainChunkRenderer GameObjects
		private Dictionary<Entity, TerrainChunkRenderer> _entityRendererMap;

		// Pool of inactive TerrainChunkRenderer GameObjects
		private List<TerrainChunkRenderer> _rendererPool;

		// TODO: Reference to the TerrainChunkRenderer prefab - MUST be set up in the Unity Editor
		public GameObject TerrainChunkRendererPrefab; // Public field to be assigned in Inspector

		protected override void OnCreate()
		{
			// Query for chunks needing mesh generation (have TerrainChunk, DensityByte, and NeedsMeshGenerationTag)
			_chunksNeedingMeshQuery = GetEntityQuery(
				typeof(TerrainChunk),
				typeof(DensityByte),
				typeof(NeedsMeshGenerationTag)
			);

			// Query for chunks that are marked for cleanup before being destroyed
			_chunksBeingDestroyedQuery = GetEntityQuery(
				typeof(TerrainChunk),
				typeof(TerrainChunkCleanupTag)
			);
			// This approach uses a cleanup tag added by TerrainChunkClientSystem before destroying entities

			// Get lookups
			_terrainSettingsLookup = GetComponentLookup<TerrainSettings>(true);
			_densityBufferLookup = GetBufferLookup<DensityByte>(true);

			// Initialize the map and pool
			_entityRendererMap = new Dictionary<Entity, TerrainChunkRenderer>(1024);
			_rendererPool = new List<TerrainChunkRenderer>(1024);

			// Ensure the system runs on the main thread for GameObject operations
			// [UpdateInGroup(typeof(PresentationSystemGroup))] attribute already does this.
		}

		protected override void OnUpdate()
		{
			// Process chunks that are marked for cleanup (about to be destroyed)
			// Create an EntityCommandBuffer for removing the cleanup tag after processing
			var cleanupEcb = new EntityCommandBuffer(Allocator.TempJob);

			// Use `ToEntityArray` to get the entity list from the query
			var entitiesBeingDestroyed = _chunksBeingDestroyedQuery.ToEntityArray(Allocator.TempJob);
			var chunksBeingDestroyed = _chunksBeingDestroyedQuery.ToComponentDataArray<TerrainChunk>(Allocator.TempJob);

			for (var i = 0; i < entitiesBeingDestroyed.Length; i++)
			{
				var entity = entitiesBeingDestroyed[i];
				var chunk = chunksBeingDestroyed[i];

				// Check if we have a renderer mapped to this entity
				if (_entityRendererMap.TryGetValue(entity, out var renderer))
				{
					// Remove mapping
					_entityRendererMap.Remove(entity);

					// Clean up the renderer and return it to the pool
					renderer.Cleanup();
					_rendererPool.Add(renderer);

					Debug.Log($"[TerrainChunkGameObjectManagerSystem] Cleaned up renderer for chunk {chunk.ChunkCoord} (entity {entity.Index})");
				}

				// Remove the cleanup tag to indicate that this entity has been processed
				cleanupEcb.RemoveComponent<TerrainChunkCleanupTag>(entity);
			}

			// Dispose of the temporary arrays
			entitiesBeingDestroyed.Dispose();
			chunksBeingDestroyed.Dispose();

			// Playback the command buffer to remove the cleanup tags
			cleanupEcb.Playback(EntityManager);
			cleanupEcb.Dispose();


			// Access the singleton terrain settings (must be available for mesh generation)
			if (!SystemAPI.HasSingleton<TerrainSettings>())
				// Terrain settings not available yet, skip mesh generation
				// Debug.LogWarning("[TerrainChunkGameObjectManagerSystem] TerrainSettings singleton not found."); // Too noisy
				return;

			var terrainSettings = SystemAPI.GetSingleton<TerrainSettings>();
			var chunkSize = terrainSettings.ChunkSize;
			var solidThreshold = terrainSettings.SolidThreshold;
			var generateSmoothNormals = true; // TODO: Make this configurable

			// Create an EntityCommandBuffer for structural changes
			var ecb = new EntityCommandBuffer(Allocator.TempJob);

			// Build an EntityQuery that targets entities with the specified components
			var query = SystemAPI.QueryBuilder()
				.WithAll<TerrainChunk, DensityByte, NeedsMeshGenerationTag>()
				.Build();

			// Add a change filter for components (NeedsMeshGenerationTag)
			query.SetChangedVersionFilter(ComponentType.ReadWrite<NeedsMeshGenerationTag>());

			// Fetch entities and component data from the filtered query
			var entityArray = query.ToEntityArray(Allocator.Temp);
			var chunkComponents = query.ToComponentDataArray<TerrainChunk>(Allocator.Temp);

			for (var i = 0; i < entityArray.Length; i++)
			{
				var entity = entityArray[i];
				var chunk = chunkComponents[i];

				// Get a renderer from the pool or instantiate a new one
				TerrainChunkRenderer renderer = InstantiateRenderer(chunk.ChunkCoord);
				if (renderer == null)
				{
					// Cannot create renderer, remove the tag and skip
					ecb.RemoveComponent<NeedsMeshGenerationTag>(entity);
					continue;
				}

				// Initialize the renderer
				renderer.Initialize(chunk.ChunkCoord, chunkSize, entity);

				// Get the density data buffer (read-only as this is in PresentationSystemGroup)
				// Note: Accessing buffer here requires SystemBase. It's not Burst-compatible.
				// If using a Job, would need NativeArray copy or other approach.
				// This system is intentionally on the main thread, so direct access is okay.
				if (_densityBufferLookup.HasBuffer(entity))
				{
					var densityBuffer = _densityBufferLookup[entity].AsNativeArray();
					// Update the mesh using the density data
					renderer.UpdateMesh(densityBuffer.Reinterpret<byte>(), solidThreshold, generateSmoothNormals);
				}
				else
				{
					Debug.LogError(
						$"[TerrainChunkGameObjectManagerSystem] DensityBuffer missing for chunk entity {entity.Index} ({chunk.ChunkCoord}). Cannot generate mesh.");
					// Clean up the renderer as it's invalid
					renderer.Cleanup();
					_rendererPool.Add(renderer);
				}

				// Add the entity-renderer mapping
				_entityRendererMap[entity] = renderer;

				// Remove the tag indicating mesh generation is complete using ECB
				ecb.RemoveComponent<NeedsMeshGenerationTag>(entity);
			}

			entityArray.Dispose();
			chunkComponents.Dispose();

			// Playback the command buffer
			ecb.Playback(EntityManager);
			ecb.Dispose();
		}

		// Helper method to get a renderer from the pool or instantiate a new one
		private TerrainChunkRenderer InstantiateRenderer(int3 chunkCoord)
		{
			// Try to get from pool first
			if (_rendererPool.Count > 0)
			{
				var renderer = _rendererPool[_rendererPool.Count - 1];
				_rendererPool.RemoveAt(_rendererPool.Count - 1);
				// Activate the GameObject
				renderer.gameObject.SetActive(true);
				return renderer;
			}

			// If no pooled renderer available, instantiate a new one
			if (TerrainChunkRendererPrefab != null)
			{
				// Instantiate a new renderer from prefab
				var go = GameObject.Instantiate(TerrainChunkRendererPrefab);
				var renderer = go.GetComponent<TerrainChunkRenderer>();
				Debug.Log($"[TerrainChunkGameObjectManagerSystem] Instantiated new renderer for chunk {chunkCoord}");
				return renderer;
			}

			// If prefab is not assigned, log error and return null
			Debug.LogError("[TerrainChunkGameObjectManagerSystem] TerrainChunkRendererPrefab is not assigned!");
			return null;
		}

		protected override void OnDestroy()
		{
			// Dispose of the Dictionary is not needed as it is managed memory.
			// Native containers in PlayerChunkLoader are disposed within its Dispose method, called by ECS cleanup.

			// TODO: Clean up any remaining GameObjects in the pool or map on shutdown
			// This depends on your pooling strategy.
			// For simplicity, let's destroy remaining renderers in the map and pool.
			if (_entityRendererMap != null)
			{
				foreach (var pair in _entityRendererMap)
					if (pair.Value != null && pair.Value.gameObject != null)
						GameObject.Destroy(pair.Value.gameObject);

				_entityRendererMap.Clear();
			}

			if (_rendererPool != null)
			{
				foreach (var renderer in _rendererPool)
					if (renderer != null && renderer.gameObject != null)
						GameObject.Destroy(renderer.gameObject);

				_rendererPool.Clear();
			}
		}
	}
}
