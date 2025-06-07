using UnityEditor;
using UnityEditor.UI;
using UnityEngine.UI;

namespace TheraBytes.BetterUi.Editor
{
	[CustomEditor(typeof(BetterScrollbar))] [CanEditMultipleObjects]
	public class BetterScrollbarEditor : ScrollbarEditor
	{
		private readonly BetterElementHelper<Scrollbar, BetterScrollbar> helper = new();

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			helper.DrawGui(serializedObject);

			serializedObject.ApplyModifiedProperties();
		}

		[MenuItem("CONTEXT/Scrollbar/♠ Make Better")]
		public static void MakeBetter(MenuCommand command)
		{
			var obj = command.context as Scrollbar;
			Betterizer.MakeBetter<Scrollbar, BetterScrollbar>(obj);
		}
	}
}
