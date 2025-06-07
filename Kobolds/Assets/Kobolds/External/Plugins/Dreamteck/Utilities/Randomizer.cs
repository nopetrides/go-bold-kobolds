using UnityEngine;
using Random = System.Random;

namespace Dreamteck.Utilities
{
	public class Randomizer
	{
		private readonly int _seed;

		public Randomizer(int seed)
		{
			_seed = seed;
			random = new Random(_seed);
		}

		public Random random { get; private set; }

		public float Random01()
		{
			return (float) random.NextDouble();
		}

		public float Random(float min, float max)
		{
			return (float) DMath.Lerp(min, max, random.NextDouble());
		}

		public int Random(int min, int max)
		{
			return (int) DMath.Lerp(min, max, random.NextDouble());
		}

		public Vector2 RandomVector2(float min, float max)
		{
			return new Vector2(Random(min, max), Random(min, max));
		}

		public Vector3 RandomVector3(float min, float max)
		{
			return new Vector3(Random(min, max), Random(min, max), Random(min, max));
		}

		public Vector3 OnUnitSphere()
		{
			return Quaternion.Euler(Random(0f, 360f), Random(0f, 360f), Random(0f, 360f)) * Vector3.forward;
		}

		public Vector3 OnUnitCircle()
		{
			return Quaternion.AngleAxis(Random(0f, 360f), Vector3.forward) * Vector3.up;
		}

		public Vector3 InsideUnitSphere()
		{
			return OnUnitSphere() * Random01();
		}

		public Vector3 InsideUnitCircle()
		{
			return OnUnitCircle() * Random01();
		}

		public void Reset()
		{
			random = new Random(_seed);
		}

		public void Reset(int seed)
		{
			random = new Random(seed);
		}
	}
}
