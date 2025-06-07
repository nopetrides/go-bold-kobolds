using System;
using UnityEditor;
using UnityEngine;

namespace TheraBytes.BetterUi.Editor
{
	public class InfoWithButtonWizardPageElement : WizardPageElementBase
	{
		public static float ButtonWidth = 100;
		private readonly ButtonInfo[] buttons;

		private readonly string text;

		public InfoWithButtonWizardPageElement(string text, string buttonText, Action buttonClickCallback)
			: this(text, new ButtonInfo(buttonText, buttonClickCallback))
		{
		}

		public InfoWithButtonWizardPageElement(string text, params ButtonInfo[] buttons)
		{
			this.text = text;
			this.buttons = buttons;

			markCompleteImmediately = true;
		}

		public override void DrawGui()
		{
			EditorGUILayout.BeginHorizontal();

			EditorGUILayout.BeginVertical();
			GUILayout.FlexibleSpace();

			EditorGUILayout.LabelField(text, EditorStyles.wordWrappedLabel);

			GUILayout.FlexibleSpace();
			EditorGUILayout.EndVertical();


			GUILayout.FlexibleSpace();

			EditorGUILayout.BeginVertical();

			foreach (var btn in buttons) btn.Draw(ButtonWidth);

			EditorGUILayout.EndVertical();

			EditorGUILayout.EndHorizontal();
		}

		public class ButtonInfo
		{
			public ButtonInfo(string buttonText, Action clickCallback)
			{
				ButtonText = buttonText;
				ClickCallback = clickCallback;
			}

			public string ButtonText { get; }
			public event Action ClickCallback;

			public void Draw(float width)
			{
				if (GUILayout.Button(ButtonText, GUILayout.Width(width), GUILayout.ExpandHeight(true)))
					if (ClickCallback != null)
						ClickCallback();
			}
		}
	}
}
