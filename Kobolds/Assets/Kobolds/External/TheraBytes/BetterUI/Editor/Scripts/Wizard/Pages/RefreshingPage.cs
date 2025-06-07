using UnityEngine;

namespace TheraBytes.BetterUi.Editor
{
	public class RefreshingPage : WizardPage
	{
		public RefreshingPage(IWizard wizard)
			: base(wizard)
		{
		}

		public override string NameId => "RefreshingPage";
		protected override string NextButtonText => "...";

		protected override void OnInitialize()
		{
			Add(new InfoWizardPageElement("Please Wait ...", InfoType.Header));
			Add(new InfoWizardPageElement(new GUIContent(Resources.Load<Texture2D>("wizard_banner"))));
			Add(new InfoWizardPageElement("If the wizard disappers after recompiling, select:"));
			Add(new InfoWizardPageElement("     Tools -> Better UI -> Settings -> Setup Wizard", InfoType.Header));
			Add(new CustomWizardPageElement(o => { })); // disable "Next" button
		}

		protected override void AfterGui()
		{
			// no page info
		}
	}
}
