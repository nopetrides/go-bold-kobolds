#if MM_POSTPROCESSING
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace MoreMountains.Feedbacks
{
	[AddComponentMenu("More Mountains/Springs/MM Spring Bloom Intensity")]
	public class MMSpringBloomIntensity : MMSpringFloatComponent<PostProcessVolume>
	{
		protected Bloom _bloom;

		public override float TargetFloat
		{
			get => _bloom.intensity;
			set => _bloom.intensity.Override(value);
		}

		protected override void Initialization()
		{
			if (Target == null) Target = gameObject.GetComponent<PostProcessVolume>();
			Target.profile.TryGetSettings(out _bloom);
			base.Initialization();
		}
	}
}
#endif
