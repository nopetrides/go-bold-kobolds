using System.Collections.Generic;
using Unity.Mathematics;

namespace Kobolds.Utilities
{
	/// <summary>
	///     Utility for handling chunk coordinate calculations and multi-ownership boundary logic.
	/// </summary>
	public static class ChunkCoordinator
	{
		// The size of each chunk along one axis (8 units)
		public const int ChunkSize = 8;

		// The number of density points per chunk (8x8x8)
		public const int PointsPerChunk = ChunkSize * ChunkSize * ChunkSize;

		/// <summary>
		///     Returns all chunk coordinates that own the given world position.
		/// </summary>
		/// <param name="worldPos">The world position to check.</param>
		/// <returns>List of int3 chunk coordinates that own this point.</returns>
		public static List<int3> GetOwningChunks(float3 worldPos)
		{
			// Floor the world position to integer grid
			var grid = (int3) math.floor(worldPos);
			// List to store all owning chunk coordinates
			var owningChunks = new List<int3>(8);
			// For each axis, a point can be on the boundary (shared by 1 or 2 chunks per axis)
			for (var dx = 0; dx <= 1; dx++)
			{
				for (var dy = 0; dy <= 1; dy++)
				{
					for (var dz = 0; dz <= 1; dz++)
					{
						// For each axis, if the point is on a chunk boundary, it is owned by both adjacent chunks
						var chunkCoord = new int3(
							(grid.x - dx) / ChunkSize,
							(grid.y - dy) / ChunkSize,
							(grid.z - dz) / ChunkSize
						);
						// Only add if this chunk actually contains the point in its 8x8x8 grid
						if (IsPointInChunk(chunkCoord, grid)) owningChunks.Add(chunkCoord);
					}
				}
			}

			return owningChunks;
		}

		/// <summary>
		///     Checks if a grid point is within the chunk's 8x8x8 grid.
		/// </summary>
		/// <param name="chunkCoord">Chunk coordinate.</param>
		/// <param name="grid">Grid point (int3).</param>
		/// <returns>True if the chunk contains the grid point.</returns>
		public static bool IsPointInChunk(int3 chunkCoord, int3 grid)
		{
			// Calculate the minimum grid point for this chunk
			var min = chunkCoord * ChunkSize;
			// Calculate the maximum grid point for this chunk
			var max = min + (ChunkSize - 1);
			// Check if the grid point is within the chunk's bounds
			return grid.x >= min.x && grid.x <= max.x &&
					grid.y >= min.y && grid.y <= max.y &&
					grid.z >= min.z && grid.z <= max.z;
		}

		/// <summary>
		///     Converts a world position to the primary chunk coordinate (the chunk whose min corner contains the point).
		/// </summary>
		/// <param name="worldPos">World position.</param>
		/// <returns>Chunk coordinate (int3).</returns>
		public static int3 WorldToChunkCoord(float3 worldPos)
		{
			// Floor the world position to integer grid, then divide by chunk size
			var grid = (int3) math.floor(worldPos);
			return new int3(
				grid.x / ChunkSize,
				grid.y / ChunkSize,
				grid.z / ChunkSize
			);
		}

		/// <summary>
		///     Converts a world position to the local index in a chunk's 8x8x8 density array.
		/// </summary>
		/// <param name="chunkCoord">Chunk coordinate.</param>
		/// <param name="worldPos">World position.</param>
		/// <returns>Linear index (0-511) in the chunk's density array.</returns>
		public static int WorldToChunkLocalIndex(int3 chunkCoord, float3 worldPos)
		{
			// Floor the world position to integer grid
			var grid = (int3) math.floor(worldPos);
			// Calculate the local position within the chunk
			var local = grid - chunkCoord * ChunkSize;
			// Clamp to [0,7] in each axis
			local = math.clamp(local, 0, ChunkSize - 1);
			// Convert to linear index (z-major order)
			return local.z * ChunkSize * ChunkSize + local.y * ChunkSize + local.x;
		}

		/// <summary>
		///     Converts a chunk coordinate and local index to world grid position.
		/// </summary>
		/// <param name="chunkCoord">Chunk coordinate.</param>
		/// <param name="localIndex">Linear index (0-511).</param>
		/// <returns>World grid position (int3).</returns>
		public static int3 ChunkLocalIndexToWorldGrid(int3 chunkCoord, int localIndex)
		{
			// Convert linear index to local x, y, z
			var z = localIndex / (ChunkSize * ChunkSize);
			var y = localIndex / ChunkSize % ChunkSize;
			var x = localIndex % ChunkSize;
			// Calculate world grid position
			return chunkCoord * ChunkSize + new int3(x, y, z);
		}

		/// <summary>
		///     Returns the world grid position of the minimum corner of a chunk.
		/// </summary>
		/// <param name="chunkCoord">Chunk coordinate.</param>
		/// <returns>World grid position (int3).</returns>
		public static int3 ChunkMinGrid(int3 chunkCoord)
		{
			// Multiply chunk coordinate by chunk size
			return chunkCoord * ChunkSize;
		}

		/// <summary>
		///     Returns the world grid position of the maximum corner of a chunk.
		/// </summary>
		/// <param name="chunkCoord">Chunk coordinate.</param>
		/// <returns>World grid position (int3).</returns>
		public static int3 ChunkMaxGrid(int3 chunkCoord)
		{
			// Add (ChunkSize-1) to each axis
			return chunkCoord * ChunkSize + (ChunkSize - 1);
		}
	}
}
