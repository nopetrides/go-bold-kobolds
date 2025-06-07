using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	///     This feedback lets you control the size delta property (the size of this RectTransform relative to the distances
	///     between the anchors) of a RectTransform, over time
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp(
		"This feedback lets you control the size delta property (the size of this RectTransform relative to the distances between the anchors) of a RectTransform, over time")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks.MMTools")]
	[FeedbackPath("UI/RectTransformSizeDelta")]
	public class MMF_RectTransformSizeDelta : MMF_FeedbackBase
	{
		/// the value to remap the curve's 1 to, randomized between its min and max - put the same value in both min and max if you don't want any randomness
		[Tooltip(
			"the value to remap the curve's 1 to, randomized between its min and max - put the same value in both min and max if you don't want any randomness")]
		[MMFEnumCondition("Mode", (int) MMFeedbackBase.Modes.OverTime, (int) MMFeedbackBase.Modes.Instant)]
		[MMFVector("Min", "Max")]
		public Vector2 RemapOne = Vector2.one;

		/// the value to remap the curve's 0 to, randomized between its min and max - put the same value in both min and max if you don't want any randomness
		[Tooltip(
			"the value to remap the curve's 0 to, randomized between its min and max - put the same value in both min and max if you don't want any randomness")]
		[MMFEnumCondition("Mode", (int) MMFeedbackBase.Modes.OverTime)]
		[MMFVector("Min", "Max")]
		public Vector2 RemapZero = Vector2.zero;

		[MMFInspectorGroup("Size Delta", true, 38)]
		/// the speed at which we should animate the size delta
		[Tooltip("the speed at which we should animate the size delta")]
		[MMFEnumCondition("Mode", (int) MMFeedbackBase.Modes.OverTime)]
		public MMTweenType SpeedCurve = new(new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1)));

		[MMFInspectorGroup("Target RectTransform", true, 37, true)]
		/// the rect transform we want to impact
		[Tooltip("the rect transform we want to impact")]
		public RectTransform TargetRectTransform;

		public override bool HasAutomatedTargetAcquisition => true;
		public override bool CanForceInitialValue => true;

		protected override void AutomateTargetAcquisition()
		{
			TargetRectTransform = FindAutomatedTarget<RectTransform>();
		}

		protected override void FillTargets()
		{
			if (TargetRectTransform == null) return;

			var target = new MMF_FeedbackBaseTarget();
			var receiver = new MMPropertyReceiver();
			receiver.TargetObject = TargetRectTransform.gameObject;
			receiver.TargetComponent = TargetRectTransform;
			receiver.TargetPropertyName = "sizeDelta";
			receiver.RelativeValue = RelativeValues;
			receiver.Vector2RemapZero = RemapZero;
			receiver.Vector2RemapOne = RemapOne;
			target.Target = receiver;
			target.LevelCurve = SpeedCurve;
			target.RemapLevelZero = 0f;
			target.RemapLevelOne = 1f;
			target.InstantLevel = 1f;

			_targets.Add(target);
		}

		/// sets the inspector color for this feedback
#if UNITY_EDITOR
		public override Color FeedbackColor
		{
			get { return MMFeedbacksInspectorColors.UIColor; }
		}

		public override bool EvaluateRequiresSetup()
		{
			return TargetRectTransform == null;
		}

		public override string RequiredTargetText => TargetRectTransform != null ? TargetRectTransform.name : "";
		public override string RequiresSetupText =>
			"This feedback requires that a TargetRectTransform be set to be able to work properly. You can set one below.";
#endif
	}
}
