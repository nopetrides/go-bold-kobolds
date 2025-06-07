using UnityEditor;

namespace TheraBytes.BetterUi.Editor
{
	public class SeparatorWizardPageElement : WizardPageElementBase
	{
		public SeparatorWizardPageElement()
		{
			markCompleteImmediately = true;
		}

		public override void DrawGui()
		{
			EditorGUILayout.Separator();
		}
	}
}
