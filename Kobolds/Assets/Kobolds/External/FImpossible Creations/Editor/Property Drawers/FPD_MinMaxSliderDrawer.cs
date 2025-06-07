using UnityEditor;
using UnityEngine;

namespace FIMSpace.FEditor
{
	[CustomPropertyDrawer(typeof(FPD_MinMaxSliderAttribute))]
	public class FPropDrawers_MinMaxSlider : PropertyDrawer
	{
		private int adjustSwitcherValue = 60;

		public override void OnGUI(Rect rect, SerializedProperty property, GUIContent content)
		{
			var minMax = attribute as FPD_MinMaxSliderAttribute;

			if (property.propertyType == SerializedPropertyType.Vector2)
			{
				rect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);

				var minValue = property.vector2Value.x;
				var maxValue = property.vector2Value.y;

				var minRange = minMax.MinValue;
				var maxRange = minMax.MaxValue;

				EditorGUI.MinMaxSlider(rect, content, ref minValue, ref maxValue, minRange, maxRange);
				rect.y += EditorGUIUtility.singleLineHeight;

				var vec = new Vector2();
				vec.x = minValue;
				vec.y = maxValue;

				property.vector2Value = vec;

				float preAdjust = adjustSwitcherValue;

				adjustSwitcherValue = EditorGUI.IntField(rect, "Adjust Both: ", adjustSwitcherValue);
				if (adjustSwitcherValue < 1) adjustSwitcherValue = 1;
				if (adjustSwitcherValue > minMax.MaxValue) adjustSwitcherValue = (int) minMax.MaxValue;

				var val = new Vector2(minValue, maxValue);
				var preVal = val;

				rect.y += EditorGUIUtility.singleLineHeight;
				val = EditorGUI.Vector2Field(rect, "Range: ", val);

				if (adjustSwitcherValue != preAdjust)
					property.vector2Value = new Vector2(-adjustSwitcherValue, adjustSwitcherValue);

				if (val != preVal)
					property.vector2Value = new Vector2(val.x, val.y);
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var size = EditorGUIUtility.singleLineHeight;
			size += EditorGUIUtility.singleLineHeight * 3;

			return size;
		}
	}
}


[CustomPropertyDrawer(typeof(BackgroundColorAttribute))]
public class BackgroundColorDecorator : DecoratorDrawer
{
	private BackgroundColorAttribute Attribute => (BackgroundColorAttribute) attribute;

	public override float GetHeight()
	{
		return 0;
	}

	public override void OnGUI(Rect position)
	{
		GUI.backgroundColor = Attribute.Color;
	}
}
