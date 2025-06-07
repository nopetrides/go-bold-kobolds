#if MM_UI
using System.Collections;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	///     This feedback will let you control the texture scale of a target UI Image over time
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("This feedback will let you control the texture scale of a target UI Image over time.")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[FeedbackPath("UI/Image Texture Scale")]
	public class MMF_ImageTextureScale : MMF_Feedback
	{
		//
		public enum MaterialPropertyTypes
		{
			Main,
			TextureID
		}

		/// the possible modes for this feedback
		public enum Modes
		{
			OverTime,
			Instant
		}

		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;

		protected Coroutine _coroutine;

		protected Vector2 _initialValue;
		protected Material _material;
		protected Vector2 _newValue;

		/// if this is true, calling that feedback will trigger it, even if it's in progress. If it's false, it'll prevent any new Play until the current one is over
		[Tooltip(
			"if this is true, calling that feedback will trigger it, even if it's in progress. If it's false, it'll prevent any new Play until the current one is over")]
		public bool AllowAdditivePlays = false;

		/// how long the material should change over time
		[Tooltip("how long the material should change over time")]
		[MMFEnumCondition("Mode", (int) Modes.OverTime)]
		public float Duration = 0.2f;

		/// the value to move the intensity to in instant mode
		[Tooltip("the value to move the intensity to in instant mode")]
		[MMFEnumCondition("Mode", (int) Modes.Instant)]
		public Vector2 InstantScale;

		/// the property name, for example _MainTex_ST, or _MainTex if you don't have UseMaterialPropertyBlocks set to true
		[Tooltip(
			"the property name, for example _MainTex_ST, or _MainTex if you don't have UseMaterialPropertyBlocks set to true")]
		[MMEnumCondition("MaterialPropertyType", (int) MaterialPropertyTypes.TextureID)]
		public string MaterialPropertyName = "_MainTex_ST";

		/// whether to target the main texture property, or one specified in MaterialPropertyName
		[Tooltip("whether to target the main texture property, or one specified in MaterialPropertyName")]
		public MaterialPropertyTypes MaterialPropertyType = MaterialPropertyTypes.Main;

		/// whether the feedback should affect the material instantly or over a period of time
		[Tooltip("whether the feedback should affect the material instantly or over a period of time")]
		public Modes Mode = Modes.OverTime;

		/// whether or not the values should be relative
		[Tooltip("whether or not the values should be relative")]
		public bool RelativeValues = true;

		/// the value to remap the scale curve's 1 to, randomized between its min and max - put the same value in both min and max if you don't want any randomness
		[Tooltip(
			"the value to remap the scale curve's 1 to, randomized between its min and max - put the same value in both min and max if you don't want any randomness")]
		[MMFEnumCondition("Mode", (int) Modes.OverTime)]
		[MMFVector("Min", "Max")]
		public Vector2 RemapOne = Vector2.one;

		/// the value to remap the scale curve's 0 to, randomized between its min and max - put the same value in both min and max if you don't want any randomness
		[Tooltip(
			"the value to remap the scale curve's 0 to, randomized between its min and max - put the same value in both min and max if you don't want any randomness")]
		[MMFEnumCondition("Mode", (int) Modes.OverTime)]
		[MMFVector("Min", "Max")]
		public Vector2 RemapZero = Vector2.zero;

		[MMFInspectorGroup("Intensity", true, 65)]
		/// the curve to tween the scale on
		[Tooltip("the curve to tween the scale on")]
		[MMFEnumCondition("Mode", (int) Modes.OverTime)]
		public AnimationCurve ScaleCurve = new(new Keyframe(0, 0), new Keyframe(0.3f, 1f), new Keyframe(1, 0));

		[MMFInspectorGroup("Texture Scale", true, 63, true)]
		/// the UI Image on which to change texture scale on
		[Tooltip("the UI Image on which to change texture scale on")]
		public Image TargetImage;

		public override bool HasRandomness => true;
		public override bool HasAutomatedTargetAcquisition => true;

		/// the duration of this feedback is the duration of the transition
		public override float FeedbackDuration
		{
			get => Mode == Modes.Instant ? 0f : ApplyTimeMultiplier(Duration);
			set => Duration = value;
		}

		protected override void AutomateTargetAcquisition()
		{
			TargetImage = FindAutomatedTarget<Image>();
		}

		/// <summary>
		///     On init we store our initial texture scale
		/// </summary>
		/// <param name="owner"></param>
		protected override void CustomInitialization(MMF_Player owner)
		{
			base.CustomInitialization(owner);

			if (TargetImage == null)
			{
				Debug.LogWarning(
					"[Image Texture Scale Feedback] The image texture scale feedback on " + Owner.name +
					" doesn't have a TargetImage, it won't work. You need to specify an Image in its inspector.");
				return;
			}

			_material = TargetImage.material;

			switch (MaterialPropertyType)
			{
				case MaterialPropertyTypes.Main:
					_initialValue = _material.mainTextureScale;
					break;
				case MaterialPropertyTypes.TextureID:
					_initialValue = _material.GetTextureScale(MaterialPropertyName);
					break;
			}
		}

		/// <summary>
		///     On Play we initiate our scale change
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized || TargetImage == null) return;

			var intensityMultiplier = ComputeIntensity(feedbacksIntensity, position);

			switch (Mode)
			{
				case Modes.Instant:
					if (NormalPlayDirection)
						ApplyValue(InstantScale * intensityMultiplier);
					else
						ApplyValue(_initialValue);
					break;
				case Modes.OverTime:
					if (!AllowAdditivePlays && _coroutine != null) return;
					if (_coroutine != null) Owner.StopCoroutine(_coroutine);
					_coroutine = Owner.StartCoroutine(TransitionCo(intensityMultiplier));
					break;
			}
		}

		/// <summary>
		///     This coroutine will modify the scale value over time
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator TransitionCo(float intensityMultiplier)
		{
			var journey = NormalPlayDirection ? 0f : FeedbackDuration;
			IsPlaying = true;
			while (journey >= 0 && journey <= FeedbackDuration && FeedbackDuration > 0)
			{
				var remappedTime = MMFeedbacksHelpers.Remap(journey, 0f, FeedbackDuration, 0f, 1f);

				SetMaterialValues(remappedTime, intensityMultiplier);

				journey += NormalPlayDirection ? FeedbackDeltaTime : -FeedbackDeltaTime;
				yield return null;
			}

			SetMaterialValues(FinalNormalizedTime, intensityMultiplier);
			IsPlaying = false;
			_coroutine = null;
			yield return null;
		}

		/// <summary>
		///     Applies scale to the target material
		/// </summary>
		/// <param name="time"></param>
		protected virtual void SetMaterialValues(float time, float intensityMultiplier)
		{
			_newValue.x = MMFeedbacksHelpers.Remap(ScaleCurve.Evaluate(time), 0f, 1f, RemapZero.x, RemapOne.x);
			_newValue.y = MMFeedbacksHelpers.Remap(ScaleCurve.Evaluate(time), 0f, 1f, RemapZero.y, RemapOne.y);

			if (RelativeValues) _newValue += _initialValue;

			ApplyValue(_newValue * intensityMultiplier);
		}

		/// <summary>
		///     Applies the specified value to the material
		/// </summary>
		/// <param name="newValue"></param>
		protected virtual void ApplyValue(Vector2 newValue)
		{
			switch (MaterialPropertyType)
			{
				case MaterialPropertyTypes.Main:
					_material.mainTextureScale = newValue;
					break;
				case MaterialPropertyTypes.TextureID:
					_material.SetTextureScale(MaterialPropertyName, newValue);
					break;
			}
		}

		/// <summary>
		///     Stops this feedback
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1)
		{
			if (!Active || !FeedbackTypeAuthorized || _coroutine == null) return;
			base.CustomStopFeedback(position, feedbacksIntensity);
			IsPlaying = false;
			Owner.StopCoroutine(_coroutine);
			_coroutine = null;
		}

		/// sets the inspector color for this feedback
#if UNITY_EDITOR
		public override Color FeedbackColor
		{
			get { return MMFeedbacksInspectorColors.UIColor; }
		}

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
