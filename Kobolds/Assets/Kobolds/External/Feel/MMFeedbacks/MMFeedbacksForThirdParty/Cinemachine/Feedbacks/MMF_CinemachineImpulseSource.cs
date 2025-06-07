#if MM_CINEMACHINE
using Cinemachine;
#elif MM_CINEMACHINE3
using Unity.Cinemachine;
#endif
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.FeedbacksForThirdParty
{
	[AddComponentMenu("")]
#if MM_CINEMACHINE || MM_CINEMACHINE3
	[FeedbackPath("Camera/Cinemachine Impulse Source")]
#endif
	[MovedFrom(false, null, "MoreMountains.Feedbacks.Cinemachine")]
	[FeedbackHelp(
		"This feedback lets you generate an impulse on a Cinemachine Impulse source. You'll need a Cinemachine Impulse Listener on your camera for this to work.")]
	public class MMF_CinemachineImpulseSource : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;

		/// whether or not to clear impulses (stopping camera shakes) when the Stop method is called on that feedback
		[Tooltip(
			"whether or not to clear impulses (stopping camera shakes) when the Stop method is called on that feedback")]
		public bool ClearImpulseOnStop = false;


		[MMFInspectorGroup("Cinemachine Impulse Source", true, 28)]
		/// the velocity to apply to the impulse shake
		[Tooltip("the velocity to apply to the impulse shake")]
		public Vector3 Velocity = new(1f, 1f, 1f);

		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized) return;

#if MM_CINEMACHINE || MM_CINEMACHINE3
			if (ImpulseSource != null) ImpulseSource.GenerateImpulse(Velocity);
#endif
		}

		/// <summary>
		///     Stops the animation if needed
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1)
		{
			if (!Active || !FeedbackTypeAuthorized || !ClearImpulseOnStop) return;
			base.CustomStopFeedback(position, feedbacksIntensity);

#if MM_CINEMACHINE || MM_CINEMACHINE3
			CinemachineImpulseManager.Instance.Clear();
#endif
		}

		/// <summary>
		///     On restore, we put our object back at its initial position
		/// </summary>
		protected override void CustomRestoreInitialValues()
		{
			if (!Active || !FeedbackTypeAuthorized) return;

#if MM_CINEMACHINE || MM_CINEMACHINE3
			CinemachineImpulseManager.Instance.Clear();
#endif
		}

		/// sets the inspector color for this feedback
#if UNITY_EDITOR
		public override Color FeedbackColor
		{
			get { return MMFeedbacksInspectorColors.CameraColor; }
		}
#if MM_CINEMACHINE || MM_CINEMACHINE3
		public override bool EvaluateRequiresSetup()
		{
			return ImpulseSource == null;
		}

		public override string RequiredTargetText => ImpulseSource != null ? ImpulseSource.name : "";
#endif
		public override string RequiresSetupText =>
			"This feedback requires that an ImpulseSource be set to be able to work properly. You can set one below.";
#endif
#if MM_CINEMACHINE || MM_CINEMACHINE3
		/// the impulse definition to broadcast
		[Tooltip("the impulse definition to broadcast")]
		public CinemachineImpulseSource ImpulseSource;

		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition()
		{
			ImpulseSource = FindAutomatedTarget<CinemachineImpulseSource>();
		}
#endif
	}
}
