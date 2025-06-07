using System.Collections;
using UnityEditor;
using UnityEngine;

namespace FIMSpace.Jiggling
{
	/// <summary>
	///     FM: Animating transform's rotation and scale to make it look kinda like jelly
	/// </summary>
	[AddComponentMenu("FImpossible Creations/Jiggling/FJiggling Simple - Bones")]
	public class FJiggling_SimpleBones : FJiggling_Simple
	{
		public bool NoRotationKeyframes;

		[Tooltip("If your animation is scaling bones set this to false")]
		public bool NoScaleKeyframes = true;

		[Tooltip("Toggle it to true if you animator is using 'Animated Physics' update mode")]
		public bool AnimatePhysics;

		private bool animatePhysicsWorking;

		private Quaternion initialKeyRotation;
		private Vector3 initialKeyScale;
		private bool triggerAnimatePhysics;

		protected override void Update()
		{
			// Erasing all actions made in Update() 
		}

		protected virtual void LateUpdate()
		{
			if (AnimatePhysics)
			{
				if (!animatePhysicsWorking) StartCoroutine(AnimatePhysicsClock());
				if (!triggerAnimatePhysics) return;
				triggerAnimatePhysics = false;
			}

			if (NoRotationKeyframes) TransformToAnimate.localRotation = initialKeyRotation;
			if (NoScaleKeyframes) TransformToAnimate.localScale = initialKeyScale;

			// Every beginning of late update rotations are the same as in animation played by Animator component
			initRotation = TransformToAnimate.localRotation; // initialKeyRotation;
			initScale = TransformToAnimate.localScale;

			// Doing update calculations in LateUpdate() to override Animator's work
			base.Update();
		}

		protected override void Init()
		{
			if (initialized) return;

			base.Init();
			initialKeyRotation = TransformToAnimate.localRotation;
			initialKeyScale = TransformToAnimate.localScale;
		}

		/// <summary>
		///     Support for 'animate physics' option inside unity's Animator
		/// </summary>
		private IEnumerator AnimatePhysicsClock()
		{
			animatePhysicsWorking = true;

			while (true)
			{
				yield return new WaitForFixedUpdate();
				triggerAnimatePhysics = true;
			}
		}
	}

#if UNITY_EDITOR
	/// <summary>
	///     FM: Editor class for Jiggle Bones component to check animation from editor level (in playmode)
	/// </summary>
	[CustomEditor(typeof(FJiggling_SimpleBones))]
	[CanEditMultipleObjects]
	public class FJiggling_SimpleBonesEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			var targetScript = (FJiggling_SimpleBones) target;
			DrawDefaultInspector();

			GUILayout.Space(10f);

			if (!Application.isPlaying) GUI.color = GUI.color.ChangeColorAlpha(0.45f);
			if (GUILayout.Button("Jiggle It"))
				if (Application.isPlaying) targetScript.StartJiggle();
				else Debug.Log("You must be in playmode to run this method!");
		}
	}
#endif
}
