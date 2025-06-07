using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using Kobolds.Utilities;
using System.Collections.Generic;

namespace Kobolds.Rendering.MonoBehaviours
{
    /// <summary>
    /// MonoBehaviour responsible for rendering and providing collision for a terrain chunk.
    /// Bridges ECS density data to Unity's rendering and physics systems.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class TerrainChunkRenderer : MonoBehaviour
    {
        // Cached references to required components
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;

        // The generated mesh object for this chunk
        private Mesh _chunkMesh;

        // Lists to hold generated mesh data
        private List<Vector3> _vertices = new List<Vector3>();
        private List<int> _triangles = new List<int>();
        private List<Vector3> _normals = new List<Vector3>();

        // The coordinate of this chunk in chunk space
        public int3 ChunkCoord { get; private set; }
        // The size of the chunk (should match ChunkCoordinator.ChunkSize)
        public int ChunkSize { get; private set; }
        // The ECS entity this renderer is associated with
        public Entity Entity { get; private set; }

        /// <summary>
        /// Called when the script instance is being loaded.
        /// Get references to required components.
        /// </summary>
        private void Awake()
        {
            // Get MeshFilter component
            _meshFilter = GetComponent<MeshFilter>();
            // Get MeshRenderer component
            _meshRenderer = GetComponent<MeshRenderer>();
            // Get MeshCollider component
            _meshCollider = GetComponent<MeshCollider>();
            // Ensure mesh collider is not convex for complex cave shapes
            _meshCollider.convex = false;
        }

        /// <summary>
        /// Initializes the chunk renderer with coordinate, size, and associated ECS entity.
        /// Positions the GameObject and sets its name.
        /// </summary>
        /// <param name="chunkCoord">The chunk's coordinate.</param>
        /// <param name="chunkSize">The size of the chunk.</param>
        /// <param name="entity">The associated ECS entity.</param>
        public void Initialize(int3 chunkCoord, int chunkSize, Entity entity)
        {
            // Set chunk properties
            ChunkCoord = chunkCoord;
            ChunkSize = chunkSize;
            Entity = entity;
            // Position the GameObject at the chunk's world position (min corner)
            transform.position = new Vector3(chunkCoord.x * chunkSize, chunkCoord.y * chunkSize, chunkCoord.z * chunkSize);
            // Set the GameObject's name for debugging
            gameObject.name = $"Chunk ({chunkCoord.x},{chunkCoord.y},{chunkCoord.z})";

            // Ensure renderer and collider are initially enabled
            _meshRenderer.enabled = true;
            _meshCollider.enabled = true;
        }

        /// <summary>
        /// Updates the mesh based on new density data using Marching Cubes.
        /// </summary>
        /// <param name="densityData">NativeArray of density bytes.</param>
        /// <param name="threshold">The density threshold for mesh generation.</param>
        /// <param name="generateSmoothNormals">Whether to generate smooth per-vertex normals.</param>
        public void UpdateMesh(NativeArray<byte> densityData, byte threshold, bool generateSmoothNormals = true)
        {
            // Validate density data size
            if (densityData.Length != ChunkCoordinator.PointsPerChunk)
            {
                Debug.LogError($"[TerrainChunkRenderer] Invalid density data size for chunk {ChunkCoord}. Expected {ChunkCoordinator.PointsPerChunk}, got {densityData.Length}.");
                // Clear mesh and disable components to prevent errors
                ClearMesh();
                _meshRenderer.enabled = false;
                _meshCollider.enabled = false;
                return;
            }

            // Ensure the mesh object exists or create a new one
            if (_chunkMesh == null)
            {
                _chunkMesh = new Mesh();
                // Use 16-bit indices for efficiency
                _chunkMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
            }

            // Clear previous mesh data
            _chunkMesh.Clear();
            _vertices.Clear();
            _triangles.Clear();
            _normals.Clear();

            // Generate mesh data using the MarchingCubes utility
            MarchingCubes.GenerateMesh(densityData, _vertices, _triangles, _normals, generateSmoothNormals);

            // Check if any geometry was generated
            if (_vertices.Count > 0)
            {
                // Assign the generated data to the mesh
                _chunkMesh.SetVertices(_vertices);
                _chunkMesh.SetTriangles(_triangles, 0);
                _chunkMesh.SetNormals(_normals);

                // Recalculate mesh bounds
                _chunkMesh.RecalculateBounds();

                // Mark the mesh as dynamic if it will be modified by digging
                _chunkMesh.MarkDynamic();

                // Assign the mesh to the MeshFilter and MeshCollider
                _meshFilter.sharedMesh = _chunkMesh;
                _meshCollider.sharedMesh = _chunkMesh;

                // Enable renderer and collider if they were disabled
                _meshRenderer.enabled = true;
                _meshCollider.enabled = true;

                Debug.Log($"[TerrainChunkRenderer] Generated mesh for chunk {ChunkCoord} with {_vertices.Count} vertices, {_triangles.Count / 3} triangles.");
            }
            else
            {
                // If no vertices were generated (all air or all solid), clear the mesh and disable components
                ClearMesh();
                _meshRenderer.enabled = false;
                _meshCollider.enabled = false;
                Debug.Log($"[TerrainChunkRenderer] No mesh generated for chunk {ChunkCoord} (all air or solid).");
            }
        }

        /// <summary>
        /// Clears the mesh data.
        /// </summary>
        private void ClearMesh()
        {
            // Clear the mesh data if the mesh object exists
            if (_chunkMesh != null)
            {
                _chunkMesh.Clear();
            }
            // Clear lists just in case
            _vertices.Clear();
            _triangles.Clear();
            _normals.Clear();
        }

        /// <summary>
        /// Cleans up mesh data for pooling or reuse.
        /// </summary>
        public void Cleanup()
        {
            // Clear mesh data
            ClearMesh();
            // Reset component state for potential reuse
            _meshRenderer.enabled = false;
            _meshCollider.enabled = false;
            ChunkCoord = int3.zero; // Reset coordinate
            ChunkSize = 0; // Reset size
            Entity = Entity.Null; // Reset associated entity
        }

        /// <summary>
        /// Called when the GameObject is being destroyed.
        /// Dispose of the mesh object to prevent memory leaks.
        /// </summary>
        private void OnDestroy()
        {
            // Dispose the mesh if it was created and is not currently assigned to a mesh filter (implying it might be part of a pool)
            // A more robust pooling system would handle mesh disposal explicitly.
            if (_chunkMesh != null)
            {
                // Destroy the mesh object
                Mesh.Destroy(_chunkMesh);
                _chunkMesh = null;
            }
        }
    }
} 