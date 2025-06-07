using UnityEditor;
using UnityEngine;

namespace FIMSpace.FEditor
{
	[CustomPropertyDrawer(typeof(FPD_DrawTextureAttribute))]
	public class FPD_DrawTexture : PropertyDrawer
	{
		private Texture2D asset;
		private FPD_DrawTextureAttribute Attribute => (FPD_DrawTextureAttribute) attribute;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (asset == null) asset = Resources.Load(Attribute.path, typeof(Texture2D)) as Texture2D;

			EditorGUILayout.BeginHorizontal();
			EditorGUIUtility.labelWidth = Attribute.labelWidth;

			if (Attribute.fieldWidth > 0)
				EditorGUILayout.PropertyField(property, true, GUILayout.Width(Attribute.fieldWidth));
			else
				EditorGUILayout.PropertyField(property, true);


			EditorGUILayout.LabelField(
				new GUIContent(asset), GUILayout.Width(Attribute.width), GUILayout.Height(Attribute.height));
			EditorGUILayout.EndHorizontal();

			EditorGUIUtility.labelWidth = 0;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return 0;
		}
	}
}
