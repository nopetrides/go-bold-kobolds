#if MM_POSTPROCESSING
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace MoreMountains.Feedbacks
{
	[AddComponentMenu("More Mountains/Springs/MM Spring Vignette Center")]
	public class MMSpringVignetteCenter : MMSpringVector2Component<PostProcessVolume>
	{
		protected Vignette _vignette;

		public override Vector2 TargetVector2
		{
			get => _vignette.center;
			set => _vignette.center.Override(value);
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
