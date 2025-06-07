using UnityEditor;
using UnityEngine;

namespace FIMSpace.FEditor
{
	[CustomPropertyDrawer(typeof(FPD_FoldableAttribute))]
	public class FPD_Foldable : PropertyDrawer
	{
		private SerializedProperty foldProp;
		private FPD_FoldableAttribute Attribute => (FPD_FoldableAttribute) attribute;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (string.IsNullOrEmpty(Attribute.FoldVariable) == false)
				if (foldProp == null)
					foldProp = property.serializedObject.FindProperty(Attribute.FoldVariable);

			if (foldProp == null)
			{
				EditorGUI.PropertyField(position, property, label);
			}
			else
			{
				if (foldProp.boolValue)
					EditorGUI.PropertyField(position, property, label);
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (string.IsNullOrEmpty(Attribute.FoldVariable) == false)
				if (foldProp == null)
					foldProp = property.serializedObject.FindProperty(Attribute.FoldVariable);

			if (foldProp == null)
				return base.GetPropertyHeight(property, label);
			if (foldProp.boolValue)
				return base.GetPropertyHeight(property, label);
			return 0;
		}
	}
}
