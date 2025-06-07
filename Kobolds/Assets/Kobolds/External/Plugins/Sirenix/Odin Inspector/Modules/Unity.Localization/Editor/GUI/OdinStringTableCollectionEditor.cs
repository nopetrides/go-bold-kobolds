//-----------------------------------------------------------------------
// <copyright file="OdinStringTableCollectionEditor.cs" company="Sirenix ApS">
// Copyright (c) Sirenix ApS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

#define USING_WIDTH_NON_PERCENT

using System;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Internal;
using Sirenix.OdinInspector.Modules.Localization.Editor.Configs;
using Sirenix.OdinInspector.Modules.Localization.Editor.Internal;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Sirenix.OdinInspector.Modules.Localization.Editor
{
	public class
		OdinStringTableCollectionEditor : OdinTableCollectionEditor<StringTableCollection, StringTable,
		StringTableEntry>
	{
		private string currentSyntaxErrorMessage;
		private Exception currentSyntaxException;
		private bool currentSyntaxHasErrors;
		private string currentSyntaxHighlightedText;
		private string currentSyntaxSource;

		public OdinStringTableCollectionEditor(
			StringTableCollection collection, OdinMenuEditorWindow relatedWindow,
			OdinLocalizationEditorWindow.WindowState windowState) :
			base(collection, relatedWindow, windowState)
		{
		}

		protected override void OnInitialize()
		{
			for (var i = 0; i < SharedEntries.Length; i++)
			{
				var sharedEntry = SharedEntries[i];

				MeasureEntry(sharedEntry);
			}

			//this.SharedEntries.OnSharedEntryAdded += (i, sharedEntry) => { this.MeasureEntry(sharedEntry); };

			//this.SharedEntries.OnSharedEntryRemoved += (i, sharedEntry) => { this.SharedEntryHeights.Remove(sharedEntry.Id); };

			OnTableEntryModified = sharedEntry =>
			{
				if (!Collection.SharedData.Contains(sharedEntry.Id)) return;

				var index = SharedEntries.GetIndex(sharedEntry);

				MeasureEntry(sharedEntry);

				EntryScrollView.ReallocateRect(index, SharedEntryHeights[sharedEntry.Id], sharedEntry);
			};
		}

		protected override void AllocateItems()
		{
			for (var i = 0; i < SharedEntries.Length; i++)
			{
				var sharedEntry = SharedEntries[i];

				if (!SharedEntries.IsVisible(sharedEntry)) continue;

				if (!SharedEntryHeights.ContainsKey(sharedEntry.Id)) MeasureEntry(sharedEntry);

				EntryScrollView.AllocateRect(SharedEntryHeights[sharedEntry.Id], sharedEntry);

#if false
				this.ControlIds[sharedEntry] = GUIUtility.GetControlID(FocusType.Keyboard);

				for (var j = 0; j < this.GUITables.Count; j++)
				{
					OdinGUITable<StringTable> table = this.GUITables[j];

					if (table.Type == OdinGUITable<StringTable>.GUITableType.Key)
					{
						continue;
					}

					StringTableEntry entry = table.Table.GetEntry(sharedEntry.Id);

					if (entry is null)
					{
						table.Table.AddEntry(sharedEntry.Id, string.Empty);

						entry = table.Table.GetEntry(sharedEntry.Id);
					}

					this.ControlIds[entry] = GUIUtility.GetControlID(FocusType.Keyboard);
				}
#endif
			}
		}

		protected override void DrawItems(ref OdinGUIScrollView.VisibleItems visibleItems)
		{
			MeasureVisibleEntries(ref visibleItems);

			var scrollSpeed = OdinLocalizationConfig.Instance.scrollSpeed;

			EntryScrollView.BeginScrollView(
				new Vector2(PinnedWidth, OdinLocalizationConstants.COLUMN_HEIGHT),
				new Vector2(-PinnedWidth, 0),
				scrollSpeed);
			{
				DrawEntries(ref visibleItems, false);
			}
			EntryScrollView.EndScrollView();

			EntryScrollView.BeginClip(
				offset: new Vector2(0.0f, OdinLocalizationConstants.COLUMN_HEIGHT), ignoreScrollX: true);
			{
				DrawEntries(ref visibleItems, true);
			}
			EntryScrollView.EndClip();
		}

		private void DrawEntries(ref OdinGUIScrollView.VisibleItems visibleItems, bool pinned)
		{
			for (var i = 0; i < visibleItems.Length; i++)
			{
				if (!visibleItems.HasAssociatedData(i)) continue;

				var hint = visibleItems.Offset + i + ControlIdHint;

				var position = visibleItems.GetRect(i);

				var sharedEntry = visibleItems.GetAssociatedData<SharedTableData.SharedTableEntry>(i);

				var isEven = (visibleItems.Offset + i) % 2 == 0;

				for (var j = 0; j < GUITables.Count; j++)
				{
					var table = GUITables[j];

					if (!table.IsVisible) continue;

					if (table.IsPinned != pinned) continue;

					if (!GUITables.TablesWithinVisibleBounds.Contains(table))
					{
						GUIUtility.GetControlID(hint, FocusType.Keyboard);
						position.TakeFromLeft(table.Width).Padding(OdinLocalizationConstants.ENTRY_PADDING);
						continue;
					}


#if USING_WIDTH_NON_PERCENT
					var entryRect = position.TakeFromLeft(table.Width).Padding(OdinLocalizationConstants.ENTRY_PADDING);
#else
					Rect entryRect =
 position.TakeFromLeft(table.Width).Padding(OdinLocalizationConstants.ENTRY_PADDING);
#endif

					bool isCellPressed, isSelected;

					switch (table.Type)
					{
						case OdinGUITable<StringTable>.GUITableType.Key:
							isSelected = IsSharedEntrySelected(sharedEntry);

							if (isSelected)
							{
								SelectionAnimFloat.Move(1 / 0.18f, Easing.InSine);

								var start = FancyColor.Gray;

								var end = OdinLocalizationGUI.Selected;

								FancyColor.PushBlend(start.Lerp(end, SelectionAnimFloat), FancyColor.BlendMode.Overlay);
							}

							isCellPressed = DrawCell(entryRect, isEven);

							DrawKey(entryRect, sharedEntry, GUIUtility.GetControlID(hint, FocusType.Keyboard));

							if (isSelected) FancyColor.PopBlend();

							if (isCellPressed) SelectSharedEntry(sharedEntry);

							break;

						case OdinGUITable<StringTable>.GUITableType.Default:
							var entry = table.Asset.GetEntry(sharedEntry.Id);

							isSelected = IsEntrySelected(entry);

							if (isSelected)
							{
								SelectionAnimFloat.Move(1 / 0.18f, Easing.InSine);

								var start = FancyColor.Gray;

								var end = OdinLocalizationGUI.Selected;

								if (entry.IsSmart && OdinLocalizationConfig.Instance.useSyntaxHighlighter)
								{
									if (currentSyntaxSource != entry.Value)
									{
										currentSyntaxHighlightedText =
											OdinLocalizationSyntaxHighlighter.HighlightAsRichText(entry.Value);
										currentSyntaxErrorMessage = OdinLocalizationSyntaxHighlighter.GetErrorMessage(
											entry.Value, out var foundError, out var exception);
										currentSyntaxHasErrors = foundError;
										currentSyntaxException = exception;
										currentSyntaxSource = entry.Value;
									}

									if (currentSyntaxHasErrors)
										FancyColor.PushBlend(
											start.Lerp(new FancyColor(0.68f, 0.2f, 0.2f), SelectionAnimFloat),
											FancyColor.BlendMode.Overlay);
									else
										FancyColor.PushBlend(
											start.Lerp(end, SelectionAnimFloat), FancyColor.BlendMode.Overlay);
								}
								else
								{
									FancyColor.PushBlend(
										start.Lerp(end, SelectionAnimFloat), FancyColor.BlendMode.Overlay);
								}
							}

							isCellPressed = DrawCell(entryRect, isEven);

							DrawEntry(
								entryRect, entry, GUIUtility.GetControlID(hint, FocusType.Keyboard), table,
								sharedEntry);

							if (isSelected)
							{
								if (OdinLocalizationConfig.Instance.useSyntaxHighlighter && entry.IsSmart &&
									currentSyntaxHasErrors)
								{
									var errorRect = entryRect.AlignLeft(OdinLocalizationConstants.ROW_MENU_WIDTH)
										.AlignMiddle(16);

									SdfIcons.DrawIcon(
										errorRect, SdfIconType.ExclamationOctagonFill,
										Event.current.IsMouseOver(errorRect) ?
											new Color(1, 1, 1, 1f) :
											new Color(1, 1, 1, 0.6f));

									if (Event.current.OnMouseDown(errorRect, 0))
									{
										RelatedWindow.ShowToast(
											ToastPosition.BottomLeft,
											SdfIconType.ExclamationOctagonFill,
											currentSyntaxErrorMessage,
											new Color(0.68f, 0.2f, 0.2f),
											20.0f);

										if (currentSyntaxException != null) Debug.LogException(currentSyntaxException);
									}
								}

								FancyColor.PopBlend();
							}

							if (isCellPressed)
							{
								if (entry is null) entry = table.Asset.AddEntry(sharedEntry.Id, string.Empty);

								SelectEntry(entry);
							}

							break;
					}
				}
			}
		}

		private void DrawEntry(
			Rect position, StringTableEntry entry, int id, OdinGUITable<StringTable> table,
			SharedTableData.SharedTableEntry sharedEntry)
		{
			bool changed;
			string value;

			var smartToggleRect = position.TakeFromRight(OdinLocalizationConstants.ROW_MENU_WIDTH);
			position.TakeFromLeft(OdinLocalizationConstants.ROW_MENU_WIDTH);

			if (entry?.Value is null)
			{
				value = OdinLocalizationGUI.TextField(position, string.Empty, out changed, id);
			}
			else if (OdinLocalizationConfig.Instance.useSyntaxHighlighter && entry.IsSmart &&
					entry == CurrentSelectedEntry)
			{
				value = OdinLocalizationGUI.TextFieldSyntaxHighlighted(
					position, entry.Value, currentSyntaxHighlightedText, out changed, id);

				if (changed)
				{
					currentSyntaxHighlightedText = OdinLocalizationSyntaxHighlighter.HighlightAsRichText(value);
					currentSyntaxErrorMessage = OdinLocalizationSyntaxHighlighter.GetErrorMessage(
						value, out var foundError, out var exception);
					currentSyntaxHasErrors = foundError;
					currentSyntaxException = exception;
					currentSyntaxSource = value;
				}
			}
			else
			{
				value = OdinLocalizationGUI.TextField(position, entry.Value, out changed, id);
			}

			if (changed)
			{
				if (entry == null) entry = table.Asset.AddEntry(sharedEntry.Id, value);

				Undo.RecordObject(entry.Table, "Modified String Table Entry Text");
				entry.Value = value;
				OdinLocalizationEvents.RaiseTableEntryModified(entry.SharedEntry);
				EditorUtility.SetDirty(entry.Table);
			}

			smartToggleRect = smartToggleRect.AlignMiddle(16);

			if (entry == null)
			{
				SdfIcons.DrawIcon(
					smartToggleRect, SdfIconType.Lightbulb,
					new Color(1, 1, 1, Event.current.IsMouseOver(smartToggleRect) ? 0.8f : 0.3f));

				if (Event.current.OnMouseDown(smartToggleRect, 0))
				{
					Undo.RecordObject(table.Asset, "Added String Table Entry By Smart Toggle");
					entry = table.Asset.AddEntry(sharedEntry.Id, string.Empty);

					entry.IsSmart = !entry.IsSmart;
					EditorUtility.SetDirty(table.Asset);
				}
			}
			else
			{
				SdfIcons.DrawIcon(
					smartToggleRect,
					entry.IsSmart ? SdfIconType.LightbulbFill : SdfIconType.Lightbulb,
					new Color(1, 1, 1, Event.current.IsMouseOver(smartToggleRect) ? 0.8f : 0.3f));

				if (Event.current.OnMouseDown(smartToggleRect, 0))
				{
					Undo.RecordObject(entry.Table, "Toggled Smart Flag On String Entry");
					entry.IsSmart = !entry.IsSmart;
					EditorUtility.SetDirty(entry.Table);
				}
			}

			GUI.Label(smartToggleRect, GUIHelper.TempContent(string.Empty, "Toggle Smart String"));
		}

		protected override void MeasureAllEntries()
		{
			for (var i = 0; i < SharedEntries.Length; i++) MeasureEntry(SharedEntries[i]);

			HasGUIChanged = true;
		}

		protected override void MeasureVisibleEntries(ref OdinGUIScrollView.VisibleItems visibleItems)
		{
			var dataOffset = visibleItems.Offset;

			for (var i = 0; i < visibleItems.Length; i++)
			{
				if (!visibleItems.HasAssociatedData(i)) continue;

				var sharedEntry = visibleItems.GetAssociatedData<SharedTableData.SharedTableEntry>(i);

				MeasureEntry(sharedEntry);

				EntryScrollView.ReallocateRect(dataOffset + i, SharedEntryHeights[sharedEntry.Id], sharedEntry);
			}
		}

		private void MeasureEntry(SharedTableData.SharedTableEntry sharedEntry)
		{
			float height = OdinLocalizationConstants.ROW_HEIGHT;

			for (var i = 0; i < GUITables.Count; i++)
			{
				var currentTable = GUITables[i];

				switch (currentTable.Type)
				{
					case OdinGUITable<StringTable>.GUITableType.Default:
						var strEntry = currentTable.Asset.GetEntry(sharedEntry.Id);

						if (strEntry is null) continue;

#if USING_WIDTH_NON_PERCENT
						var strEntryHeight = MeasureText(strEntry.Value, currentTable.Width);
#else
						float strEntryHeight = MeasureText(strEntry.Value, currentTable.Width);
#endif

						if (strEntryHeight > height) height = strEntryHeight;

						break;

					case OdinGUITable<StringTable>.GUITableType.Key:
#if USING_WIDTH_NON_PERCENT
						var keyHeight = MeasureText(sharedEntry.Key, currentTable.Width);
#else
						float keyHeight = MeasureText(sharedEntry.Key, currentTable.Width);
#endif

						if (keyHeight > height) height = keyHeight;

						break;
				}
			}

			SharedEntryHeights[sharedEntry.Id] = height;
		}
	}
}
