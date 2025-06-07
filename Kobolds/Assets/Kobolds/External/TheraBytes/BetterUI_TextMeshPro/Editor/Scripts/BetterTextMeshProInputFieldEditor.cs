using TheraBytes.BetterUi.Editor.ThirdParty;
using TMPro;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;

namespace TheraBytes.BetterUi.Editor
{
	[CustomEditor(typeof(BetterTextMeshProInputField))] [CanEditMultipleObjects]
	public class BetterTextMeshProInputFieldEditor : TMP_InputFieldEditor
	{
		private readonly BetterElementHelper<TMP_InputField, BetterTextMeshProInputField> helper = new();
		private SerializedProperty additionalPlaceholdersProp;

		private bool foldout = true;

		private SerializedProperty overrideSizeProp;

		private SerializedProperty pointSizeScalerProp;

		protected override void OnEnable()
		{
			base.OnEnable();

			pointSizeScalerProp = serializedObject.FindProperty("pointSizeScaler");
			overrideSizeProp = serializedObject.FindProperty("overridePointSize");
			additionalPlaceholdersProp = serializedObject.FindProperty("additionalPlaceholders");
		}

		public override void OnInspectorGUI()
		{
			EditorGUILayout.Space();

			var origFontStyle = EditorStyles.foldout.fontStyle;
			EditorStyles.foldout.fontStyle = FontStyle.Bold;

			foldout = EditorGUILayout.Foldout(foldout, new GUIContent("Better UI"));

			EditorStyles.foldout.fontStyle = origFontStyle;

			if (foldout)
			{
				EditorGUI.indentLevel++;

				EditorGUILayout.PropertyField(overrideSizeProp);
				if (overrideSizeProp.boolValue) EditorGUILayout.PropertyField(pointSizeScalerProp);

				helper.DrawGui(serializedObject);

				ReorderableListGUI.Title("Additional Placeholders");
				ReorderableListGUI.ListField(additionalPlaceholdersProp);

				serializedObject.ApplyModifiedProperties();

				EditorGUI.indentLevel--;
			}


			base.OnInspectorGUI();
		}

		[MenuItem("CONTEXT/TMP_InputField/♠ Make Better")]
		public static void MakeBetter(MenuCommand command)
		{
			var obj = command.context as TMP_InputField;
			Betterizer.MakeBetter<TMP_InputField, BetterTextMeshProInputField>(obj);
		}
	}
}
