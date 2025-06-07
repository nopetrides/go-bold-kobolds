#if MM_URP
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MoreMountains.Feedbacks
{
	[AddComponentMenu("More Mountains/Springs/MM Spring Color Adjustments Contrast URP")]
	public class MMSpringColorAdjustmentsContrast_URP : MMSpringFloatComponent<Volume>
	{
		protected ColorAdjustments _colorAdjustments;

		public override float TargetFloat
		{
			get => _colorAdjustments.contrast.value;
			set => _colorAdjustments.contrast.Override(value);
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
