#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace FIMSpace.FEditor
{
	[CustomPropertyDrawer(typeof(FPD_HideOnBoolAttribute))]
	public class FPropDrawers_HideOnBool : PropertyDrawer
	{
		private FPD_HideOnBoolAttribute Attribute => (FPD_HideOnBoolAttribute) attribute;

		public override void OnGUI(Rect rect, SerializedProperty property, GUIContent content)
		{
			var enabled = IsEnabled(property);

			var wasEnabled = GUI.enabled;
			GUI.enabled = enabled;

			if (!Attribute.HideInInspector || enabled) EditorGUI.PropertyField(rect, property, content, true);

			GUI.enabled = wasEnabled;
		}

		private bool IsEnabled(SerializedProperty property)
		{
			bool enabled;
			var boolProp = property.serializedObject.FindProperty(Attribute.BoolVarName);

			if (boolProp == null) enabled = true;
			else enabled = boolProp.boolValue;

			return enabled;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var enabled = IsEnabled(property);

			if (!Attribute.HideInInspector || enabled) return EditorGUI.GetPropertyHeight(property, label);

			return -EditorGUIUtility.standardVerticalSpacing;
		}
	}
}


#endif
