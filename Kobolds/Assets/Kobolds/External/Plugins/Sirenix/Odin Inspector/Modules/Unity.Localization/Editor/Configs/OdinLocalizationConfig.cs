//-----------------------------------------------------------------------
// <copyright file="OdinLocalizationConfig.cs" company="Sirenix ApS">
// Copyright (c) Sirenix ApS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Modules.Localization.Editor.Internal;
using Sirenix.Reflection.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Sirenix.OdinInspector.Modules.Localization.Editor.Configs
{
	[GlobalConfig("Plugins/Sirenix/Odin Inspector/Modules/Unity.Localization/Editor/Configs")]
	public class OdinLocalizationConfig : GlobalConfig<OdinLocalizationConfig>
	{
		[ShowInInspector]
		[BoxGroup("User Interface")]
		[Range(96, 1024)]
		public int assetRowHeight = 128;

		[BoxGroup("Syntax Highlighting")]
		public bool useSyntaxHighlighter = true;

		[EnableIf(nameof(useSyntaxHighlighter))]
		[BoxGroup("Syntax Highlighting")]
		public ThemeColor placeholderColor = new(
			new Color(0.743147f, 0.9433962f, 0.9242815f), new Color(0, 0.5882353f, 0.5333334f));

		[EnableIf(nameof(useSyntaxHighlighter))]
		[BoxGroup("Syntax Highlighting")]
		public ThemeColor selectorColor = new(new Color(1.0f, 0.7727525f, 0.3632075f), new Color(1, 0.6470588f, 0));

		[EnableIf(nameof(useSyntaxHighlighter))]
		[BoxGroup("Syntax Highlighting")]
		public ThemeColor formatterColor = new(
			new Color(0.9921569f, 0.9855571f, 0.8823529f), new Color(0.9607843f, 0.9607843f, 0.8627451f));

		[BoxGroup("Navigation")]
		[Range(1, 1000.0f)]
		public float scrollSpeed = 24.0f;

		[BoxGroup("Navigation")]
		public bool invertMouseDragNavigation = true;

		[BoxGroup("Navigation")]
		[Range(0.5f, 5.0f)]
		public float mouseDragSpeed = 1.0f;

		[InfoBox(
			"We couldn't find the necessary methods/classes to perform custom undo operations, therefore this option has been disabled and will be considered false even if true.",
			VisibleIf = "@!OdinLocalizationReflectionValues.HasAPIForCustomUndo")]
		[EnableIf("@OdinLocalizationReflectionValues.HasAPIForCustomUndo")]
		[BoxGroup("Undo")]
		public bool useCustomUndoHandlingForAssetCollections = true;

		[Button(ButtonSizes.Large)]
		public void Reset()
		{
			if (!EditorUtility.DisplayDialog(
					"Odin Localization Config", "Are you certain you want to reset your Localization config?", "Yes",
					"No")) return;

			useCustomUndoHandlingForAssetCollections = OdinLocalizationReflectionValues.HasAPIForCustomUndo;

			assetRowHeight = 128;

			useSyntaxHighlighter = true;
			placeholderColor = new ThemeColor(
				new Color(0.743147f, 0.9433962f, 0.9242815f), new Color(0, 0.5882353f, 0.5333334f));
			selectorColor = new ThemeColor(new Color(1.0f, 0.7727525f, 0.3632075f), new Color(1, 0.6470588f, 0));
			formatterColor = new ThemeColor(
				new Color(0.9921569f, 0.9855571f, 0.8823529f), new Color(0.9607843f, 0.9607843f, 0.8627451f));

			scrollSpeed = 24.0f;
			invertMouseDragNavigation = true;
			mouseDragSpeed = 1.0f;
		}

		public class ThemeColorDrawer : OdinValueDrawer<ThemeColor>
		{
			protected override void Initialize()
			{
				base.Initialize();
				Property.State.Expanded = false;
			}

			protected override void DrawPropertyLayout(GUIContent label)
			{
				SirenixEditorGUI.BeginBox(string.Empty);
				{
					SirenixEditorGUI.BeginBoxHeader();
					{
						GUILayout_Internal.BeginRow();
						{
							GUILayout_Internal.BeginColumn(LayoutSize.Pixels(EditorGUIUtility.labelWidth + 6.0f));
							{
								Property.State.Expanded = EditorGUILayout.Foldout(
									Property.State.Expanded,
									$"{label.text} ({(EditorGUIUtility.isProSkin ? "Dark" : "Light")})",
									true);
							}
							GUILayout_Internal.EndColumn();

							GUILayout_Internal.BeginColumn(LayoutSize.Auto);
							{
								Property.Children[nameof(ThemeColor.Color)].Draw(null);
							}
							GUILayout_Internal.EndColumn();
						}
						GUILayout_Internal.EndRow();
					}
					SirenixEditorGUI.EndBoxHeader();

					var toggle = ValueEntry.ValueState != PropertyValueState.NullReference && Property.State.Expanded;

					if (SirenixEditorGUI.BeginFadeGroup(this, toggle))
					{
						GUILayout.BeginHorizontal();
						Property.Children[nameof(ThemeColor.lightColor)].Draw();
						GUILayout.Space(3.5f);
						GUILayout.EndHorizontal();

						GUILayout.BeginHorizontal();
						Property.Children[nameof(ThemeColor.darkColor)].Draw();
						GUILayout.Space(3.5f);
						GUILayout.EndHorizontal();
					}

					SirenixEditorGUI.EndFadeGroup();
				}
				SirenixEditorGUI.EndBox();

				if (Property.State.Expanded) GUILayout.Space(4.0f);
			}
		}

		[Serializable]
		public class ThemeColor
		{
			public Color lightColor;
			public Color darkColor;

			public ThemeColor(Color lightColor, Color darkColor)
			{
				this.lightColor = lightColor;
				this.darkColor = darkColor;
			}

			[ShowInInspector]
			public Color Color
			{
				get => EditorGUIUtility.isProSkin ? darkColor : lightColor;

				set
				{
					if (EditorGUIUtility.isProSkin)
						darkColor = value;
					else
						lightColor = value;
				}
			}

			public static implicit operator Color(ThemeColor color)
			{
				return color.Color;
			}
		}
	}
}
