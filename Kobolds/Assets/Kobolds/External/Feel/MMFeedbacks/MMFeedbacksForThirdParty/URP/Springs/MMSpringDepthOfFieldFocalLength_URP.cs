#if MM_URP
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MoreMountains.Feedbacks
{
	[AddComponentMenu("More Mountains/Springs/MM Spring Depth Of Field Focal Length URP")]
	public class MMSpringDepthOfFieldFocalLength_URP : MMSpringFloatComponent<Volume>
	{
		protected DepthOfField _depthOfField;

		public override float TargetFloat
		{
			get => _depthOfField.focalLength.value;
			set => _depthOfField.focalLength.Override(value);
		}

		protected override void Initialization()
		{
			if (Target == null) Target = gameObject.GetComponent<Volume>();
			Target.profile.TryGet(out _depthOfField);
			base.Initialization();
		}
	}
}
#endif
