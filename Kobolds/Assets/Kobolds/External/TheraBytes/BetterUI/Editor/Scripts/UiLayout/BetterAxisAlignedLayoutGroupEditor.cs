using UnityEditor;
using UnityEngine.UI;

namespace TheraBytes.BetterUi.Editor
{
	[CustomEditor(typeof(BetterAxisAlignedLayoutGroup))] [CanEditMultipleObjects]
	public class BetterAxisAlignedLayoutGroupEditor
		: BetterHorizontalOrVerticalLayoutGroupEditor<HorizontalOrVerticalLayoutGroup, BetterAxisAlignedLayoutGroup>
	{
		public override void OnInspectorGUI()
		{
			DrawPaddingAndSpacingConfigurations();
			ScreenConfigConnectionHelper.DrawGui("Settings", settingsConfigs, ref settingsFallback, DrawSettings);

			serializedObject.ApplyModifiedProperties();
		}

		[MenuItem("CONTEXT/HorizontalLayoutGroup/♠ Make Better")]
		public static void MakeBetterHorizontal(MenuCommand command)
		{
			MakeBetter(command, BetterAxisAlignedLayoutGroup.Axis.Horizontal);
		}

		[MenuItem("CONTEXT/VerticalLayoutGroup/♠ Make Better")]
		public static void MakeBetterVertical(MenuCommand command)
		{
			MakeBetter(command, BetterAxisAlignedLayoutGroup.Axis.Vertical);
		}

		private static void MakeBetter(MenuCommand command, BetterAxisAlignedLayoutGroup.Axis orientation)
		{
#pragma warning disable 0618
			MarginSizeModifier exPadding = null;
			FloatSizeModifier exSpacing = null;
			var h = command.context as BetterHorizontalLayoutGroup;
			if (h != null)
			{
				exPadding = h.PaddingSizer;
				exSpacing = h.SpacingSizer;
			}
			else
			{
				var v = command.context as BetterVerticalLayoutGroup;
				if (v != null)
				{
					exPadding = v.PaddingSizer;
					exSpacing = v.SpacingSizer;
				}
			}

#pragma warning restore 0618

			var lg = MakeBetterLogic(command);


			if (lg != null)
			{
				lg.Orientation = orientation;

				if (exPadding != null)
				{
					CopySizerValues(exPadding, lg.PaddingSizer);
					exPadding.ModLeft.CopyTo(lg.PaddingSizer.ModLeft);
					exPadding.ModRight.CopyTo(lg.PaddingSizer.ModRight);
					exPadding.ModTop.CopyTo(lg.PaddingSizer.ModTop);
					exPadding.ModTop.CopyTo(lg.PaddingSizer.ModBottom);
				}

				if (exSpacing != null)
				{
					CopySizerValues(exSpacing, lg.SpacingSizer);
					exSpacing.Mod.CopyTo(lg.SpacingSizer.Mod);
				}
			}
		}

		private static void CopySizerValues<T>(ScreenDependentSize<T> source, ScreenDependentSize<T> target)
		{
			target.MinSize = source.MinSize;
			target.MaxSize = source.MaxSize;
			target.OptimizedSize = source.OptimizedSize;
		}
	}
}
