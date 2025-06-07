using System;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	///     Describes a blink phase, defined by a duration for the phase, and the time it should remain inactive and active,
	///     sequentially
	///     For the duration of the phase, the object will be off for OffDuration, then on for OnDuration, then off again for
	///     OffDuration, etc
	///     If you want a grenade to blink briefly every .2 seconds, for 1 second, these parameters are what you're after :
	///     PhaseDuration = 1f;
	///     OffDuration = 0.2f;
	///     OnDuration = 0.1f;
	/// </summary>
	[Serializable]
	public class BlinkPhase
	{
		/// the duration of that specific phase, in seconds
		public float PhaseDuration = 1f;

		/// the time the object should remain off
		public float OffDuration = 0.2f;

		/// the time the object should then remain on
		public float OnDuration = 0.1f;

		/// the speed at which to lerp to off state
		public float OffLerpDuration = 0.05f;

		/// the speed at which to lerp to on state
		public float OnLerpDuration = 0.05f;
	}

	[Serializable]
	public class BlinkTargetRenderer
	{
		public Renderer TargetRenderer;
		public int TargetMaterialIndex;
	}

	/// <summary>
	///     Add this class to a GameObject to make it blink, either by enabling/disabling a gameobject, changing its alpha,
	///     emission intensity, or a value on a shader)
	/// </summary>
	[AddComponentMenu("More Mountains/Feedbacks/Shakers/Various/MM Blink")]
	public class MMBlink : MMMonoBehaviour
	{
		/// the possible methods to blink an object
		public enum Methods
		{
			SetGameObjectActive,
			MaterialAlpha,
			MaterialEmissionIntensity,
			ShaderFloatValue
		}

		/// the possible states of the blinking object
		public enum States
		{
			On,
			Off
		}

		[MMInspectorGroup("Blink Method", true, 17)]
		/// the selected method to blink the target object
		[Tooltip("the selected method to blink the target object")]
		public Methods Method = Methods.SetGameObjectActive;

		/// the object to set active/inactive if that method was chosen
		[Tooltip("the object to set active/inactive if that method was chosen")]
		[MMFEnumCondition("Method", (int) Methods.SetGameObjectActive)]
		public GameObject TargetGameObject;

		/// the target renderer to work with
		[Tooltip("the target renderer to work with")]
		[MMFEnumCondition(
			"Method", (int) Methods.MaterialAlpha, (int) Methods.MaterialEmissionIntensity,
			(int) Methods.ShaderFloatValue)]
		public Renderer TargetRenderer;

		/// the material index to target
		[Tooltip("the material index to target")]
		[MMFEnumCondition(
			"Method", (int) Methods.MaterialAlpha, (int) Methods.MaterialEmissionIntensity,
			(int) Methods.ShaderFloatValue)]
		public int MaterialIndex;

		/// the shader property to alter a float on
		[Tooltip("the shader property to alter a float on")]
		[MMFEnumCondition(
			"Method", (int) Methods.MaterialAlpha, (int) Methods.MaterialEmissionIntensity,
			(int) Methods.ShaderFloatValue)]
		public string ShaderPropertyName = "_Color";

		/// the value to apply when blinking is off
		[Tooltip("the value to apply when blinking is off")]
		[MMFEnumCondition(
			"Method", (int) Methods.MaterialAlpha, (int) Methods.MaterialEmissionIntensity,
			(int) Methods.ShaderFloatValue)]
		public float OffValue;

		/// the value to apply when blinking is on
		[Tooltip("the value to apply when blinking is on")]
		[MMFEnumCondition(
			"Method", (int) Methods.MaterialAlpha, (int) Methods.MaterialEmissionIntensity,
			(int) Methods.ShaderFloatValue)]
		public float OnValue = 1f;

		/// whether to lerp these values or not
		[Tooltip("whether to lerp these values or not")]
		[MMFEnumCondition(
			"Method", (int) Methods.MaterialAlpha, (int) Methods.MaterialEmissionIntensity,
			(int) Methods.ShaderFloatValue)]
		public bool LerpValue = true;

		/// the curve to apply to the lerping
		[Tooltip("the curve to apply to the lerping")]
		[MMFEnumCondition(
			"Method", (int) Methods.MaterialAlpha, (int) Methods.MaterialEmissionIntensity,
			(int) Methods.ShaderFloatValue)]
		public AnimationCurve Curve = new(new Keyframe(0, 0), new Keyframe(0.3f, 1.05f), new Keyframe(1, 0));

		/// if this is true, this component will use material property blocks instead of working on an instance of the material.
		[Tooltip(
			"if this is true, this component will use material property blocks instead of working on an instance of the material.")]
		public bool UseMaterialPropertyBlocks;

		[MMInspectorGroup("Extra Targets", true, 12)]
		/// a list of optional extra renderers and their material index to target
		[Tooltip("a list of optional extra renderers and their material index to target")]
		public List<BlinkTargetRenderer> ExtraRenderers;

		/// a list of optional extra game objects to target
		[Tooltip("a list of optional extra game objects to target")]
		public List<GameObject> ExtraGameObjects;

		[MMInspectorGroup("State", true, 18)]
		/// whether the object should blink or not
		[Tooltip("whether the object should blink or not")]
		public bool Blinking = true;

		/// whether or not to force a certain state on exit
		[Tooltip("whether or not to force a certain state on exit")]
		public bool ForceStateOnExit;

		/// the state to apply on exit
		[Tooltip("the state to apply on exit")]
		[MMFCondition("ForceStateOnExit", true)]
		public States StateOnExit = States.On;

		[MMInspectorGroup("TimeScale", true, 120)]
		/// whether or not this MMBlink should operate on unscaled time 
		[Tooltip("whether or not this MMBlink should operate on unscaled time")]
		public TimescaleModes TimescaleMode = TimescaleModes.Scaled;

		[MMInspectorGroup("Sequence", true, 121)]
		/// how many times the sequence should repeat (-1 : infinite)
		[Tooltip("how many times the sequence should repeat (-1 : infinite)")]
		public int RepeatCount;

		/// The list of phases to apply blinking with
		[Tooltip("The list of phases to apply blinking with")]
		public List<BlinkPhase> Phases;

		[MMInspectorGroup("Debug", true, 122)]
		[MMInspectorButtonBar(
			new[] {"ToggleBlinking", "StartBlinking", "StopBlinking"},
			new[] {"ToggleBlinking", "StartBlinking", "StopBlinking"},
			new[] {true, true, true},
			new[] {"main-call-to-action", "", ""})]
		public bool DebugToolbar;

		/// is the blinking object in an active state right now?
		[Tooltip("is the blinking object in an active state right now?")]
		[MMFReadOnly]
		public bool Active;

		/// the index of the phase we're currently in
		[Tooltip("the index of the phase we're currently in")]
		[MMFReadOnly]
		public int CurrentPhaseIndex;

		protected float _currentBlinkDuration;
		protected Color _currentColor;
		protected float _currentLerpDuration;
		protected float _currentPhaseStartedAt;
		protected List<Color> _extraInitialColors;
		protected List<MaterialPropertyBlock> _extraPropertyBlocks;
		protected Color _initialColor;
		protected float _initialShaderFloatValue;

		protected float _lastBlinkAt;
		protected MaterialPropertyBlock _propertyBlock;
		protected int _propertyID;
		protected int _repeatCount;

		/// <summary>
		///     The duration of the blink is the sum of its phases' durations, plus the time it takes to repeat them all
		/// </summary>
		public virtual float Duration
		{
			get
			{
				if (RepeatCount < 0
					|| Phases.Count == 0)
					return 0f;

				var totalDuration = 0f;
				foreach (var phase in Phases) totalDuration += phase.PhaseDuration;
				return totalDuration + totalDuration * RepeatCount;
			}
		}

		/// <summary>
		///     On Update, we blink if we are supposed to
		/// </summary>
		protected virtual void Update()
		{
			DetermineState();

			if (!Blinking) return;

			Blink();
		}

		/// <summary>
		///     On enable, initializes blink properties
		/// </summary>
		protected virtual void OnEnable()
		{
			InitializeBlinkProperties();
		}

		protected void OnDisable()
		{
			if (ForceStateOnExit)
			{
				if (StateOnExit == States.Off)
					ApplyBlink(false, 0f);
				else
					ApplyBlink(true, 1f);
			}
		}


		public virtual float GetTime()
		{
			return TimescaleMode == TimescaleModes.Scaled ? Time.time : Time.unscaledTime;
		}

		public virtual float GetDeltaTime()
		{
			return TimescaleMode == TimescaleModes.Scaled ? Time.deltaTime : Time.unscaledDeltaTime;
		}

		/// <summary>
		///     Makes the object blink if it wasn't already blinking, stops it otherwise
		/// </summary>
		public virtual void ToggleBlinking()
		{
			Blinking = !Blinking;
			ResetBlinkProperties();
		}

		/// <summary>
		///     Makes the object start blinking
		/// </summary>
		public virtual void StartBlinking()
		{
			enabled = true;
			Blinking = true;
			ResetBlinkProperties();
		}

		/// <summary>
		///     Makes the object stop blinking
		/// </summary>
		public virtual void StopBlinking()
		{
			Blinking = false;
			ResetBlinkProperties();
		}

		/// <summary>
		///     Determines the current phase and determines whether the object should be active or inactive
		/// </summary>
		protected virtual void DetermineState()
		{
			DetermineCurrentPhase();

			if (!Blinking) return;

			if (Active)
			{
				if (GetTime() - _lastBlinkAt > Phases[CurrentPhaseIndex].OnDuration)
				{
					Active = false;
					_lastBlinkAt = GetTime();
				}
			}
			else
			{
				if (GetTime() - _lastBlinkAt > Phases[CurrentPhaseIndex].OffDuration)
				{
					Active = true;
					_lastBlinkAt = GetTime();
				}
			}

			_currentBlinkDuration =
				Active ? Phases[CurrentPhaseIndex].OnDuration : Phases[CurrentPhaseIndex].OffDuration;
			_currentLerpDuration =
				Active ? Phases[CurrentPhaseIndex].OnLerpDuration : Phases[CurrentPhaseIndex].OffLerpDuration;
		}

		/// <summary>
		///     Blinks the object based on its computed state
		/// </summary>
		protected virtual void Blink()
		{
			var currentValue = _currentColor.a;
			var initialValue = Active ? OffValue : OnValue;
			var targetValue = Active ? OnValue : OffValue;
			var newValue = targetValue;

			if (LerpValue && GetTime() - _lastBlinkAt < _currentLerpDuration)
			{
				var t = MMFeedbacksHelpers.Remap(GetTime() - _lastBlinkAt, 0f, _currentLerpDuration, 0f, 1f);
				newValue = Curve.Evaluate(t);
				newValue = MMFeedbacksHelpers.Remap(newValue, 0f, 1f, initialValue, targetValue);
			}
			else
			{
				newValue = targetValue;
			}

			ApplyBlink(Active, newValue);
		}

		/// <summary>
		///     Applies the blink to the object based on its type
		/// </summary>
		/// <param name="active"></param>
		/// <param name="value"></param>
		protected virtual void ApplyBlink(bool active, float value)
		{
			switch (Method)
			{
				case Methods.SetGameObjectActive:
					TargetGameObject.SetActive(active);
					foreach (var go in ExtraGameObjects) go.SetActive(active);
					break;
				case Methods.MaterialAlpha:
					_currentColor.a = value;
					ApplyCurrentColor(TargetRenderer);
					for (var index = 0; index < ExtraRenderers.Count; index++)
					{
						var blinkRenderer = ExtraRenderers[index];
						ApplyCurrentColor(blinkRenderer.TargetRenderer);
					}

					break;
				case Methods.MaterialEmissionIntensity:
					_currentColor = _initialColor * value;
					ApplyCurrentColor(TargetRenderer);
					for (var index = 0; index < ExtraRenderers.Count; index++)
					{
						var blinkRenderer = ExtraRenderers[index];
						ApplyCurrentColor(blinkRenderer.TargetRenderer);
					}

					break;
				case Methods.ShaderFloatValue:
					ApplyFloatValue(TargetRenderer, value);
					for (var index = 0; index < ExtraRenderers.Count; index++)
					{
						var blinkRenderer = ExtraRenderers[index];
						ApplyFloatValue(blinkRenderer.TargetRenderer, value);
					}

					break;
			}
		}

		protected virtual void ApplyFloatValue(Renderer targetRenderer, float value)
		{
			if (UseMaterialPropertyBlocks)
			{
				targetRenderer.GetPropertyBlock(_propertyBlock, MaterialIndex);
				_propertyBlock.SetFloat(_propertyID, value);
				targetRenderer.SetPropertyBlock(_propertyBlock);
			}
			else
			{
				targetRenderer.materials[MaterialIndex].SetFloat(_propertyID, value);
			}
		}

		protected virtual void ApplyCurrentColor(Renderer targetRenderer)
		{
			if (UseMaterialPropertyBlocks)
			{
				targetRenderer.GetPropertyBlock(_propertyBlock, MaterialIndex);
				_propertyBlock.SetColor(_propertyID, _currentColor);
				targetRenderer.SetPropertyBlock(_propertyBlock);
			}
			else
			{
				targetRenderer.materials[MaterialIndex].SetColor(_propertyID, _currentColor);
			}
		}

		/// <summary>
		///     Determines the current phase index based on phase durations
		/// </summary>
		protected virtual void DetermineCurrentPhase()
		{
			// if the phase duration is null or less, we'll be in that phase forever, and return
			if (Phases[CurrentPhaseIndex].PhaseDuration <= 0) return;
			// if the phase's duration is elapsed, we move to the next phase
			if (GetTime() - _currentPhaseStartedAt > Phases[CurrentPhaseIndex].PhaseDuration)
			{
				CurrentPhaseIndex++;
				_currentPhaseStartedAt = GetTime();
			}

			if (CurrentPhaseIndex > Phases.Count - 1)
			{
				CurrentPhaseIndex = 0;
				if (RepeatCount != -1)
				{
					_repeatCount--;
					if (_repeatCount < 0)
					{
						ResetBlinkProperties();

						if (ForceStateOnExit)
						{
							if (StateOnExit == States.Off)
								ApplyBlink(false, 0f);
							else
								ApplyBlink(true, 1f);
						}

						Blinking = false;
					}
				}
			}
		}

		/// <summary>
		///     Resets counters and grabs properties and initial colors
		/// </summary>
		protected virtual void InitializeBlinkProperties()
		{
			if (Phases.Count == 0)
			{
				Debug.LogError("MMBlink : You need to define at least one phase for this component to work.");
				enabled = false;
				return;
			}

			_currentPhaseStartedAt = GetTime();
			CurrentPhaseIndex = 0;
			_repeatCount = RepeatCount;
			_propertyBlock = new MaterialPropertyBlock();

			switch (Method)
			{
				case Methods.MaterialAlpha:
					GetInitialColor();
					break;
				case Methods.MaterialEmissionIntensity:
					GetInitialColor();
					break;
				case Methods.ShaderFloatValue:
					GetInitialFloatValue();
					break;
			}
		}

		protected virtual void GetInitialColor()
		{
			TargetRenderer.GetPropertyBlock(_propertyBlock, MaterialIndex);
			_propertyID = Shader.PropertyToID(ShaderPropertyName);
			_initialColor = UseMaterialPropertyBlocks ?
				TargetRenderer.sharedMaterials[MaterialIndex].GetColor(_propertyID) :
				TargetRenderer.materials[MaterialIndex].GetColor(_propertyID);
			_currentColor = _initialColor;
		}

		protected virtual void GetInitialFloatValue()
		{
			TargetRenderer.GetPropertyBlock(_propertyBlock, MaterialIndex);
			_propertyID = Shader.PropertyToID(ShaderPropertyName);
			_initialShaderFloatValue = UseMaterialPropertyBlocks ?
				TargetRenderer.sharedMaterials[MaterialIndex].GetFloat(_propertyID) :
				TargetRenderer.materials[MaterialIndex].GetFloat(_propertyID);
		}

		/// <summary>
		///     Resets blinking properties to original values
		/// </summary>
		protected virtual void ResetBlinkProperties()
		{
			_currentPhaseStartedAt = GetTime();
			CurrentPhaseIndex = 0;
			_repeatCount = RepeatCount;

			var value = 1f;
			if (Method == Methods.ShaderFloatValue) value = _initialShaderFloatValue;
			ApplyBlink(false, value);
		}
	}
}
