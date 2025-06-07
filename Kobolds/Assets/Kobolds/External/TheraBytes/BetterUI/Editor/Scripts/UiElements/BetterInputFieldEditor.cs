using TheraBytes.BetterUi.Editor.ThirdParty;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine.UI;

namespace TheraBytes.BetterUi.Editor
{
	[CustomEditor(typeof(BetterInputField))] [CanEditMultipleObjects]
	public class BetterInputFieldEditor : InputFieldEditor
	{
		private readonly BetterElementHelper<InputField, BetterInputField> helper = new();
		private SerializedProperty additionalPlaceholdersProp;

		protected override void OnEnable()
		{
			base.OnEnable();
			additionalPlaceholdersProp = serializedObject.FindProperty("additionalPlaceholders");
		}


		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			helper.DrawGui(serializedObject);

			ReorderableListGUI.Title("Additional Placeholders");
			ReorderableListGUI.ListField(additionalPlaceholdersProp);

			serializedObject.ApplyModifiedProperties();
		}

		[MenuItem("CONTEXT/InputField/♠ Make Better")]
		public static void MakeBetter(MenuCommand command)
		{
			var obj = command.context as InputField;
			Betterizer.MakeBetter<InputField, BetterInputField>(obj);
		}
	}
}
