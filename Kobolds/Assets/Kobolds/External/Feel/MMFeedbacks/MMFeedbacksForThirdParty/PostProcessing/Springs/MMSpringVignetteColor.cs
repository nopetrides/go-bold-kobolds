#if MM_POSTPROCESSING
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace MoreMountains.Feedbacks
{
	[AddComponentMenu("More Mountains/Springs/MM Spring Vignette Color")]
	public class MMSpringVignetteColor : MMSpringColorComponent<PostProcessVolume>
	{
		protected Vignette _vignette;

		public override Color TargetColor
		{
			get => _vignette.color;
			set => _vignette.color.Override(value);
		}

		protected override void Initialization()
		{
			if (Target == null) Target = gameObject.GetComponent<PostProcessVolume>();
			Target.profile.TryGetSettings(out _vignette);
			base.Initialization();
		}
	}
}
#endif
