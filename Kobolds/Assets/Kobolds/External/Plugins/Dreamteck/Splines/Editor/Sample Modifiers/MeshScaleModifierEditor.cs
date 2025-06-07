using UnityEditor;
using UnityEngine;

namespace Dreamteck.Splines.Editor
{
	public class MeshScaleModifierEditor : SplineSampleModifierEditor
	{
		private readonly float addTime = 0f;
		public bool allowSelection = true;

		public MeshScaleModifierEditor(MeshGenerator user, SplineUserEditor editor, int channelIndex) : base(
			user, editor, "_channels/[" + channelIndex + "]/_scaleModifier")
		{
			title = "Scale Modifiers";
		}

		public void ClearSelection()
		{
			selected = -1;
		}

		public override void DrawInspector()
		{
			base.DrawInspector();
			if (!isOpen) return;
			if (GUILayout.Button("Add New Scale"))
			{
				var key = AddKey(addTime - 0.1f, addTime + 0.1f);
				key.FindPropertyRelative("scale").vector3Value = Vector3.one;
				UpdateValues();
			}
		}

		protected override void KeyGUI(SerializedProperty key)
		{
			var scale = key.FindPropertyRelative("scale");
			base.KeyGUI(key);
			EditorGUILayout.PropertyField(scale);
		}
	}
}
