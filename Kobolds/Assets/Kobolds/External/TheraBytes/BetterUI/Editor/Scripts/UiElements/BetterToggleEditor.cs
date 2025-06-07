using UnityEditor;
using UnityEditor.UI;
using UnityEngine.UI;

namespace TheraBytes.BetterUi.Editor
{
	[CustomEditor(typeof(BetterToggle))] [CanEditMultipleObjects]
	public class BetterToggleEditor : ToggleEditor
	{
		private readonly BetterElementHelper<Toggle, BetterToggle> OnOffTransitions = new("betterToggleTransitions");

		private readonly BetterElementHelper<Toggle, BetterToggle> transitions = new();

		private readonly BetterElementHelper<Toggle, BetterToggle> transitionsWhenOff = new("betterTransitionsWhenOff");

		private readonly BetterElementHelper<Toggle, BetterToggle> transitionsWhenOn = new("betterTransitionsWhenOn");


		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			var tgl = target as BetterToggle;
			transitions.DrawGui(serializedObject);
			OnOffTransitions.DrawGui(serializedObject);
			transitionsWhenOn.DrawGui(serializedObject);
			transitionsWhenOff.DrawGui(serializedObject);

			serializedObject.ApplyModifiedProperties();
		}

		[MenuItem("CONTEXT/Toggle/♠ Make Better")]
		public static void MakeBetter(MenuCommand command)
		{
			var tgl = command.context as Toggle;
			Betterizer.MakeBetter<Toggle, BetterToggle>(tgl);
		}
	}
}
