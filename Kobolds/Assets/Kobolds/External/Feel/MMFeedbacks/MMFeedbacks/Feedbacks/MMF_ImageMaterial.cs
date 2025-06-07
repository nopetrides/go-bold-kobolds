#if MM_UI
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UI;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	///     This feedback will let you change the material on a target UI Image
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("This feedback will let you change the material on a target UI Image")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[FeedbackPath("Renderer/Image Material")]
	public class MMF_ImageMaterial : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;

		protected Material _initialMaterial;

		/// the new material to apply to the target image
		[Tooltip("the new material to apply to the target image")]
		public Material NewMaterial;

		[MMFInspectorGroup("Image", true, 12, true)]
		/// the target Image we want to change the material on
		[Tooltip("the target Image we want to change the material on")]
		public Image TargetImage;

		public override bool HasAutomatedTargetAcquisition => true;

		protected override void AutomateTargetAcquisition()
		{
			TargetImage = FindAutomatedTarget<Image>();
		}

		/// <summary>
		///     On play we turn raycastTarget on or off
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized) return;

			if (TargetImage == null) return;

			_initialMaterial = TargetImage.material;
			TargetImage.material = NewMaterial;
		}

		/// <summary>
		///     On restore, we restore our initial state
		/// </summary>
		protected override void CustomRestoreInitialValues()
		{
			if (!Active || !FeedbackTypeAuthorized) return;
			TargetImage.material = _initialMaterial;
		}
#if UNITY_EDITOR
		public override Color FeedbackColor => MMFeedbacksInspectorColors.UIColor;

		public override bool EvaluateRequiresSetup()
		{
			return TargetImage == null;
		}

		public override string RequiredTargetText => TargetImage != null ? TargetImage.name : "";
		public override string RequiresSetupText =>
			"This feedback requires that a TargetImage be set to be able to work properly. You can set one below.";
#endif
	}
}
#endif
