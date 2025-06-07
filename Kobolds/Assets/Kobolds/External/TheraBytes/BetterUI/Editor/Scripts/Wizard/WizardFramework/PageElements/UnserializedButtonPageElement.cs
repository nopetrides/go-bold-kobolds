using System;
using UnityEngine;

namespace TheraBytes.BetterUi.Editor
{
	public class UnserializedButtonPageElement : WizardPageElementBase
	{
		private readonly Action buttonCallback;
		private readonly GUIContent buttonContent;

		public UnserializedButtonPageElement(
			string buttonContent, Action buttonCallback, bool completeImmediately = true)
			: this(new GUIContent(buttonContent), buttonCallback, completeImmediately)
		{
		}

		public UnserializedButtonPageElement(
			GUIContent buttonContent, Action buttonCallback, bool completeImmediately = true)
		{
			this.buttonContent = buttonContent;
			this.buttonCallback = buttonCallback;
			markCompleteImmediately = completeImmediately;
		}

		public override void DrawGui()
		{
			if (GUILayout.Button(buttonContent) && buttonCallback != null) buttonCallback();
		}
	}
}
