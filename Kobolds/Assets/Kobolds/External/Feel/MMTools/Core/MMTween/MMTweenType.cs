using System;
using UnityEngine;

namespace MoreMountains.Tools
{
	public enum MMTweenDefinitionTypes
	{
		MMTween,
		AnimationCurve
	}

	[Serializable]
	public class MMTweenType
	{
		public MMTweenDefinitionTypes MMTweenDefinitionType = MMTweenDefinitionTypes.MMTween;
		public MMTween.MMTweenCurve MMTweenCurve = MMTween.MMTweenCurve.EaseInCubic;
		public AnimationCurve Curve = new(new Keyframe(0, 0), new Keyframe(1, 1f));
		public bool Initialized;

		public string ConditionPropertyName = "";
		public string EnumConditionPropertyName = "";
		public bool[] EnumConditions = new bool[32];

		public MMTweenType(
			MMTween.MMTweenCurve newCurve, string conditionPropertyName = "", string enumConditionPropertyName = "",
			params int[] enumConditionValues)
		{
			MMTweenCurve = newCurve;
			MMTweenDefinitionType = MMTweenDefinitionTypes.MMTween;
			ConditionPropertyName = conditionPropertyName;
			EnumConditionPropertyName = enumConditionPropertyName;
			for (var i = 0; i < enumConditionValues.Length; i++) EnumConditions[enumConditionValues[i]] = true;
		}

		public MMTweenType(
			AnimationCurve newCurve, string conditionPropertyName = "", string enumConditionPropertyName = "",
			params int[] enumConditionValues)
		{
			Curve = newCurve;
			MMTweenDefinitionType = MMTweenDefinitionTypes.AnimationCurve;
			ConditionPropertyName = conditionPropertyName;
			EnumConditionPropertyName = enumConditionPropertyName;
			for (var i = 0; i < enumConditionValues.Length; i++) EnumConditions[enumConditionValues[i]] = true;
		}

		public static MMTweenType DefaultEaseInCubic { get; } = new(MMTween.MMTweenCurve.EaseInCubic);

		public float Evaluate(float t)
		{
			return MMTween.Evaluate(t, this);
		}
	}
}
