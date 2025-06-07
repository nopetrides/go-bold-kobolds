using System.Collections.Generic;
using FIMSpace.Basics;
using FIMSpace.FEditor;
using UnityEditor;
using UnityEngine;

namespace FIMSpace.GroundFitter
{
	/// <summary>
	///     FM: Changing transform's orientation to fit to the ground, have controll over rotation in Y axis and possibility to
	///     move forward
	/// </summary>
	public class FGroundRotator : FGroundFitter_Base
	{
		[Tooltip("Root transform should be first object in the hierarchy of your movement game object")]
		public Transform RootTransform;

		public EFUpdateClock UpdateClock = EFUpdateClock.Update;

		private Quaternion initLocalRotation;
		private Vector3 mappingForward;

		private Vector3 mappingRight;
		private Vector3 mappingUp;

		protected override void Reset()
		{
			base.Reset();
			RelativeLookUp = false;
			RelativeLookUpBias = 0f;
			GlueToGround = false;
		}

		protected override void Start()
		{
			base.Start();
			initLocalRotation = TransformToRotate.localRotation;

			mappingForward = transform.InverseTransformDirection(RootTransform.forward);
			mappingUp = transform.InverseTransformDirection(RootTransform.up);
			mappingRight = transform.InverseTransformDirection(RootTransform.right);
		}

		private void Update()
		{
			if (UpdateClock == EFUpdateClock.FixedUpdate) return;
			TransformToRotate.localRotation = initLocalRotation;
		}

		private void FixedUpdate()
		{
			if (UpdateClock != EFUpdateClock.FixedUpdate) return;
			TransformToRotate.localRotation = initLocalRotation;
		}

		private void LateUpdate()
		{
			deltaTime = Time.deltaTime;
			FitToGround();
		}

		internal override void RotationCalculations()
		{
			targetRotationToApply = helperRotation;
			targetRotationToApply *= RootTransform.rotation;

			var toApplyEul = targetRotationToApply.eulerAngles;
			targetRotationToApply = Quaternion.Euler(
				Mathf.Clamp(FLogicMethods.WrapAngle(toApplyEul.x), -MaxForwardRotation, MaxForwardRotation) *
				(1 - MildForwardValue), toApplyEul.y,
				Mathf.Clamp(FLogicMethods.WrapAngle(toApplyEul.z), -MaxHorizontalRotation, MaxHorizontalRotation) *
				(1 - MildHorizontalValue));
			//UnityEngine.Debug.Log("helper eul = " + helperRotation.eulerAngles);
			//UnityEngine.Debug.DrawRay(TransformToRotate.position + Vector3.up, targetRotationToApply * Vector3.forward, Color.magenta, 1.01f);

			toApplyEul = targetRotationToApply.eulerAngles;
			toApplyEul = RootTransform.rotation.QToLocal(Quaternion.Euler(toApplyEul)).eulerAngles;

			var rot = TransformToRotate.rotation;
			if (toApplyEul.x != 0f) rot *= Quaternion.AngleAxis(toApplyEul.x, mappingRight);
			if (toApplyEul.y != 0f) rot *= Quaternion.AngleAxis(toApplyEul.y, mappingUp);
			if (toApplyEul.z != 0f) rot *= Quaternion.AngleAxis(toApplyEul.z, mappingForward);
			TransformToRotate.rotation = rot;
		}
	}

#region Inspector GUI

#if UNITY_EDITOR
	[CustomEditor(typeof(FGroundRotator))]
	[CanEditMultipleObjects]
	public class FGroundRotator_Editor : FGroundFitter_BaseEditor
	{
		public SerializedProperty sp_RootTransform;

		private void OnEnable()
		{
			sp_RootTransform = serializedObject.FindProperty("RootTransform");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			if (sp_RootTransform.objectReferenceValue == null)
				EditorGUILayout.HelpBox(
					"FGroundRotator should be attached to child transform of your character, for example to first skeleton bone!\nThen 'RootTransform' should be main game object of your character.",
					MessageType.Warning);

			GUILayout.BeginHorizontal();
			if (sp_RootTransform.objectReferenceValue == null) GUI.backgroundColor = Color.yellow;
			EditorGUILayout.PropertyField(sp_RootTransform);

			if (sp_RootTransform.objectReferenceValue == null)
			{
				GUILayout.Label(EditorGUIUtility.IconContent("console.warnicon.sml").image, GUILayout.Width(19));
				GUI.backgroundColor = Color.white;
			}
			else
			{
				var get = target as FGroundRotator;
				if (sp_RootTransform.objectReferenceValue == get.transform)
					GUILayout.Label(
						new GUIContent(
							EditorGUIUtility.IconContent("console.erroricon.sml").image,
							"Root transform must be parent of object to which you attach the 'FGroundRotator'"),
						GUILayout.Width(19));
			}

			GUILayout.EndHorizontal();

			FGUI_Inspector.DrawUILine(0.4f, 0.1f, 1, 5);


			var targetScript = (FGroundFitter_Base) target;
			var exclude = new List<string>();

			exclude.Add("drawDebug");
			exclude.Add("drawGizmo");
			exclude.Add("RelativeLookUp");
			exclude.Add("RelativeLookUpBias");
			exclude.Add("GlueToGround");
			exclude.Add("RootTransform");
			exclude.Add("TotalSmoother");

			if (targetScript.LookAheadRaycast == 0f) exclude.Add("AheadBlend");

			if (!targetScript.ZoneCast)
			{
				exclude.Add("ZoneCastDimensions");
				exclude.Add("ZoneCastOffset");
				exclude.Add("ZoneCastBias");
				exclude.Add("ZoneCastPrecision");
			}

			DrawPropertiesExcluding(serializedObject, exclude.ToArray());

			FGUI_Inspector.DrawUILine(0.4f, 0.1f, 1, 5);
			GUILayout.Space(5);
			serializedObject.ApplyModifiedProperties();
		}
	}
#endif

#endregion
}
