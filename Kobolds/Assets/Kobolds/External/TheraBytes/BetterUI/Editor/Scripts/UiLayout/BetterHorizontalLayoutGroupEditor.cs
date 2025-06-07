using UnityEditor;
using UnityEngine.UI;

namespace TheraBytes.BetterUi.Editor
{
#pragma warning disable 0618

	[CustomEditor(typeof(BetterHorizontalLayoutGroup))] [CanEditMultipleObjects]
	public class BetterHorizontalLayoutGroupEditor
		: BetterHorizontalOrVerticalLayoutGroupEditor<HorizontalLayoutGroup, BetterHorizontalLayoutGroup>
	{
		public override void OnInspectorGUI()
		{
			DrawObsoleteWarning();
			base.OnInspectorGUI();
		}
	}
#pragma warning restore 0618
}
