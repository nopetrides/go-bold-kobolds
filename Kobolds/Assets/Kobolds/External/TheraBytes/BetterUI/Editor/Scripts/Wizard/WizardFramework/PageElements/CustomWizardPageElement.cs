using System;
using UnityEngine;

namespace TheraBytes.BetterUi.Editor
{
	public class CustomWizardPageElement : WizardPageElementBase
	{
		private readonly Action<CustomWizardPageElement> drawGuiCallback;

		public CustomWizardPageElement(Action<CustomWizardPageElement> drawGuiCallback)
		{
			this.drawGuiCallback = drawGuiCallback;
		}

		public override void DrawGui()
		{
			if (drawGuiCallback == null)
			{
				Debug.LogError("No gui callback assigned for wizard element.");
				State = WizardElementState.Complete;
				return;
			}

			drawGuiCallback(this);
		}
	}
}
