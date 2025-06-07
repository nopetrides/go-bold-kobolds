using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamteck.Splines
{
	[Serializable]
	public class ColorModifier : SplineSampleModifier
	{
		public ColorKey[] keys = new ColorKey[0];

		public ColorModifier()
		{
			keys = new ColorKey[0];
		}

		public override bool hasKeys => keys.Length > 0;

		public override List<Key> GetKeys()
		{
			return new List<Key>(keys);
		}

		public override void SetKeys(List<Key> input)
		{
			keys = new ColorKey[input.Count];
			for (var i = 0; i < input.Count; i++) keys[i] = (ColorKey) input[i];
			base.SetKeys(input);
		}

		public void AddKey(double f, double t)
		{
			ArrayUtility.Add(ref keys, new ColorKey(f, t));
		}

		public override void Apply(ref SplineSample result)
		{
			if (keys.Length == 0) return;
			base.Apply(ref result);
			for (var i = 0; i < keys.Length; i++)
				result.color = keys[i].Blend(result.color, keys[i].Evaluate(result.percent) * blend);
		}

		[Serializable]
		public class ColorKey : Key
		{
			public enum BlendMode
			{
				Lerp,
				Multiply,
				Add,
				Subtract
			}

			public Color color = Color.white;
			public BlendMode blendMode = BlendMode.Lerp;

			public ColorKey(double f, double t) : base(f, t)
			{
			}

			public Color Blend(Color input, float percent)
			{
				switch (blendMode)
				{
					case BlendMode.Lerp: return Color.Lerp(input, color, blend * percent);
					case BlendMode.Add: return input + color * blend * percent;
					case BlendMode.Subtract: return input - color * blend * percent;
					case BlendMode.Multiply: return Color.Lerp(input, input * color, blend * percent);
					default: return input;
				}
			}
		}
	}
}
