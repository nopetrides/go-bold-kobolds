#if MM_POSTPROCESSING
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace MoreMountains.Feedbacks
{
	[AddComponentMenu("More Mountains/Springs/MM Spring Chromatic Aberration Intensity")]
	public class MMSpringChromaticAberrationIntensity : MMSpringFloatComponent<PostProcessVolume>
	{
		protected ChromaticAberration _chromaticAberration;

		public override float TargetFloat
		{
			get => _chromaticAberration.intensity;
			set => _chromaticAberration.intensity.Override(value);
		}

		protected override void Initialization()
		{
			if (Target == null) Target = gameObject.GetComponent<PostProcessVolume>();
			Target.profile.TryGetSettings(out _chromaticAberration);
			base.Initialization();
		}
	}
}
#endif
