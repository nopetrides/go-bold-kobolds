using Unity.Mathematics;
using Unity.Burst;
using UnityEngine; // For Debug.Log

namespace Kobolds.Utilities
{
    /// <summary>
    /// Utility for generating deterministic 3D noise, required for terrain generation.
    /// Wraps a noise implementation (e.g., Simplex or Perlin) for use with seeds and world coordinates.
    /// Note: This is a placeholder and would typically wrap a more complex noise library or algorithm.
    /// </summary>
    public static class FastNoiseLite
    {
        /// <summary>
        /// Generates deterministic 3D noise for a given world position and seed.
        /// </summary>
        /// <param name="worldPosition">The world position to sample noise at.</param>
        /// <param name="seed">The seed for deterministic generation.</param>
        /// <param name="frequency">The frequency/scale of the noise.</param>
        /// <returns>A noise value (typically in the range of -1.0 to 1.0).</returns>
        // BurstCompile is not strictly needed on a static method like this unless called from a Job,
        // but it's good practice if the underlying noise function could be Burst-compiled.
        public static float GetNoise(float3 worldPosition, int seed, float frequency = 0.1f)
        {
            // TODO: Implement a proper deterministic noise algorithm here.
            // For demonstration, using Unity.Mathematics.noise.snoise, which is already deterministic and Burst compatible.
            // In a real project, you might integrate a dedicated noise library or a custom implementation.
            
            // Add seed and scale by frequency
            float3 samplePoint = worldPosition * frequency + new float3(seed * 0.1f); 
            
            // Generate Simplex noise
            float noiseValue = noise.snoise(samplePoint);

            // Unity.Mathematics.noise.snoise typically returns values in [-1, 1]
            // Debug.Log($"Noise at {worldPosition} with seed {seed}: {noiseValue}"); // Avoid in tight loops

            return noiseValue;
        }

        // TODO: Add other noise types or parameters as needed (e.g., fractal noise, octaves, gain, lacunarity).
    }
} 