using TMPro;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;

namespace TheraBytes.BetterUi.Editor
{
	[CustomEditor(typeof(BetterTextMeshProDropdown))] [CanEditMultipleObjects]
	public class BetterTextMeshProDropDownEditor : DropdownEditor
	{
		private bool foldout = true;

		private readonly BetterElementHelper<TMP_Dropdown, BetterTextMeshProDropdown> showHideTransitions = new();

		private readonly BetterElementHelper<TMP_Dropdown, BetterTextMeshProDropdown> transitions = new();


		protected override void OnEnable()
		{
			base.OnEnable();
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

				transitions.DrawGui(serializedObject);
				showHideTransitions.DrawGui(serializedObject);

				serializedObject.ApplyModifiedProperties();

				EditorGUI.indentLevel--;
			}

			base.OnInspectorGUI();
		}

		[MenuItem("CONTEXT/TMP_Dropdown/♠ Make Better")]
		public static void MakeBetter(MenuCommand command)
		{
			var sel = command.context as TMP_Dropdown;
			Betterizer.MakeBetter<TMP_Dropdown, BetterTextMeshProDropdown>(sel);
		}
	}
}
