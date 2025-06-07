using UnityEditor;
using UnityEngine;

namespace Dreamteck.Splines.Editor
{
	public class RotationModifierEditor : SplineSampleModifierEditor
	{
		private readonly float addTime = 0f;
		public bool allowSelection = true;

		public RotationModifierEditor(SplineUser user, SplineUserEditor parent) : base(
			user, parent, "_rotationModifier")
		{
			title = "Rotation Modifiers";
		}

		public void ClearSelection()
		{
			selected = -1;
		}

		public override void DrawInspector()
		{
			base.DrawInspector();
			if (!isOpen) return;
			if (GUILayout.Button("Add New Rotation"))
			{
				AddKey(addTime - 0.1f, addTime + 0.1f);
				UpdateValues();
			}
		}

		protected override void KeyGUI(SerializedProperty key)
		{
			var rotation = key.FindPropertyRelative("rotation");
			var target = key.FindPropertyRelative("target");
			var useLookTarget = key.FindPropertyRelative("useLookTarget");
			base.KeyGUI(key);
			if (!useLookTarget.boolValue) EditorGUILayout.PropertyField(rotation);
			EditorGUILayout.PropertyField(useLookTarget);
			if (useLookTarget.boolValue) EditorGUILayout.PropertyField(target);
		}

		protected override bool KeyHandles(SerializedProperty key, bool edit)
		{
			if (!isOpen) return false;
			var changed = false;
			var start = key.FindPropertyRelative("_featherStart");
			var end = key.FindPropertyRelative("_featherEnd");
			var centerStart = key.FindPropertyRelative("_centerStart");
			var centerEnd = key.FindPropertyRelative("_centerEnd");
			var rotation = key.FindPropertyRelative("rotation");
			var target = key.FindPropertyRelative("target");
			var useLookTarget = key.FindPropertyRelative("useLookTarget");
			var position = GetPosition(start.floatValue, end.floatValue, centerStart.floatValue, centerEnd.floatValue);
			var result = new SplineSample();
			user.spline.Evaluate(position, ref result);
			if (useLookTarget.boolValue)
			{
				if (target.objectReferenceValue != null)
				{
					var targetTransform = (Transform) target.objectReferenceValue;
					Handles.DrawDottedLine(result.position, targetTransform.position, 5f);
					if (edit)
					{
						var lastPos = targetTransform.position;
						targetTransform.position = Handles.PositionHandle(
							targetTransform.position, Quaternion.identity);
						if (lastPos != targetTransform.position)
						{
							MainPointModule.HoldInteraction();
							EditorUtility.SetDirty(targetTransform);
							changed = true;
						}
					}
				}
			}
			else
			{
				var directionRot = Quaternion.LookRotation(result.forward, result.up);
				var rot = directionRot * Quaternion.Euler(rotation.vector3Value);
				SplineEditorHandles.DrawArrowCap(result.position, rot, HandleUtility.GetHandleSize(result.position));

				if (edit)
				{
					var lastEuler = rot.eulerAngles;
					rot = Handles.RotationHandle(rot, result.position);
					rot = Quaternion.Inverse(directionRot) * rot;
					rotation.vector3Value = rot.eulerAngles;
					if (rot.eulerAngles != lastEuler)
					{
						MainPointModule.HoldInteraction();
						changed = true;
					}
				}
			}

			return base.KeyHandles(key, edit) || changed;
		}
	}
}
