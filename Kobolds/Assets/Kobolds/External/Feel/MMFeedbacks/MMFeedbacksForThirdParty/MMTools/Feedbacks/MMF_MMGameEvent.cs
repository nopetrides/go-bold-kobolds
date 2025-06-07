using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	///     This feedback will trigger a MMGameEvent of the specified name when played
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("This feedback will trigger a MMGameEvent of the specified name when played")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks.MMTools")]
	[FeedbackPath("Events/MMGameEvent")]
	public class MMF_MMGameEvent : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;

		public bool BoolParameter;

		[MMFInspectorGroup("Optional Payload", true, 58, true)]
		public int IntParameter;

		[MMFInspectorGroup("MMGameEvent", true, 57, true)]
		public string MMGameEventName;

		public string StringParameter;
		public Vector2 Vector2Parameter;
		public Vector3 Vector3Parameter;

		/// <summary>
		///     On Play we change the values of our fog
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized) return;
			MMGameEvent.Trigger(
				MMGameEventName, IntParameter, Vector2Parameter, Vector3Parameter, BoolParameter, StringParameter);
		}

		/// sets the inspector color for this feedback
#if UNITY_EDITOR
		public override Color FeedbackColor
		{
			get { return MMFeedbacksInspectorColors.EventsColor; }
		}

		public override bool EvaluateRequiresSetup()
		{
			return MMGameEventName == "";
		}

		public override string RequiredTargetText => MMGameEventName;
		public override string RequiresSetupText => "This feedback requires that you specify a MMGameEventName below.";
#endif
	}
}
