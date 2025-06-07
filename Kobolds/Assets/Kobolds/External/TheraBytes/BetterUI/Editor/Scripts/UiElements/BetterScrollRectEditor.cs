using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TheraBytes.BetterUi.Editor
{
	[CustomEditor(typeof(BetterScrollRect))] [CanEditMultipleObjects]
	public class BetterScrollRectEditor : ScrollRectEditor
	{
		private SerializedProperty hProp, vProp;
		private SerializedProperty hSpacingFallback, hSpacingCollection;
		private SerializedProperty vSpacingFallback, vSpacingCollection;


		protected override void OnEnable()
		{
			base.OnEnable();

			hProp = serializedObject.FindProperty("horizontalStartPosition");
			vProp = serializedObject.FindProperty("verticalStartPosition");

			hSpacingFallback = serializedObject.FindProperty("horizontalSpacingFallback");
			hSpacingCollection = serializedObject.FindProperty("customHorizontalSpacingSizers");

			vSpacingFallback = serializedObject.FindProperty("verticalSpacingFallback");
			vSpacingCollection = serializedObject.FindProperty("customVerticalSpacingSizers");
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			var obj = target as BetterScrollRect;

			if (obj.horizontal)
			{
				EditorGUILayout.PropertyField(hProp);

				if (obj.horizontalScrollbar != null)
					if (GUILayout.Button("From current Horizontal Scrollbar value"))
						hProp.floatValue = obj.horizontalScrollbar.value;

				if (obj.horizontalScrollbarVisibility == ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport)
					ScreenConfigConnectionHelper.DrawSizerGui(
						"Horizontal Scrollbar Spacing", hSpacingCollection, ref hSpacingFallback);

				EditorGUILayout.Separator();
			}

			if (obj.vertical)
			{
				EditorGUILayout.PropertyField(vProp);

				if (obj.verticalScrollbar != null)
					if (GUILayout.Button("From current Vertical Scrollbar value"))
						vProp.floatValue = obj.verticalScrollbar.value;

				if (obj.verticalScrollbarVisibility == ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport)
					ScreenConfigConnectionHelper.DrawSizerGui(
						"Vertical Scrollbar Spacing", vSpacingCollection, ref vSpacingFallback);
			}

			serializedObject.ApplyModifiedProperties();
		}

		[MenuItem("CONTEXT/ScrollRect/♠ Make Better")]
		public static void MakeBetter(MenuCommand command)
		{
			var obj = command.context as ScrollRect;
			var hSpace = obj.horizontalScrollbarSpacing;
			var vSpace = obj.verticalScrollbarSpacing;

			var newScrollRect = Betterizer.MakeBetter<ScrollRect, BetterScrollRect>(obj);
			var betterVersion = newScrollRect as BetterScrollRect;
			if (betterVersion != null)
			{
				betterVersion.HorizontalSpacingSizer.SetSize(betterVersion, hSpace);
				betterVersion.VerticalSpacingSizer.SetSize(betterVersion, vSpace);
			}
		}
	}
}
