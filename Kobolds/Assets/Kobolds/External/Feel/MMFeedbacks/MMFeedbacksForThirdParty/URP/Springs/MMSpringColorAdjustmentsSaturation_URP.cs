#if MM_URP
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MoreMountains.Feedbacks
{
	[AddComponentMenu("More Mountains/Springs/MM Spring Color Adjustments Saturation URP")]
	public class MMSpringColorAdjustmentsSaturation_URP : MMSpringFloatComponent<Volume>
	{
		protected ColorAdjustments _colorAdjustments;

		public override float TargetFloat
		{
			get => _colorAdjustments.saturation.value;
			set => _colorAdjustments.saturation.Override(value);
		}

		protected override void Initialization()
		{
			if (Target == null) Target = gameObject.GetComponent<Volume>();
			Target.profile.TryGet(out _colorAdjustments);
			base.Initialization();
		}
	}
}
#endif
