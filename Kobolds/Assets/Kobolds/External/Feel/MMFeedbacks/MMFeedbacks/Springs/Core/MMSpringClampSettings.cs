using System;
using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.Feedbacks
{
	[Serializable]
	public class MMSpringClampSettings
	{
		[Header("Min")]
		/// whether or not to clamp the min value of this spring, preventing it from going below a certain value
		[Tooltip(
			"whether or not to clamp the min value of this spring, preventing it from going below a certain value")]
		public bool ClampMin;

		/// the value below which this spring can't go
		[Tooltip("the value below which this spring can't go")]
		[MMCondition("ClampMin", true)]
		public float ClampMinValue;

		/// if ClampMin is true, whether or not to use the initial value as the min value
		[Tooltip("if ClampMin is true, whether or not to use the initial value as the min value")]
		[MMCondition("ClampMin", true)]
		public bool ClampMinInitial;

		/// whether or not the spring should bounce off the min value or not
		[Tooltip("whether or not the spring should bounce off the min value or not")]
		[MMCondition("ClampMin", true)]
		public bool ClampMinBounce;

		[Header("Max")]
		/// whether or not to clamp the max value of this spring, preventing it from going above a certain value
		[Tooltip(
			"whether or not to clamp the max value of this spring, preventing it from going above a certain value")]
		public bool ClampMax;

		/// the value above which this spring can't go
		[Tooltip("the value above which this spring can't go")]
		[MMCondition("ClampMax", true)]
		public float ClampMaxValue = 10f;

		/// if ClampMax is true, whether or not to use the initial value as the max value
		[Tooltip("if ClampMax is true, whether or not to use the initial value as the max value")]
		[MMCondition("ClampMax", true)]
		public bool ClampMaxInitial;

		/// whether or not the spring should bounce off the max value or not
		[Tooltip("whether or not the spring should bounce off the max value or not")]
		[MMCondition("ClampMax", true)]
		public bool ClampMaxBounce;

		public bool ClampNeeded => ClampMin || ClampMax || ClampMinBounce || ClampMaxBounce;

		public virtual float GetTargetValue(float value, float initialValue)
		{
			var targetValue = value;
			var clampMinValue = ClampMinInitial ? initialValue : ClampMinValue;
			if (ClampMin && value < clampMinValue) targetValue = clampMinValue;
			var clampMaxValue = ClampMaxInitial ? initialValue : ClampMaxValue;
			if (ClampMax && value > clampMaxValue) targetValue = clampMaxValue;
			return targetValue;
		}
	}
}
