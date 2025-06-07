using UnityEditor;
using UnityEditor.UI;
using UnityEngine.UI;

namespace TheraBytes.BetterUi.Editor
{
	[CustomEditor(typeof(BetterSlider))] [CanEditMultipleObjects]
	public class BetterSliderEditor : SliderEditor
	{
		private readonly BetterElementHelper<Slider, BetterSlider> helper = new();

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			var obj = target as BetterSlider;
			helper.DrawGui(serializedObject);

			serializedObject.ApplyModifiedProperties();
		}

		[MenuItem("CONTEXT/Slider/♠ Make Better")]
		public static void MakeBetter(MenuCommand command)
		{
			var obj = command.context as Slider;
			Betterizer.MakeBetter<Slider, BetterSlider>(obj);
		}
	}
}
