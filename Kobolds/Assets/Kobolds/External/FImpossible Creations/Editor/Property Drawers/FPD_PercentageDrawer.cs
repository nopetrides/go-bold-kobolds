using UnityEditor;
using UnityEngine;

namespace FIMSpace.FEditor
{
	[CustomPropertyDrawer(typeof(FPD_PercentageAttribute))]
	public class FPD_Percentage : PropertyDrawer
	{
		private FPD_PercentageAttribute Attribute => (FPD_PercentageAttribute) attribute;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			label = EditorGUI.BeginProperty(position, label, property);
			position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

			var sliderVal = property.floatValue;
			//sliderVal = EditorGUI.Slider(position, GUIContent.none, property.floatValue, Attribute.Min, Attribute.Max);

			var fieldSize = 48f;
			var percField = new Rect(
				position.x + position.width - fieldSize + 5, position.y, fieldSize, position.height);
			var floatField = position;

			var editable = Attribute.editableValue;
			if (GUI.enabled == false) editable = false;

			if (editable)
			{
				floatField = new Rect(
					position.x + position.width - fieldSize + 2, position.y, fieldSize - 18, position.height);
				percField.position = new Vector2(position.x + position.width - 14, percField.position.y);
				percField.width = 18;
			}

			position.width -= fieldSize + 3;

			//float preVal = property.floatValue;

			sliderVal = GUI.HorizontalSlider(position, property.floatValue, Attribute.Min, Attribute.Max);

			//if (preVal != sliderVal) property.serializedObject.ApplyModifiedProperties();

			if (Attribute.basic)
			{
				var pre = sliderVal;
				var value = EditorGUI.FloatField(floatField, sliderVal);
				if (value != pre) sliderVal = value;

				EditorGUI.LabelField(percField, Attribute.Suffix);
			}
			else if (Attribute.from0to100)
			{
				if (!editable)
				{
					EditorGUI.LabelField(percField, Mathf.Round(sliderVal / Attribute.Max * 100f) + Attribute.Suffix);
				}
				else
				{
					var pre = Mathf.Round(sliderVal / Attribute.Max * 100f);
					var value = EditorGUI.FloatField(floatField, Mathf.Round(sliderVal / Attribute.Max * 100f));
					if (value != pre) sliderVal = value / 100f;

					EditorGUI.LabelField(percField, Attribute.Suffix);
				}
			}
			else
			{
				if (!editable)
				{
					EditorGUI.LabelField(percField, Mathf.Round(sliderVal * 100f) + "%");
				}
				else
				{
					var pre = Mathf.Round(sliderVal * 100f);
					var value = EditorGUI.FloatField(floatField, Mathf.Round(sliderVal * 100f));
					if (value != pre) sliderVal = value / 100f;

					EditorGUI.LabelField(percField, Attribute.Suffix);
				}
			}

			property.floatValue = sliderVal;

			//sliderVal = EditorGUILayout.Slider(property.floatValue, Attribute.Min, Attribute.Max,);

			EditorGUI.EndProperty();
		}
	}
}
