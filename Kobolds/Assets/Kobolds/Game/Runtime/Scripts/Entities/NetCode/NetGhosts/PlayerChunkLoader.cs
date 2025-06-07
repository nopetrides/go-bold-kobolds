using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Kobolds.NetCode
{
	/// <summary>
	///     ECS component attached to player entities to track needed terrain chunks.
	///     Implements ISystemStateComponentData for NativeHashSet disposal.
	/// </summary>
	public struct PlayerChunkLoader : ICleanupComponentData, IDisposable
	{
		// The current chunk coordinate the player is located in
		public int3 CurrentChunkPosition;

		// The chunk coordinate the player was in during the last update
		public int3 LastChunkPosition;

		// Set of chunk coordinates currently loaded for this player
		// Allocator.Persistent is required because this NativeHashSet lives for the lifetime of the entity
		public NativeHashSet<int3> LoadedChunks;

		// Set of chunk coordinates requested from the server but not yet received
		// Allocator.Persistent is required because this NativeHashSet lives for the lifetime of the entity
		public NativeHashSet<int3> RequestedChunks;

		/// <summary>
		///     Disposes the NativeHashSets when the component is removed or entity is destroyed.
		///     Required because NativeContainers manage unmanaged memory.
		/// </summary>
		public void Dispose()
		{
			// Check if loadedChunks is created before disposing to avoid errors
			if (LoadedChunks.IsCreated) LoadedChunks.Dispose();
			// Check if requestedChunks is created before disposing to avoid errors
			if (RequestedChunks.IsCreated) RequestedChunks.Dispose();
		}
	}
}
