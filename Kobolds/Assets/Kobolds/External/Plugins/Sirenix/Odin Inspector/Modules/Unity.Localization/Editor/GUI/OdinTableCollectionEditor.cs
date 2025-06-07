//-----------------------------------------------------------------------
// <copyright file="OdinTableCollectionEditor.cs" company="Sirenix ApS">
// Copyright (c) Sirenix ApS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

#define USING_WIDTH_NON_PERCENT
//#undef USING_WIDTH_NON_PERCENT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Internal;
using Sirenix.OdinInspector.Modules.Localization.Editor.Configs;
using Sirenix.OdinInspector.Modules.Localization.Editor.Internal;
using Sirenix.Serialization;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Sirenix.OdinInspector.Modules.Localization.Editor
{
	public enum OdinTableSelectionType
	{
		None,
		SharedEntry,
		SharedTable,
		Table,
		TableEntry
	}

	public abstract class OdinTableCollectionEditor<TCollection, TTable, TEntry>
		where TCollection : LocalizationTableCollection
		where TTable : LocalizationTable
		where TEntry : TableEntry
	{
		public const int SELECTOR_ID = int.MinValue + 14085;

		internal struct DragInfo
		{
			public static DragInfo None => new() {Index = -1};

			public bool IsNone => Index == -1;

			public int Index;
		}

		[HideInInspector]
		public TCollection Collection;

		[HideInInspector]
		public OdinGUITableCollection<TTable> GUITables;

		[HideInInspector]
		public OdinSharedEntryCollection SharedEntries;

		[HideInInspector]
		public Dictionary<long, float> SharedEntryHeights;

		[HideInInspector]
		public OdinGUIScrollView EntryScrollView;

		[HideInInspector]
		public Dictionary<Locale, OdinGUITable<TTable>> LocaleTableMap;

		[HideInInspector]
		public SearchField SearchField = new();

		[HideInInspector]
		public OdinTableSelectionType SelectionType = OdinTableSelectionType.None;

		[HideInInspector]
		public OdinGUITable<TTable> CurrentSelectedTable;

		[HideInInspector]
		public SharedTableData.SharedTableEntry CurrentSelectedSharedEntry;

		[HideInInspector]
		public TEntry CurrentSelectedEntry;

		[HideInInspector]
		public OdinMenuItem MenuItem;

		protected OdinMenuEditorWindow RelatedWindow;
		protected int RelatedWindowId;
		protected OdinLocalizationEditorWindow.WindowState WindowState;

		protected float PinnedWidth;

		protected OdinGUITable<TTable> KeyTable;

		protected SirenixAnimationUtility.InterpolatedFloat SelectionAnimFloat = 0.0f;

		protected int ControlIdHint = "Odin_LocalizationEditor_Control".GetHashCode();
		protected int DragDropIdHint = "Odin_LocalizationEditor_DropId".GetHashCode();

		private bool isForceDeleteKey;
		private SharedTableData.SharedTableEntry keyToRemove;
		//private bool isDraggingNonHandle;

		protected Action<SharedTableData.SharedTableEntry> OnTableEntryModified;

		protected Action<AssetTableCollection, AssetTable, AssetTableEntry> OnAssetTableEntryAdded;

		protected Action<AssetTableCollection, AssetTable, AssetTableEntry, string> OnAssetTableEntryRemoved;

		//protected Dictionary<object, int> guiIDs;
		//protected Dictionary<object, int> ControlIds;

		protected bool HasHandledCurrentModifiedEntry;
		protected TEntry CurrentModifiedEntry;

		private Action<LocalizationTableCollection, LocalizationTable> OnTableAddedToCollection;

		private Action<LocalizationTableCollection, LocalizationTable> OnTableRemovedFromCollection;

		private readonly Undo.UndoRedoCallback OnUndoRedoPerformed;

		private float rightMenuWidth;

		private OdinGUITable<TTable> sortedTable;

		protected readonly List<LocalizationTable> LooseTables = new();

		public OdinTableCollectionEditor(
			TCollection collection, OdinMenuEditorWindow relatedWindow,
			OdinLocalizationEditorWindow.WindowState windowState)
		{
			Collection = collection;
			WindowState = windowState;

			GUITables = new OdinGUITableCollection<TTable>(Collection.Tables.Count);

#if USING_WIDTH_NON_PERCENT
			GUITables.AddKeyTable();
#else
			float averageWidthPercent = 1.0f / (this.Collection.Tables.Count + 1);

			this.GUITables.AddKeyTable(averageWidthPercent);
#endif
			KeyTable = GUITables.Last();

			LocaleTableMap = new Dictionary<Locale, OdinGUITable<TTable>>(Collection.Tables.Count);

			for (var i = 0; i < Collection.Tables.Count; i++)
			{
				var tableAsset = (TTable) Collection.Tables[i].asset;

				var tableLocale = LocalizationEditorSettings.GetLocale(tableAsset.LocaleIdentifier);

				if (tableLocale == null)
				{
					Debug.LogWarning(
						$"No locale found for {tableAsset.name} in {Collection.name}, searched for: {tableAsset.LocaleIdentifier}");
					continue;
				}

#if USING_WIDTH_NON_PERCENT
				var table = OdinGUITable<TTable>.CreateTable(tableAsset, tableLocale);
#else
				OdinGUITable<TTable> table = OdinGUITable<TTable>.CreateTable(tableAsset, averageWidthPercent);
#endif

				GUITables.Add(table);

				LocaleTableMap[LocalizationEditorSettings.GetLocale(tableAsset.LocaleIdentifier)] = table;
			}

			SharedEntries = new OdinSharedEntryCollection(Collection);

			//this.ControlIds = new Dictionary<object, int>(this.SharedEntries.Length + 64);

			EntryScrollView = new OdinGUIScrollView(SharedEntries.Length + 64, adjustViewForVerticalScrollBar: false);

			RelatedWindow = relatedWindow;
			RelatedWindowId = RelatedWindow.GetInstanceID();
			keyToRemove = null;
			isForceDeleteKey = false;

			LooseTables.Clear();
			LocalizationEditorSettings.FindLooseStringTablesUsingSharedTableData(Collection.SharedData, LooseTables);
			rightMenuWidth = EditorPrefs.GetFloat("OdinTableCollectionEditor_RightMenuWidth", 300.0f);

			OnUndoRedoPerformed += () =>
			{
				Collection.RefreshAddressables();
				MenuItem.Name = Collection.SharedData.TableCollectionName;
			};
		}

		protected abstract void OnInitialize();

		private bool hasInitialized;

		public void Initialize()
		{
			if (hasInitialized) return;

			SharedEntryHeights = new Dictionary<long, float>(SharedEntries.Length + 128);

			OnTableAddedToCollection += (collection, table) =>
			{
				if (Collection != collection) return;

				var locale = LocalizationEditorSettings.GetLocale(table.LocaleIdentifier);

				if (locale == null)
				{
					Debug.LogWarning(
						$"No locale found for {table.name} in {collection.name}, searched for: {table.LocaleIdentifier}");
					return;
				}

				if (LocaleTableMap.ContainsKey(locale)) return;

#if USING_WIDTH_NON_PERCENT
				var guiTable = OdinGUITable<TTable>.CreateTable((TTable) table, locale);
#else
				float lastAveragePercent = 1.0f / this.GUITables.Count;

				float newAveragePercent = 1.0f / (this.GUITables.Count + 1);

				for (var i = 0; i < this.GUITables.Count; i++)
				{
					this.GUITables[i].WidthPercentage *= newAveragePercent / lastAveragePercent;
				}

				OdinGUITable<TTable> guiTable = OdinGUITable<TTable>.CreateTable((TTable) table, newAveragePercent);
#endif

				GUITables.Add(guiTable);

				LocaleTableMap[locale] = guiTable;

				LooseTables.Clear();
				LocalizationEditorSettings.FindLooseStringTablesUsingSharedTableData(
					Collection.SharedData, LooseTables);
			};

			OnTableRemovedFromCollection += (collection, table) =>
			{
				if (Collection != collection) return;

#if !USING_WIDTH_NON_PERCENT
				float lastAveragePercent = 1.0f / this.GUITables.Count;
#endif

				var locale = LocalizationEditorSettings.GetLocale(table.LocaleIdentifier);

				GUITables.Remove(LocaleTableMap[locale]);

				LocaleTableMap.Remove(locale);

#if !USING_WIDTH_NON_PERCENT
				float newAveragePercent = 1.0f / this.GUITables.Count;

				for (var i = 0; i < this.GUITables.Count; i++)
				{
					this.GUITables[i].WidthPercentage *= newAveragePercent / lastAveragePercent;
				}
#endif

				LooseTables.Clear();
				LocalizationEditorSettings.FindLooseStringTablesUsingSharedTableData(
					Collection.SharedData, LooseTables);
			};

#if false
			this.UndoHandler = () =>
			{
				switch (Undo.GetCurrentGroupName())
				{
					case "Add table to collection":
						for (var i = 0; i < this.Collection.Tables.Count; i++)
						{
							LocalizationTable tableAsset = this.Collection.Tables[i].asset;
							Locale locale = LocalizationEditorSettings.GetLocale(tableAsset.LocaleIdentifier);

							if (locale == null)
							{
								Debug.LogWarning($"No locale found for {tableAsset.name} in {this.Collection.name}, searched for: {tableAsset.LocaleIdentifier}");
								continue;
							}

							if (this.LocaleTableMap.ContainsKey(locale))
							{
								continue;
							}

							OdinGUITable<TTable> table = OdinGUITable<TTable>.CreateTable((TTable) tableAsset, locale);

							this.GUITables.Add(table);

							this.LocaleTableMap.Add(locale, table);

							this.Collection.RemoveTable(tableAsset);
							this.Collection.AddTable(tableAsset);
						}

						var localesToRemove = new Stack<Locale>();

						foreach (KeyValuePair<Locale, OdinGUITable<TTable>> kvp in this.LocaleTableMap)
						{
							if (!this.Collection.ContainsTable(kvp.Key.Identifier))
							{
								localesToRemove.Push(kvp.Key);
							}
						}

						while (localesToRemove.Count > 0)
						{
							Locale locale = localesToRemove.Pop();
							OdinGUITable<TTable> table = this.LocaleTableMap[locale];

							this.GUITables.Remove(table);
							this.LocaleTableMap.Remove(locale);
						}

						break;
				}
			};
#endif

			OnInitialize();

			hasInitialized = true;
		}

		private bool needsToCheckForErrors;

		public void OnSelectInWindow()
		{
			needsToCheckForErrors = true;

			Initialize();

			AttachEvents();
		}

		private bool hasAttachedEvents;

		public void AttachEvents()
		{
			if (hasAttachedEvents) return;

			// this.SharedEntries.AttachEvents();

			if (OnTableEntryModified != null)
				LocalizationEditorSettings.EditorEvents.TableEntryModified += OnTableEntryModified;

			if (OnAssetTableEntryAdded != null)
				LocalizationEditorSettings.EditorEvents.AssetTableEntryAdded += OnAssetTableEntryAdded;

			if (OnAssetTableEntryRemoved != null)
				LocalizationEditorSettings.EditorEvents.AssetTableEntryRemoved += OnAssetTableEntryRemoved;

			if (OnTableAddedToCollection != null)
				LocalizationEditorSettings.EditorEvents.TableAddedToCollection += OnTableAddedToCollection;

			if (OnTableRemovedFromCollection != null)
				LocalizationEditorSettings.EditorEvents.TableRemovedFromCollection += OnTableRemovedFromCollection;

			if (OnUndoRedoPerformed != null) Undo.undoRedoPerformed += OnUndoRedoPerformed;

			hasAttachedEvents = true;
		}

		public void DetachEvents()
		{
			if (!hasAttachedEvents) return;

			// this.SharedEntries.DetachEvents();

			if (OnTableEntryModified != null)
				LocalizationEditorSettings.EditorEvents.TableEntryModified -= OnTableEntryModified;

			if (OnAssetTableEntryAdded != null)
				LocalizationEditorSettings.EditorEvents.AssetTableEntryAdded -= OnAssetTableEntryAdded;

			if (OnAssetTableEntryRemoved != null)
				LocalizationEditorSettings.EditorEvents.AssetTableEntryRemoved -= OnAssetTableEntryRemoved;

			if (OnTableAddedToCollection != null)
				LocalizationEditorSettings.EditorEvents.TableAddedToCollection -= OnTableAddedToCollection;

			if (OnTableRemovedFromCollection != null)
				LocalizationEditorSettings.EditorEvents.TableRemovedFromCollection -= OnTableRemovedFromCollection;

			if (OnUndoRedoPerformed != null) Undo.undoRedoPerformed -= OnUndoRedoPerformed;

			hasAttachedEvents = false;
		}

		public virtual void RemoveKey(SharedTableData.SharedTableEntry sharedEntry)
		{
			SharedEntryHeights.Remove(sharedEntry.Id);

			GUITables.UndoRecordCollection(Collection.SharedData, "Removed Shared Table Entry from Collection");

			Collection.RemoveEntry(sharedEntry.Id);

			GUITables.SetDirty(Collection.SharedData);
		}

		public void SelectEntry(TEntry entry)
		{
#if false
			if (this.Collection.SharedData.Metadata.HasMetadata<OdinTemplateMetadata>())
			{
				var templateMetadata = this.Collection.SharedData.Metadata.GetMetadata<OdinTemplateMetadata>();

				if (templateMetadata.MetadataExpected.Count > 0)
				{
					for (var i = 0; i < templateMetadata.MetadataExpected.Count; i++)
					{
						if (this.HasMetadataAmountOfType(entry.MetadataEntries, templateMetadata.MetadataExpected[i], templateMetadata))
						{
							continue;
						}

						entry.AddMetadata((IMetadata) templateMetadata.MetadataExpected[i].InstantiateDefault(false));
					}
				}
			}
#endif

			var lastSelectionType = SelectionType;

			SelectionType = OdinTableSelectionType.TableEntry;

			var lastSelection = CurrentSelectedEntry;

			CurrentSelectedEntry = entry;

			if (lastSelection == entry && SelectionType == lastSelectionType) return;

			SelectionAnimFloat = 0.0f;
			SelectionAnimFloat.Destination = 1.0f;

			if (WindowState.CurrentTopTab != OdinLocalizationEditorWindow.RightMenuTopTabs.Metadata) return;

			WindowState.ShowSharedMetadata = false;

			UpdateMetadataViewForEntry(entry);
		}

		public void SelectSharedEntry(SharedTableData.SharedTableEntry sharedEntry)
		{
			SelectionType = OdinTableSelectionType.SharedEntry;

			if (CurrentSelectedSharedEntry != sharedEntry)
			{
				SelectionAnimFloat = 0.0f;
				SelectionAnimFloat.Destination = 1.0f;
			}

			CurrentSelectedSharedEntry = sharedEntry;

			if (WindowState.CurrentTopTab != OdinLocalizationEditorWindow.RightMenuTopTabs.Metadata) return;

			WindowState.ShowSharedMetadata = true;

			WindowState.MetadataTree?.Dispose();

			WindowState.MetadataTree = PropertyTree.Create(sharedEntry);
		}

		public void SelectTable(OdinGUITable<TTable> table)
		{
			SelectionType = table.Type == OdinGUITable<TTable>.GUITableType.Key ?
				OdinTableSelectionType.SharedTable :
				OdinTableSelectionType.Table;
			CurrentSelectedTable = table;
		}

		public void UpdateMetadataViewForEntry(TEntry entry)
		{
			WindowState.MetadataTree?.Dispose();

			object metadataData = null;

			if (WindowState.ShowSharedMetadata)
			{
				metadataData = entry.SharedEntry;
			}
			else
			{
				if (typeof(TEntry) == typeof(AssetTableEntry))
					metadataData = OdinLocalizationReflectionValues.AssetTableEntry_Data_Property.GetValue(entry);

				if (typeof(TEntry) == typeof(StringTableEntry))
					metadataData = OdinLocalizationReflectionValues.StringTableEntry_Data_Property.GetValue(entry);
			}

			if (metadataData != null) WindowState.MetadataTree = PropertyTree.Create(metadataData);
		}

		public bool IsSharedEntrySelected(SharedTableData.SharedTableEntry sharedEntry)
		{
			return SelectionType == OdinTableSelectionType.SharedEntry && CurrentSelectedSharedEntry == sharedEntry;
		}

		public bool IsEntrySelected(TEntry entry)
		{
			if (entry == null) return false;

			return SelectionType == OdinTableSelectionType.TableEntry && CurrentSelectedEntry == entry;
		}

		public bool IsTableSelected(OdinGUITable<TTable> table)
		{
			return (SelectionType == OdinTableSelectionType.Table ||
					SelectionType == OdinTableSelectionType.SharedTable) && CurrentSelectedTable == table;
		}

		public void ClearSelection()
		{
			GUIUtility.hotControl = 0;
			GUIUtility.keyboardControl = 0;
			SelectionType = OdinTableSelectionType.None;
			WindowState.MetadataTree?.Dispose();
			WindowState.MetadataTree = null;
		}

		public void ClearFocus()
		{
			GUIUtility.hotControl = 0;
			GUIUtility.keyboardControl = 0;
		}

		protected bool HasGUIChanged = true;
		private int lastGUIEntryCount;


		private bool firstTimeSeeingTable = true;

		private AddressableEntryNotFoundException tableAddressableException;
		private string exceptionHeaderMsg = string.Empty;
		private string exceptionMsg = string.Empty;


		[OnInspectorGUI]
		public void DrawAndHandleExceptions()
		{
			if (tableAddressableException != null)
			{
				const float SPACING = 10;

				var rect = GUILayoutUtility.GetRect(0, 0, GUILayoutOptions.ExpandWidth().ExpandHeight());

				rect = rect.AlignCenter(520, 200);

				var shadowColor = EditorGUIUtility.isProSkin ?
					new Color(1, 0, 0, 0.2588235f) :
					new Color(1, 0, 0, 0.3137255f);

				OdinLocalizationGUI.DrawRoundBlur20(rect, shadowColor);

				var backgroundColor = EditorGUIUtility.isProSkin ?
					new Color(0.6037736f, 0.1566394f, 0.1566394f) :
					new Color(0.8301887f, 0.238875f, 0.238875f);

				SirenixEditorGUI.DrawRoundRect(rect, backgroundColor, 7.5f);

				rect = rect.Padding(14);

				var buttonsArea = rect.TakeFromBottom(32);

				GUI.BeginClip(rect.Expand(14));
				{
					var watermarkPosition = rect.SetPosition(Vector2.zero).AlignRight(80).Expand(34).AddX(30).SubY(20);

					SdfIcons.DrawIcon(
						watermarkPosition, SdfIconType.ExclamationDiamondFill, new Color(1, 1, 1, 0.075f));
				}
				GUI.EndClip();

				rect.height -= SPACING;

				var msgHeight = OdinLocalizationGUI.CardTitleWhite.CalcHeight(exceptionHeaderMsg, rect.width);
				GUI.Label(rect.TakeFromTop(msgHeight), exceptionHeaderMsg, OdinLocalizationGUI.CardTitleWhite);

				rect.yMin += SPACING;

				GUI.Label(rect, exceptionMsg, SirenixGUIStyles.MultiLineWhiteLabel);

				if (OdinLocalizationGUI.OverlaidButton(buttonsArea.TakeFromRight(120), "Fix All", SdfIconType.Tools))
				{
					Collection.RefreshAddressables();
					tableAddressableException = null;

					RelatedWindow.ShowToast(
						ToastPosition.BottomLeft,
						SdfIconType.Tools,
						$"Refreshed Addressables for '{Collection.name}'.",
						new Color(0.26f, 0.51f, 0.44f),
						12.0f);
				}

				buttonsArea.width -= SPACING;

				if (OdinLocalizationGUI.OverlaidButton(
						buttonsArea.TakeFromRight(160), "Fix And Preload All", SdfIconType.Tools))
				{
					Collection.RefreshAddressables();
					Collection.SetPreloadTableFlag(true);
					tableAddressableException = null;

					RelatedWindow.ShowToast(
						ToastPosition.BottomLeft,
						SdfIconType.Tools,
						$"Refreshed Addressables and Preloaded All Tables for '{Collection.name}'.",
						new Color(0.26f, 0.51f, 0.44f),
						12.0f);
				}

				return;
			}

			try
			{
				Draw();

				if (needsToCheckForErrors)
				{
					// NOTE: this attempts to catch any AddressableEntryNotFoundException errors, by fetching the Addressables for every table.
					Collection.IsPreloadTableFlagSet();

					needsToCheckForErrors = false;
				}
			}
			catch (AddressableEntryNotFoundException e)
			{
				tableAddressableException = e;

				exceptionHeaderMsg = e.Message;
				exceptionMsg = $"There could be multiple other tables facing the same issue in '{Collection.name}', " +
								"this can potentially be resolved by refreshing the Addressables.";

				GUIHelper.ExitGUI(false);
			}
		}

		public void Draw()
		{
			if (Event.current.type == EventType.MouseUp) SharedUniqueControlId.SetInactive();

			if (EntryScrollView.IsDraggingMouse)
				EditorGUIUtility.AddCursorRect(EntryScrollView.InteractRect, MouseCursor.Pan);

			if (HasHandledCurrentModifiedEntry) CurrentModifiedEntry = null;

			//	if (Event.current.type == EventType.MouseUp)
			//	{
			//		this.isDraggingNonHandle = false;
			//	}
			//	
			var shouldClearSelection = Event.current.OnKeyDown(KeyCode.Escape, false);

			//this.SharedEntries.UpdateIfChangesArePresent();

			var position = GUILayoutUtility.GetRect(0, 0, GUILayoutOptions.ExpandWidth().ExpandHeight());

			position = RelatedWindow.position.SetPosition(Vector2.zero);
			position.TakeFromRight(RelatedWindow.MenuWidth);

			var leftMenuSliderRect = position.TakeFromLeft(10).SubX(1);
			var rightMenuRect = position.TakeFromRight(WindowState.RightMenuWidth);
			var rightMenuSliderRect = position.TakeFromRight(11);

			RelatedWindow.MenuWidth += VerticalSlideRect(leftMenuSliderRect.AddXMax(1), false);
			RelatedWindow.MenuWidth = Mathf.Max(RelatedWindow.MenuWidth, 1);

			if (Event.current.clickCount > 1 && Event.current.IsMouseOver(leftMenuSliderRect))
			{
				RelatedWindow.MenuWidth = 1;

				if (Event.current.control || Event.current.alt || Event.current.shift) WindowState.RightMenuWidth = 0;
			}

			if (Event.current.clickCount > 1 && Event.current.IsMouseOver(rightMenuSliderRect))
			{
				if (Event.current.control || Event.current.alt || Event.current.shift)
				{
					if (WindowState.RightMenuWidth > 0)
					{
						WindowState.RightMenuWidth = 0;
						WindowState.LeftMenuWidth = 0;
					}
					else
					{
						WindowState.RightMenuWidth = WindowState.LastOpenRightMenuWidth;
						RelatedWindow.MenuWidth = WindowState.LastOpenRightMenuWidth;
					}
				}
				else
				{
					if (WindowState.RightMenuWidth > 0)
						WindowState.RightMenuWidth = 0;
					else
						WindowState.RightMenuWidth = WindowState.LastOpenRightMenuWidth;
				}
			}

			var toolbarRect = position.TakeFromTop(OdinLocalizationConstants.TOOLBAR_HEIGHT);

			var dragHandleRect = position.TakeFromLeft(OdinLocalizationConstants.DRAG_HANDLE_WIDTH);

			DrawToolbar(toolbarRect);

			GUITables.Sort();

#if USING_WIDTH_NON_PERCENT
			for (var i = 0; i < GUITables.Count; i++) GUITables[i].Width = GUITables[i].Width;
#endif

			var viewWidth = position.width;

#if USING_WIDTH_NON_PERCENT
			var columnsWidth = GUITables.GetVisibleWidth();

			if (columnsWidth >= viewWidth) viewWidth = columnsWidth;
#else
			int columnsMinTotalWidth =
 this.GUITables.GetVisibleCount() * OdinLocalizationConstants.DEFAULT_COLUMN_WIDTH;

			if (columnsMinTotalWidth > viewWidth)
			{
				viewWidth = columnsMinTotalWidth;
			}
#endif

			if (position != EntryScrollView.InteractRect) HasGUIChanged = true;

			var isEntryCountChanged = Collection.SharedData.Entries.Count != lastGUIEntryCount;

			if (HasGUIChanged || isEntryCountChanged)
			{
#if false
				if (isEntryCountChanged)
				{
					if (this.SharedEntries.IsSorted)
					{
						this.Resort();
					}

					if (this.SharedEntries.IsSearching)
					{
						this.SharedEntries.UpdateSearchTerm(this.SharedEntries.SearchTerm, this.GUITables, this.Collection);
					}
				}
#endif

				HasGUIChanged = false;

				if (isEntryCountChanged)
				{
					if (SharedEntries.IsSorted)
						switch (sortedTable.Type)
						{
							case OdinGUITable<TTable>.GUITableType.Default:
								switch (sortedTable.Asset)
								{
									case AssetTable assetTable:
										SharedEntries.SortByAssetTable(
											Collection as AssetTableCollection, assetTable, false);
										break;

									case StringTable stringTable:
										SharedEntries.SortByStringTable(stringTable, false);
										break;
								}

								break;

							case OdinGUITable<TTable>.GUITableType.Key:
								SharedEntries.SortByKeys(false);
								break;

							default:
								throw new ArgumentOutOfRangeException();
						}

					if (SharedEntries.IsSearching)
						SharedEntries.UpdateSearchTerm(SharedEntries.SearchTerm, GUITables, Collection, true);
				}

				lastGUIEntryCount = Collection.SharedData.Entries.Count;

				var previousY = EntryScrollView.PositionY;

				EntryScrollView.SetBounds(position, viewWidth);

				EntryScrollView.BeginAllocations();
				{
					AllocateItems();
				}
				EntryScrollView.EndAllocations();

				if (adjustViewForSeparatorChange &&
					lastViewHeight != 0.0f &&
					Math.Abs(lastViewHeight - EntryScrollView.ViewRect.height) > 0.01f)
				{
					var newHeight = EntryScrollView.ViewRect.height;

					var change = previousY / lastViewHeight;

					EntryScrollView.PositionY = change * newHeight;

					adjustViewForSeparatorChange = false;
				}
			}
			else
			{
				EntryScrollView.SetBoundsForCurrentAllocations(position, viewWidth);
			}

#if !USING_WIDTH_NON_PERCENT
			this.GUITables.CalcWidths(this.EntryScrollView);
#endif

			PinnedWidth = 0.0f;

			for (var i = 0; i < GUITables.Count; i++)
			{
				var table = GUITables[i];

				if (!table.IsVisible || !table.IsPinned) continue;

#if USING_WIDTH_NON_PERCENT
				PinnedWidth += table.Width;
#else
					this.PinnedWidth += this.GUITables[i].Width;
#endif
			}

			if (PinnedWidth > EntryScrollView.Bounds.width)
			{
				PinnedWidth = EntryScrollView.Bounds.width;
				GUITables.ResizePinnedToFit(EntryScrollView.Bounds.width);
			}

			GUITables.UpdateVisibleTables(EntryScrollView, PinnedWidth);

			if (firstTimeSeeingTable && columnsWidth < viewWidth)
			{
				GUITables.ResizeToFit(EntryScrollView.Bounds.width - PinnedWidth);
				firstTimeSeeingTable = false;
			}


			var visibleItems = EntryScrollView.GetVisibleItems();

			DrawRows(ref visibleItems);

			DrawPseudoRows();

			DrawItems(ref visibleItems);

			DrawColumnsAndSeparators(ref visibleItems);

			DrawDragHandles(dragHandleRect, ref visibleItems);

			DrawRightMenu(rightMenuRect);

			if (keyToRemove != null)
			{
				if (isForceDeleteKey ||
					EditorUtility.DisplayDialog(
						"Odin Table Collection Editor", $"Are you sure you want to remove entry: {keyToRemove.Key}?",
						"Yes", "No"))
					RemoveKey(keyToRemove);

				keyToRemove = null;
				isForceDeleteKey = false;
			}

			if (shouldClearSelection) ClearSelection();

			EntryScrollView.HandleMiddleMouseDrag(
				OdinLocalizationConfig.Instance.invertMouseDragNavigation,
				speed: OdinLocalizationConfig.Instance.mouseDragSpeed);


			WindowState.RightMenuWidth -= VerticalSlideRect(rightMenuSliderRect, true);
			WindowState.RightMenuWidth = Mathf.Max(WindowState.RightMenuWidth, 0);

			if (WindowState.RightMenuWidth > 338) WindowState.LastOpenRightMenuWidth = WindowState.RightMenuWidth;
		}

		protected abstract void AllocateItems();

		protected abstract void DrawItems(ref OdinGUIScrollView.VisibleItems visibleItems);

		protected abstract void MeasureAllEntries();

		protected abstract void MeasureVisibleEntries(ref OdinGUIScrollView.VisibleItems visibleItems);

		// NOTE: returns true if pressed, TODO add xml comments later
		protected static bool DrawCell(Rect rect, bool isEven)
		{
			Color background = isEven ? OdinLocalizationGUI.RowEvenBackground2 : OdinLocalizationGUI.RowOddBackground2;

			GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 1, background, 0, 2.5f);

			if (Event.current.IsMouseOver(rect) && !DragAndDropUtilities.IsDragging)
				GUI.DrawTexture(
					rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 1, OdinLocalizationGUI.RowBorderHover,
					1, 2.5f);
			else
				GUI.DrawTexture(
					rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 1, OdinLocalizationGUI.RowBorder, 1,
					2.5f);

			rect.x += OdinLocalizationConstants.ROW_MENU_WIDTH;
			rect.width -= OdinLocalizationConstants.ROW_MENU_WIDTH + OdinLocalizationConstants.ROW_MENU_WIDTH;
			var isPressed = Event.current.OnMouseDown(rect, 0, false);

			return isPressed;
		}

		protected void DrawKey(Rect rect, SharedTableData.SharedTableEntry sharedEntry, int id)
		{
			var removeRect = rect.TakeFromLeft(OdinLocalizationConstants.ROW_MENU_WIDTH);
			var copyKeyIdRect = rect.TakeFromRight(OdinLocalizationConstants.ROW_MENU_WIDTH);

			var removeBgColor = Event.current.IsMouseOver(removeRect) ?
				new Color(0.8f, 0.1f, 0.1f, 0.8f) :
				new Color(0, 0, 0, 0.2f);

			GUI.DrawTexture(
				removeRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 1.0f, removeBgColor, Vector4.zero,
				new Vector4(2.5f, 0.0f, 0.0f, 2.5f));

			if (Event.current.OnMouseDown(removeRect, 0))
			{
				keyToRemove = sharedEntry;

				if (Event.current.modifiers == EventModifiers.Shift) isForceDeleteKey = true;

				ClearSelection();

				return;
			}

			var removeFgColor = Event.current.IsMouseOver(removeRect) ?
				new Color(1, 1, 1, 0.8f) :
				new Color(1, 1, 1, 0.5f);

			removeRect = removeRect.AlignCenter(14, 14);

			SdfIcons.DrawIcon(removeRect, SdfIconType.X, removeFgColor);

			// copyKeyIdRect.x -= 20;

			copyKeyIdRect = copyKeyIdRect.AlignCenter(16, 16);

			var isMouseOverKeyRect = Event.current.IsMouseOver(copyKeyIdRect);

			var m = GUI.matrix;
			GUIUtility.RotateAroundPivot(45.0f, copyKeyIdRect.center);
			SdfIcons.DrawIcon(copyKeyIdRect, SdfIconType.KeyFill, new Color(1, 1, 1, isMouseOverKeyRect ? 0.8f : 0.3f));
			GUI.matrix = m;

			if (isMouseOverKeyRect)
				GUI.Label(copyKeyIdRect, GUIHelper.TempContent(string.Empty, "Copy Shared Entry Id"));

			if (Event.current.OnMouseDown(copyKeyIdRect, 0))
			{
				RelatedWindow.ShowToast(
					ToastPosition.BottomLeft,
					SdfIconType.Clipboard,
					$"Copied Shared Entry Id '{sharedEntry.Id}' to the clipboard.",
					new Color(0.23f, 0.36f, 0.68f),
					8.0f);

				Clipboard.Copy(sharedEntry.Id.ToString());

				ClearSelection();
			}

			var result = OdinLocalizationGUI.TextField(rect, sharedEntry.Key, out var changed, id);

			if (!changed) return;

			if (Collection.SharedData.Contains(result) && result != sharedEntry.Key)
			{
				RelatedWindow.ShowToast(
					ToastPosition.BottomLeft,
					SdfIconType.ExclamationOctagonFill,
					$"Key '{result}' already exists in the collection.",
					new Color(0.68f, 0.2f, 0.2f),
					8.0f);
			}
			else
			{
				Undo.RecordObject(Collection.SharedData, "Renamed Shared Table Entry Key");
				Collection.SharedData.RenameKey(sharedEntry.Id, result);
				OdinLocalizationEvents.RaiseTableEntryModified(sharedEntry);
				EditorUtility.SetDirty(Collection.SharedData);
			}
		}

		protected static float MeasureText(string text, float width)
		{
			// TODO: get rid of this magic number
			width -= 20 + 20 + 8 + 16;

			var rowHeightWithoutText = OdinLocalizationConstants.ROW_HEIGHT -
										SirenixGUIStyles.MultiLineCenteredLabel.lineHeight;

			var heightOfText = SirenixGUIStyles.MultiLineCenteredLabel.CalcHeight(text, width) -
								SirenixGUIStyles.MultiLineCenteredLabel.padding.vertical;

			return rowHeightWithoutText + heightOfText;
		}

		private void DrawToolbar(Rect position)
		{
			var originalPosition = position;

			var resizeToFitButtonRect = position.TakeFromRight(180f);
			var addButtonRect = position.TakeFromRight(180f);

			position = position.Padding(4);

			if (GUI.Button(resizeToFitButtonRect, "Resize Columns To Fit", SirenixGUIStyles.ToolbarButton))
			{
				GUITables.ResizeToFit(EntryScrollView.Bounds.width - PinnedWidth);
				HasGUIChanged = true;
			}

			if (GUI.Button(addButtonRect, "Add Shared Entry", SirenixGUIStyles.ToolbarButton))
			{
				GUITables.UndoRecordCollection(Collection.SharedData, "Added Shared Entry To Collection");
				var sharedEntry = Collection.SharedData.AddKey();

				OdinLocalizationEvents.RaiseTableEntryAdded(Collection, sharedEntry);
				GUITables.SetDirty(Collection.SharedData);
			}

			var searchTerm = SearchField.Draw(position, SharedEntries.SearchTerm, "Search for item(s)...");

			if (SharedEntries.UpdateSearchTerm(searchTerm, GUITables, Collection)) HasGUIChanged = true;

			if (!EditorGUIUtility.isProSkin) EditorGUI.DrawRect(originalPosition, new Color(0, 0, 0, 0.05f));
		}

		private void DrawRows(ref OdinGUIScrollView.VisibleItems visibleItems)
		{
			var clipRect = EntryScrollView.GetClipRect();

			clipRect.x -= OdinLocalizationConstants.DRAG_HANDLE_WIDTH;
			clipRect.width += OdinLocalizationConstants.DRAG_HANDLE_WIDTH;

			EntryScrollView.BeginClip(clipRect, new Vector2(0, OdinLocalizationConstants.COLUMN_HEIGHT));
			{
				for (var i = 0; i < visibleItems.Length; i++)
				{
					var rect = visibleItems.GetRect(i);

					var dropZoneRect = rect;

					rect.width += OdinLocalizationConstants.DRAG_HANDLE_WIDTH;

					var isEven = (visibleItems.Offset + i) % 2 == 0;

					EditorGUI.DrawRect(
						rect, isEven ? OdinLocalizationGUI.RowEvenBackground : OdinLocalizationGUI.RowOddBackground);

					HandleDropZone(dropZoneRect, visibleItems.Offset + i);
				}
			}
			EntryScrollView.EndClip();
		}

		private void DrawPseudoRows()
		{
			if (EntryScrollView.IsBeyondVerticalBounds) return;

			var remainderRect = new Rect(
				EntryScrollView.Bounds.x - OdinLocalizationConstants.DRAG_HANDLE_WIDTH,
				EntryScrollView.Bounds.y + EntryScrollView.ViewRect.height + OdinLocalizationConstants.COLUMN_HEIGHT,
				EntryScrollView.Bounds.width + OdinLocalizationConstants.DRAG_HANDLE_WIDTH,
				EntryScrollView.Bounds.height - EntryScrollView.ViewRect.height -
				OdinLocalizationConstants.COLUMN_HEIGHT);

			var maintainedRemainderRect = remainderRect;

			var isNextEven = SharedEntries.Length % 2 == 0;

			while (remainderRect.height > 0)
			{
				var rect = remainderRect.TakeFromTop(OdinLocalizationConstants.ROW_HEIGHT);
				Color color = isNextEven ? OdinLocalizationGUI.RowEvenBackground : OdinLocalizationGUI.RowOddBackground;

				EditorGUI.DrawRect(rect, color);

				isNextEven = !isNextEven;
			}

			EditorGUI.DrawRect(maintainedRemainderRect, new Color(0, 0, 0, 0.25f));
		}

		private void DrawColumnsAndSeparators(ref OdinGUIScrollView.VisibleItems visibleItems)
		{
			EditorGUI.DrawRect(
				EntryScrollView.Bounds.AlignTop(OdinLocalizationConstants.COLUMN_HEIGHT),
				OdinLocalizationGUI.ColumnBackground);

			var columnArea = EntryScrollView.ViewRect;

			if (!EntryScrollView.IsBeyondVerticalBounds) columnArea.height = EntryScrollView.Bounds.height;

			var lastIndex = GUITables.GetLastVisibleIndex();

			EntryScrollView.BeginClip(offset: new Vector2(PinnedWidth, 0), ignoreScrollY: true);
			{
				DrawColumns(ref visibleItems, columnArea.AlignRight(columnArea.width), false, lastIndex);
			}
			EntryScrollView.EndClip();

			var lastPinnedIndex = GUITables.GetLastVisiblePinnedIndex();

			EntryScrollView.BeginClip(ignoreScrollX: true, ignoreScrollY: true);
			{
				DrawColumns(ref visibleItems, columnArea, true, lastPinnedIndex);
			}
			EntryScrollView.EndClip();

			if (PinnedWidth > 0.0f && PinnedWidth + 20 <= EntryScrollView.Bounds.width)
			{
				var shadowRect = EntryScrollView.Bounds;

				shadowRect.x += PinnedWidth;
				shadowRect.width = 24;

				GUI.DrawTexture(
					shadowRect, OdinLocalizationGUITextures.LeftToRightFade, ScaleMode.StretchToFill, true, 1.0f,
					new Color(0, 0, 0, 0.35f), 0, 0);
			}
		}

		private void DrawColumns(
			ref OdinGUIScrollView.VisibleItems visibleItems, Rect columnArea, bool pinned, int lastIndex)
		{
			for (var i = 0; i < GUITables.Count; i++)
			{
				var table = GUITables[i];

				if (table.IsPinned != pinned) continue;

				if (!GUITables.TablesWithinVisibleBounds.Contains(table))
				{
					columnArea.TakeFromLeft(table.Width);
					continue;
				}

#if USING_WIDTH_NON_PERCENT
				var columnRect = columnArea.TakeFromLeft(table.Width);
#else
				Rect columnRect = columnArea.TakeFromLeft(table.Width);
#endif

				var columnHeaderRect = columnRect.AlignTop(OdinLocalizationConstants.COLUMN_HEIGHT);

				var isSelected = IsTableSelected(table);

				if (isSelected)
				{
					FancyColor.PushBlend(
						FancyColor.Gray.Lerp(OdinLocalizationGUI.Selected, 0.5f), FancyColor.BlendMode.Overlay);
					EditorGUI.DrawRect(columnHeaderRect, OdinLocalizationGUI.ColumnBackground);
				}

				var interactColumnRect = columnHeaderRect.Padding(4, 0);

				if (Event.current.IsMouseOver(interactColumnRect))
					EditorGUI.DrawRect(columnHeaderRect, new Color(1.0f, 1.0f, 1.0f, 0.035f));

				var pinRect = columnHeaderRect.TakeFromRight(30).SubXMax(10).AlignMiddle(18);
				var pinIcon = table.IsPinned ? SdfIconType.PinAngleFill : SdfIconType.PinAngle;

				GUI.Label(pinRect, GUIHelper.TempContent(string.Empty, "Pin Table"));

				if (Event.current.IsMouseOver(pinRect))
					SdfIcons.DrawIcon(pinRect, pinIcon, Color.white);
				else
					SdfIcons.DrawIcon(pinRect, pinIcon);

				if (Event.current.OnMouseDown(pinRect, 0))
				{
					table.IsPinned = !table.IsPinned;

					ClearFocus();
				}

				var columnTextWidth = SirenixGUIStyles.LabelCentered.CalcWidth(table.DisplayName);

				var minSortRect = columnHeaderRect.TakeFromLeft(30).AddXMin(10);

				var sortRect = columnHeaderRect.AlignCenter(20, 18);

				sortRect.x -= columnTextWidth * 0.5f + 12.0f;

				if (sortRect.x < minSortRect.x) sortRect = minSortRect.AlignMiddle(18);

				sortRect = sortRect.AlignCenter(16, 16);

				if (EditorGUIUtility.isProSkin)
				{
					GUI.Label(columnHeaderRect, table.DisplayName, SirenixGUIStyles.LabelCentered);
				}
				else
				{
					var t = SirenixGUIStyles.LabelCentered.normal.textColor;
					SirenixGUIStyles.LabelCentered.normal.textColor = new Color(0, 0, 0, 0.7f);
					GUI.Label(columnHeaderRect, table.DisplayName, SirenixGUIStyles.LabelCentered);
					SirenixGUIStyles.LabelCentered.normal.textColor = t;
				}

				SdfIconType sortIcon;

				if (sortedTable == table)
					switch (SharedEntries.CurrentSortOrderState)
					{
						case OdinSharedEntryCollection.SortOrderState.Unsorted:
							sortIcon = SdfIconType.ArrowDownUp;
							break;

						case OdinSharedEntryCollection.SortOrderState.Ascending:
							sortIcon = SdfIconType.ArrowDown;
							break;

						case OdinSharedEntryCollection.SortOrderState.Descending:
							sortIcon = SdfIconType.ArrowUp;
							break;

						default:
							throw new ArgumentOutOfRangeException();
					}
				else
					sortIcon = SdfIconType.ArrowDownUp;

				if (Event.current.IsMouseOver(sortRect))
					SdfIcons.DrawIcon(sortRect, sortIcon, Color.white);
				else
					SdfIcons.DrawIcon(sortRect, sortIcon);

				GUI.Label(sortRect, GUIHelper.TempContent(string.Empty, "Sort Table"));

				if (Event.current.OnMouseDown(sortRect, 0))
				{
					var wasSorted = SharedEntries.IsSorted && sortedTable != table;

					if (sortedTable == table || SharedEntries.CurrentSortOrderState ==
						OdinSharedEntryCollection.SortOrderState.Unsorted) SharedEntries.GotoNextSortOrderState();

					switch (table.Type)
					{
						case OdinGUITable<TTable>.GUITableType.Default:
							switch (table.Asset)
							{
								case AssetTable assetTable:
									SharedEntries.SortByAssetTable(
										Collection as AssetTableCollection, assetTable, wasSorted);
									break;

								case StringTable stringTable:
									SharedEntries.SortByStringTable(stringTable, wasSorted);
									break;
							}

							break;

						case OdinGUITable<TTable>.GUITableType.Key:
							SharedEntries.SortByKeys(wasSorted);
							break;

						default:
							throw new ArgumentOutOfRangeException();
					}

					sortedTable = table;

					ClearFocus();

					HasGUIChanged = true;
				}

				if (Event.current.OnMouseDown(interactColumnRect, 0)) SelectTable(table);

				if (isSelected) FancyColor.PopBlend();

#if !USING_WIDTH_NON_PERCENT
				if (i == lastIndex)
				{
					continue;
				}
#endif

				DrawSeparator(ref visibleItems, columnRect, table, i, lastIndex);
			}
		}

		private bool hasSeparatorChanged;
		private bool adjustViewForSeparatorChange;
		private float lastViewHeight;

		private void DrawSeparator(
			ref OdinGUIScrollView.VisibleItems visibleItems, Rect columnRect, OdinGUITable<TTable> table, int index,
			int lastIndex)
		{
			var separatorRect = columnRect.AlignRight(1);

			EditorGUI.DrawRect(separatorRect, OdinLocalizationGUI.RowBorder);

			var separatorMouseRect = separatorRect.Expand(1, 0);

			switch (Event.current.type)
			{
				case EventType.MouseDown:
					if (Event.current.button == 0 && Event.current.IsMouseOver(separatorMouseRect))
						//this.isDraggingNonHandle = true;
						ClearFocus();

					break;

				case EventType.MouseUp:
					//if (this.isDraggingNonHandle)
					//{
					// NOTE: we only adjust the ones we can see while we drag separators, to avoid unnecessary computations.
					if (hasSeparatorChanged)
					{
						MeasureAllEntries();
						hasSeparatorChanged = false;

						adjustViewForSeparatorChange = true;
					}
					//}

					//this.isDraggingNonHandle = false;
					break;
			}

			var slideAmount = table.HandleSlider(separatorMouseRect);

			if (slideAmount.x == 0.0f) return;

			if (!hasSeparatorChanged) lastViewHeight = EntryScrollView.ViewRect.height;


			hasSeparatorChanged = true;

#if USING_WIDTH_NON_PERCENT
			AppendWidth(slideAmount.x, index, lastIndex);
#else
			float newWidth = table.Width + slideAmount.x;

			OdinGUITable<TTable> nextTable = this.GUITables.GetNextVisible(index);

			float nextNewWidth = nextTable.Width - slideAmount.x;

			if (nextNewWidth < OdinLocalizationConstants.MIN_COLUMN_WIDTH)
			{
				float diff = OdinLocalizationConstants.MIN_COLUMN_WIDTH - nextNewWidth;

				newWidth -= diff;

				nextNewWidth += diff;
			}

			if (newWidth < OdinLocalizationConstants.MIN_COLUMN_WIDTH)
			{
				float diff = OdinLocalizationConstants.MIN_COLUMN_WIDTH - newWidth;

				newWidth += diff;

				nextNewWidth -= diff;
			}

			table.WidthPercentage *= newWidth / table.Width;

			nextTable.WidthPercentage *= nextNewWidth / nextTable.Width;
#endif
			MeasureVisibleEntries(ref visibleItems);
		}

		private void AppendWidth(float change, int index, int lastIndex)
		{
			var table = GUITables[index];

			table.Width += change;

			if (change < 0.0f && table.Width <= OdinLocalizationConstants.MIN_COLUMN_WIDTH)
			{
				var previousIndex = index - 1;

				while (previousIndex > -1)
				{
					var previousTable = GUITables[previousIndex];

					if (previousTable.IsPinned != table.IsPinned) break;

					previousTable.Width += change;

					if (previousTable.Width <= OdinLocalizationConstants.MIN_COLUMN_WIDTH)
					{
						previousIndex--;
						continue;
					}

					break;
				}

				if (previousIndex == -1) previousIndex = 0;

				if (index != lastIndex && GUITables[previousIndex].Width > OdinLocalizationConstants.MIN_COLUMN_WIDTH)
					GUITables[index + 1].Width -= change;
			}
			else if (index != lastIndex)
			{
				GUITables[index + 1].Width -= change;
			}
		}

		private void DrawDragHandles(Rect position, ref OdinGUIScrollView.VisibleItems visibleItems)
		{
			if (EntryScrollView.IsBeyondHorizontalBounds)
				OdinGUIScrollView.ScrollBackground(position.AlignBottom(OdinGUIScrollView.SCROLL_BAR_SIZE), false);

			var clipRect = EntryScrollView.GetClipRect();

			clipRect.x -= position.width;
			clipRect.width = position.width;

			EntryScrollView.BeginClip(clipRect, new Vector2(0, OdinLocalizationConstants.COLUMN_HEIGHT), true);
			{
				if (SharedEntries.IsSorted || SharedEntries.IsSearching) //(this.isDraggingNonHandle)
				{
					for (var i = 0; i < visibleItems.Length; i++)
					{
						var dragHandleRect = visibleItems.GetRect(i);

						dragHandleRect.width = OdinLocalizationConstants.DRAG_HANDLE_WIDTH;

						dragHandleRect.x += 2;
						dragHandleRect.width -= 4;

						if (EditorGUIUtility.isProSkin)
							SdfIcons.DrawIcon(
								dragHandleRect.AlignMiddle(16), SdfIconType.GripVertical,
								new Color(0.35f, 0.35f, 0.35f, 1.0f));
						else
							SdfIcons.DrawIcon(
								dragHandleRect.AlignMiddle(16), SdfIconType.GripVertical, new FancyColor(0.66f));
					}
				}
				else
				{
					var isDraggingAnything = IsDraggingAnything();

					for (var i = 0; i < visibleItems.Length; i++)
					{
						var dragHandleRect = visibleItems.GetRect(i);

						dragHandleRect.width = OdinLocalizationConstants.DRAG_HANDLE_WIDTH;

						var isMouseOver = Event.current.IsMouseOver(dragHandleRect);

						if (!isDraggingAnything)
						{
							var dragData = new DragInfo {Index = visibleItems.Offset + i};
							DragAndDropUtilities.DragZone(dragHandleRect, dragData, false, false);
						}

						dragHandleRect.x += 2;
						dragHandleRect.width -= 4;

						if (EditorGUIUtility.isProSkin)
							SdfIcons.DrawIcon(
								dragHandleRect.AlignMiddle(16), SdfIconType.GripVertical,
								new Color(1, 1, 1, isMouseOver ? 0.8f : 0.6f));
						else
							SdfIcons.DrawIcon(
								dragHandleRect.AlignMiddle(16), SdfIconType.GripVertical,
								new Color(0, 0, 0, isMouseOver ? 0.6f : 0.4f));
					}
				}
			}
			EntryScrollView.EndClip();

			EditorGUI.DrawRect(
				position.TakeFromTop(OdinLocalizationConstants.COLUMN_HEIGHT), OdinLocalizationGUI.ColumnBackground);
		}

		// NOTE: for now we pass by index, since you can't drag stuff around when you're searching or sorting
		private void HandleDropZone(Rect position, int indexTo)
		{
			position.x += OdinLocalizationConstants.DRAG_HANDLE_WIDTH;

			var halfHeight = position.height * 0.5f;

			var topDropRect = position.AlignTop(halfHeight);
			var bottomDropRect = position.AlignBottom(halfHeight);

			var topId = DragDropIdHint + indexTo;

			var topValue = DragAndDropUtilities.DropZone(topDropRect, DragInfo.None, DragDropIdHint + indexTo);

			var bottomId = DragDropIdHint + indexTo + SharedEntries.Length;

			var bottomValue = DragAndDropUtilities.DropZone(bottomDropRect, DragInfo.None, bottomId);

			if (DragAndDropUtilities.IsDragging)
			{
				if (DragAndDropUtilities.HoveringAcceptedDropZone == topId)
				{
					if (EditorGUIUtility.isProSkin)
						GUI.DrawTexture(
							topDropRect.AlignTop(40.0f).SubXMin(OdinLocalizationConstants.DRAG_HANDLE_WIDTH),
							OdinLocalizationGUITextures.TopToBottomFade,
							ScaleMode.StretchToFill,
							true,
							1.0f,
							new Color(0.16f, 0.7f, 1f, 0.25f),
							Vector4.zero,
							Vector4.zero);
					else
						GUI.DrawTexture(
							topDropRect.AlignTop(40.0f).SubXMin(OdinLocalizationConstants.DRAG_HANDLE_WIDTH),
							OdinLocalizationGUITextures.TopToBottomFade,
							ScaleMode.StretchToFill,
							true,
							1.0f,
							new Color(0.8f, 0.8f, 1, 0.7f),
							Vector4.zero,
							Vector4.zero);
					//EditorGUI.DrawRect(topDropRect.AlignTop(1), new Color(0, 1, 1, 0.5f));
				}

				if (DragAndDropUtilities.HoveringAcceptedDropZone == bottomId)
				{
					if (EditorGUIUtility.isProSkin)
						GUI.DrawTexture(
							bottomDropRect.AlignBottom(40.0f).SubXMin(OdinLocalizationConstants.DRAG_HANDLE_WIDTH),
							OdinLocalizationGUITextures.BottomToTopFade,
							ScaleMode.StretchToFill,
							true,
							1.0f,
							new Color(0.16f, 0.7f, 1f, 0.25f),
							Vector4.zero,
							Vector4.zero);
					else
						GUI.DrawTexture(
							bottomDropRect.AlignBottom(40.0f).SubXMin(OdinLocalizationConstants.DRAG_HANDLE_WIDTH),
							OdinLocalizationGUITextures.BottomToTopFade,
							ScaleMode.StretchToFill,
							true,
							1.0f,
							new Color(0.8f, 0.8f, 1, 0.7f),
							Vector4.zero,
							Vector4.zero);
					//EditorGUI.DrawRect(bottomDropRect.AlignBottom(1), new Color(0, 1, 1, 0.5f));
				}
			}

			if (!topValue.IsNone)
			{
				SharedEntries.MoveEntry(topValue.Index, indexTo);

				HasGUIChanged = true;

				return;
			}

			if (!bottomValue.IsNone)
			{
				SharedEntries.MoveEntry(bottomValue.Index, indexTo + 1);

				HasGUIChanged = true;
			}
		}

		protected void MoveScrollPositionToTable(OdinGUITable<TTable> table)
		{
			var x = 0.0f;

#if USING_WIDTH_NON_PERCENT
			for (var i = 0; i < GUITables.Count; i++)
			{
				if (GUITables[i] == table)
				{
					x += GUITables[i].Width * 0.5f;
					break;
				}

				if (!GUITables[i].IsVisible || GUITables[i].IsPinned) continue;

				x += GUITables[i].Width;
			}
#else
			for (var i = 0; i < this.GUITables.Count; i++)
			{
				if (this.GUITables[i] == table)
				{
					x += this.GUITables[i].Width * 0.5f;
					break;
				}

				if (!this.GUITables[i].IsVisible || this.GUITables[i].IsPinned)
				{
					continue;
				}

				x += this.GUITables[i].Width;
			}
#endif

			x -= EntryScrollView.Bounds.width * 0.5f;

			x += PinnedWidth * 0.5f;

			EntryScrollView.ScrollTo(1.0f / 0.35f, x, easing: Easing.OutQuad);
		}

		private float rightMenuTopPanelHeight;
		private Rect topPanelRect = Rect.zero;
		private Rect bottomPanelRect = Rect.zero;

		private void DrawRightMenu(Rect position)
		{
			EditorGUI.DrawRect(position, OdinLocalizationGUI.WindowBackground);

			var topPanelMaxHeight = position.height - 32;
			topPanelRect = position.TakeFromTop(WindowState.RightMenuTopPanelHeight);
			var topSlideRect = topPanelRect.TakeFromBottom(14);
			bottomPanelRect = position;

			WindowState.RightMenuTopPanelHeight += HorizontalSlideRect(topSlideRect);
			// 183 is enough height to show exactly 3 collapsed entries.
			WindowState.RightMenuTopPanelHeight = Mathf.Clamp(
				WindowState.RightMenuTopPanelHeight, 183, topPanelMaxHeight);

			EditorGUI.DrawRect(topPanelRect, OdinLocalizationGUI.Panel);
			EditorGUI.DrawRect(bottomPanelRect, OdinLocalizationGUI.Panel);


			EditorGUI.DrawRect(topPanelRect.AlignTop(32), OdinLocalizationGUI.TabsBackground);
			EditorGUI.DrawRect(bottomPanelRect.AlignTop(32), OdinLocalizationGUI.TabsBackground);

			WindowState.CurrentTopTab = OdinLocalizationGUI.Tabs(
				topPanelRect.TakeFromTop(32), WindowState.CurrentTopTab, 115);

			WindowState.CurrentBottomTab = OdinLocalizationGUI.Tabs(
				bottomPanelRect.TakeFromTop(32), WindowState.CurrentBottomTab, 115);

			switch (WindowState.CurrentTopTab)
			{
				case OdinLocalizationEditorWindow.RightMenuTopTabs.Metadata:
					DrawTopTabMetadata(topPanelRect);
					break;

				case OdinLocalizationEditorWindow.RightMenuTopTabs.Settings:
					DrawTopTabSettings(topPanelRect);
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			switch (WindowState.CurrentBottomTab)
			{
				case OdinLocalizationEditorWindow.RightMenuBottomTabs.Locale:
					DrawBottomTabLocale(bottomPanelRect);
					break;

#if false
				case OdinLocalizationEditorWindow.RightMenuBottomTabs.Template:
					this.DrawBottomTabTemplate(this.bottomPanelRect);
					break;
#endif

				case OdinLocalizationEditorWindow.RightMenuBottomTabs.Settings:
					DrawBottomTabSettings(bottomPanelRect);
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
			{
				dragging = false;
				GUIHelper.RequestRepaint();
			}
		}

		private string metadataSearchTerm = string.Empty;
		private SearchField metadataSearchField = new();

		public InspectorProperty[] GetMetadataProperties()
		{
			var metadataCollection =
				WindowState?.MetadataTree?.RootProperty?.Children[
					OdinLocalizationReflectionValues.TABLE_ENTRY_DATA__METADATA__PATH];

			var items =
				metadataCollection?.Children[OdinLocalizationReflectionValues.METADATA_COLLECTION__ITEMS__PATH];

			return items?.Children.OrderBy(c => c.ValueEntry.TypeOfValue.Name).ToArray();
		}

		private LocalizationMetadata localizationMetadata;

		private void DrawTopTabMetadata(Rect rect)
		{
			if (Event.current.OnMouseDown(rect, 0, false)) GUIHelper.RemoveFocusControl();

			if (SelectionType == OdinTableSelectionType.None) return;

			if (localizationMetadata == null) localizationMetadata = new LocalizationMetadata(Collection, WindowState);

			switch (SelectionType)
			{
				case OdinTableSelectionType.None:
					break;
				case OdinTableSelectionType.SharedEntry:
					localizationMetadata.Target = CurrentSelectedSharedEntry;
					break;
				case OdinTableSelectionType.SharedTable:
					localizationMetadata.Target = Collection;
					break;
				case OdinTableSelectionType.Table:
					localizationMetadata.Target = CurrentSelectedTable.Asset;
					break;
				case OdinTableSelectionType.TableEntry:
					localizationMetadata.Target = CurrentSelectedEntry;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			localizationMetadata.Draw(rect);
		}

		private void DrawTopTabSettings(Rect position)
		{
			position = position.Padding(4);

			GUILayout.BeginArea(position);
			{
				bool value;

				switch (SelectionType)
				{
					case OdinTableSelectionType.Table:
						EditorGUI.BeginChangeCheck();

						value = GUILayout.Toggle(
							LocalizationEditorSettings.GetPreloadTableFlag(CurrentSelectedTable.Asset),
							"Preload Table");

						if (EditorGUI.EndChangeCheck())
							LocalizationEditorSettings.SetPreloadTableFlag(CurrentSelectedTable.Asset, value, true);

						break;

					case OdinTableSelectionType.TableEntry:
						if (CurrentSelectedEntry is StringTableEntry stringTableEntry)
						{
							EditorGUI.BeginChangeCheck();

							value = GUILayout.Toggle(stringTableEntry.IsSmart, "Smart");

							if (EditorGUI.EndChangeCheck())
							{
								stringTableEntry.IsSmart = value;
								EditorUtility.SetDirty(stringTableEntry.Table);
							}

							break;
						}

						goto default;

					default:
						GUILayout.Label("No item with settings selected.", SirenixGUIStyles.LabelCentered);
						break;
				}
			}
			GUILayout.EndArea();
		}

		private Vector2 bottomTabLocaleScrollPosition = Vector2.zero;

		private List<Toggle> toggles;
		private static FancyColor ThumbColorGrayscale = FancyColor.White;
		private static FancyColor BackgroundColorGrayscale = FancyColor.Gray;
		private static FancyColor BorderColorGrayscale = new(0.91f, 0.91f, 0.91f);
		private static readonly FancyColor EnabledColor = new(EditorGUIUtility.isProSkin ? 0.66f : 0.86f);
		private static readonly FancyColor DisabledColor = new(EditorGUIUtility.isProSkin ? 0.46f : 0.66f);
		private bool dragging;
		private Toggle lastChangedToggle;
		private bool newValue;
		private Vector2 localeTabScrollPosition;

		private void DrawBottomTabLocale(Rect position)
		{
			position = position.Padding(4);

			GUILayout.BeginArea(position);
			{
				var projectLocales = LocalizationEditorSettings.GetLocales();

				if (toggles == null)
				{
					toggles = new List<Toggle>
					{
						new()
						{
							Label = "Key",
							Toggled = KeyTable.IsVisible
						}
					};
					toggles.AddRange(
						projectLocales.Select(locale =>
						{
							LocaleTableMap.TryGetValue(locale, out var table);

							return new Toggle
							{
								Label = locale.LocaleName,
								Toggled = table?.IsVisible ?? false
							};
						}));
				}

				var hasAllLocales = projectLocales.Count == LocaleTableMap.Count;

				if (!hasAllLocales)
				{
					if (SirenixEditorGUI.Button("Add Missing Locales", ButtonSizes.Large))
						for (var i = 0; i < projectLocales.Count; i++)
						{
							var locale = projectLocales[i];

							if (LocaleTableMap.ContainsKey(locale)) continue;

							var table = Collection.GetTable(locale.Identifier);

							if (table != null)
								Collection.AddTable(table, true);
							else
								Collection.AddNewTable(locale.Identifier);
						}

					GUILayout.Space(4);
					SirenixEditorGUI.HorizontalLineSeparator();
					GUILayout.Space(4);
				}

				const float LINE_HEIGHT = 20.0f;
				const float LOCALE_SPACING = 2.0f;

				localeTabScrollPosition = GUILayout.BeginScrollView(
					localeTabScrollPosition /*, GUILayoutOptions.MaxHeight(MAX_HEIGHT)*/);
				{
					var keyRect = GUILayoutUtility.GetRect(0, LINE_HEIGHT, GUILayoutOptions.ExpandWidth());
					KeyTable.IsVisible = DrawLocaleToggle(ref keyRect, toggles[0], KeyTable);

					if (EntryScrollView.IsBeyondHorizontalBounds && KeyTable.IsVisible &&
						!KeyTable.IsPinned)
						if (Event.current.OnMouseDown(keyRect, 0))
							MoveScrollPositionToTable(KeyTable);

					var lastLocaleIndex = projectLocales.Count - 1;

					for (var i = 0; i < projectLocales.Count; i++)
					{
						var locale = projectLocales[i];
						var toggle = toggles[i + 1];

						var totalRect = GUILayoutUtility.GetRect(
							0,
							LINE_HEIGHT,
							GUILayoutOptions.ExpandWidth().ExpandHeight(false));

						if (locale != null && !LocaleTableMap.ContainsKey(locale))
						{
							GUIHelper.PushGUIEnabled(false);
							{
								DrawLocaleToggle(ref totalRect, toggle, null);
							}
							GUIHelper.PopGUIEnabled();

							LocalizationTable looseTable = null;

							foreach (var localizationTable in LooseTables)
								if (localizationTable.LocaleIdentifier == locale.Identifier)
								{
									looseTable = localizationTable;
									break;
								}

							var buttonRect = totalRect.TakeFromRight(80);

							if (looseTable != null)
							{
								if (GUI.Button(buttonRect, "Add"))
								{
									Collection.AddTable(looseTable);
									Undo.ClearUndo(looseTable);
									Undo.ClearUndo(Collection);
									FancyColor.PopBlend();
									GUIHelper.ExitGUI(false);
								}
							}
							else
							{
								if (GUI.Button(buttonRect, "Create"))
								{
									Collection.AddNewTable(locale.Identifier);
									Undo.ClearUndo(Collection);
									FancyColor.PopBlend();
									GUIHelper.ExitGUI(false);
								}
							}

							if (i != lastLocaleIndex) GUILayout.Space(LOCALE_SPACING);

							continue;
						}


						var table = locale == null ? KeyTable : LocaleTableMap[locale];

						table.IsVisible = DrawLocaleToggle(ref totalRect, toggle, table);

						if (table.Type != OdinGUITable<TTable>.GUITableType.Key)
						{
							var removeLocaleRect = totalRect.TakeFromRight(80);

							if (GUI.Button(removeLocaleRect, "Remove"))
								if (EditorUtility.DisplayDialog(
										"Odin Localization Editor",
										$"Are you sure you want to remove the locale '{locale.Identifier.CultureInfo.EnglishName}' from '{Collection.name}'?\n" +
										"This can have side effects that can't be undone.",
										"Yes",
										"No"))
								{
									Collection.RemoveTable(table.Asset);
									Undo.ClearUndo(table.Asset);
									Undo.ClearUndo(Collection);
									FancyColor.PopBlend();
									GUIHelper.ExitGUI(false);
								}
						}

						if (EntryScrollView.IsBeyondHorizontalBounds && table.IsVisible && !table.IsPinned)
							if (Event.current.OnMouseDown(totalRect, 0))
								MoveScrollPositionToTable(table);

						if (i != lastLocaleIndex) GUILayout.Space(LOCALE_SPACING);
					}
				}
				GUILayout.EndScrollView();

				GUILayout.Space(4);
				SirenixEditorGUI.HorizontalLineSeparator();
				GUILayout.Space(4);

				if (SirenixEditorGUI.Button("Manage Locales", ButtonSizes.Medium))
					try
					{
						TwoWaySerializationBinder.Default.BindToType(
								"UnityEditor.Localization.UI.LocaleGeneratorWindow, Unity.Localization.Editor")?
							.GetMethod("ShowWindow", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, null);
					}
					catch (NullReferenceException nullReferenceException)
					{
						Debug.LogError(
							$"[Odin]: Failed to find LocaleGeneratorWindow.ShowWindow.\n{nullReferenceException.Message}");
					}
			}
			GUILayout.EndArea();
		}

		private bool DrawLocaleToggle(ref Rect rect, Toggle toggle, OdinGUITable<TTable> table)
		{
			const int toggleWidth = 35;

			var toggleRect = rect.TakeFromLeft(toggleWidth).SubXMax(4).VerticalPadding(2).AddY(1);
			var color = GUI.enabled ? toggle.CurrentColor : new Color(0.35f, 0.35f, 0.35f);
			toggle.Enabled = GUI.enabled;

			// Draw toggle background
			GUI.DrawTexture(
				toggleRect,
				Texture2D.whiteTexture,
				ScaleMode.StretchToFill,
				false,
				1f,
				BackgroundColorGrayscale.Blend(color, FancyColor.BlendMode.Multiply),
				0,
				float.MaxValue);

			// Draw toggle thumb
			GUI.DrawTexture(
				toggle.CurrentThumbRect,
				Texture2D.whiteTexture,
				ScaleMode.StretchToFill,
				false,
				1f,
				ThumbColorGrayscale.Blend(color, FancyColor.BlendMode.Multiply),
				0,
				float.MaxValue);

			AnimateThumb(toggleRect, toggle);

			GUI.Label(
				rect, toggle.Label,
				GUI.enabled && Event.current.IsMouseOver(rect) ? SirenixGUIStyles.WhiteLabel : SirenixGUIStyles.Label);

			if (GUI.enabled && Event.current.OnMouseDown(toggleRect, 0))
			{
				dragging = true;
				lastChangedToggle = toggle;
				newValue = !toggle.Toggled;
				toggle.Toggled = newValue;

				switch (Event.current.modifiers)
				{
					case EventModifiers.Control:
						for (var i = 0; i < toggles.Count; i++)
						{
							if (!toggles[i].Enabled) continue;
							toggles[i].Toggled = newValue;
						}

						break;
					case EventModifiers.Shift:
						for (var i = 0; i < toggles.Count; i++)
						{
							if (!toggles[i].Enabled) continue;
							toggles[i].Toggled = table.IsVisible;
						}

						break;
					case EventModifiers.Alt:
						for (var i = 0; i < toggles.Count; i++)
						{
							if (!toggles[i].Enabled) continue;
							toggles[i].Toggled = toggles[i] == toggle;
						}

						break;
				}
			}

			if (GUI.enabled && dragging)
			{
				var mp = Event.current.mousePosition;
				if (toggle != lastChangedToggle && toggleRect.y < mp.y && toggleRect.yMax > mp.y)
				{
					lastChangedToggle = toggle;
					toggle.Toggled = newValue;
				}

				GUIHelper.RequestRepaint();
			}

			if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
			{
				dragging = false;
				GUIHelper.RequestRepaint();
			}

			return toggle.Toggled;
		}

		private static void AnimateThumb(Rect toggleRect, Toggle toggle)
		{
			const float thumbAnimationDurationInSeconds = 0.07f;
			const float
				thumbAnimationSpeed =
					1f / (thumbAnimationDurationInSeconds /
						2f); // divided by 2 since the animation is split into 2 phases.
			const float colorAnimationDurationInSeconds = 0.6f;
			const float colorAnimationSpeed = 1f / colorAnimationDurationInSeconds;

			var targetRect = toggle.Toggled ?
				toggleRect.AlignRight(toggleRect.height).AlignCenterY(toggleRect.height).Padding(2) :
				toggleRect.AlignLeft(toggleRect.height).AlignCenterY(toggleRect.height).Padding(2);

			var targetColor = toggle.Toggled ? EnabledColor : DisabledColor;

			if (toggle.CurrentColor != (Color) targetColor)
			{
				if (toggle.T1.IsDone) toggle.T1.Reset(0f);

				toggle.T1.Move(colorAnimationSpeed, Easing.InOutExpo);
				toggle.CurrentColor = Color.Lerp(toggle.StartColor, targetColor, toggle.T1.GetValue());
			}

			if (Event.current.type == EventType.Repaint && toggle.CurrentThumbRect == Rect.zero)
			{
				toggle.CurrentThumbRect = targetRect;
				toggle.CurrentColor = targetColor;
				GUIHelper.RequestRepaint();
			}

			if (Event.current.type == EventType.Repaint && toggle.CurrentThumbRect != targetRect)
			{
				if (toggle.Toggled)
				{
					if (toggle.CurrentThumbRect.xMax < targetRect.xMax)
					{
						if (toggle.T2.IsDone) toggle.T2.Reset(0f);

						toggle.T2.Move(thumbAnimationSpeed);
						var xMax = Mathf.Lerp(toggle.StartXMax, targetRect.xMax, toggle.T2.GetValue());
						toggle.CurrentThumbRect.xMax = xMax;
					}
					else if (toggle.CurrentThumbRect.xMin < targetRect.xMin)
					{
						if (toggle.T2.IsDone) toggle.T2.Reset(0f);

						toggle.T2.Move(thumbAnimationSpeed);
						var xMin = Mathf.Lerp(toggle.StartXMin, targetRect.xMin, toggle.T2.GetValue());
						toggle.CurrentThumbRect.xMin = xMin;
					}
				}
				else
				{
					if (toggle.CurrentThumbRect.xMin > targetRect.xMin)
					{
						if (toggle.T2.IsDone) toggle.T2.Reset(0f);

						toggle.T2.Move(thumbAnimationSpeed);
						var xMin = Mathf.Lerp(toggle.StartXMin, targetRect.xMin, toggle.T2.GetValue());
						toggle.CurrentThumbRect.xMin = xMin;
					}
					else if (toggle.CurrentThumbRect.xMax > targetRect.xMax)
					{
						if (toggle.T2.IsDone) toggle.T2.Reset(0f);

						toggle.T2.Move(thumbAnimationSpeed);
						var xMax = Mathf.Lerp(toggle.StartXMax, targetRect.xMax, toggle.T2.GetValue());
						toggle.CurrentThumbRect.xMax = xMax;
					}
				}

				GUIHelper.RequestRepaint();
			}
		}

		private void LocaleToggle(Rect position, Locale locale)
		{
			if (locale != null && !LocaleTableMap.ContainsKey(locale))
			{
				var createLocaleRect = position.TakeFromRight(80);

				GUIHelper.PushGUIEnabled(false);
				{
					GUI.Toggle(position.TakeFromLeft(GUI.skin.toggle.padding.left), false, GUIContent.none);

					GUI.Label(position, locale.LocaleName, SirenixGUIStyles.Label);
				}
				GUIHelper.PopGUIEnabled();

				LocalizationTable looseTable = null;

				foreach (var localizationTable in LooseTables)
					if (localizationTable.LocaleIdentifier == locale.Identifier)
					{
						looseTable = localizationTable;
						break;
					}

				if (looseTable != null)
				{
					if (GUI.Button(createLocaleRect, "Add"))
					{
						Collection.AddTable(looseTable, true);
						FancyColor.PopBlend();
						GUIHelper.ExitGUI(false);
					}
				}
				else
				{
					if (GUI.Button(createLocaleRect, "Create"))
					{
						Collection.AddNewTable(locale.Identifier);
						FancyColor.PopBlend();
						GUIHelper.ExitGUI(false);
					}
				}

				return;
			}

			var table = locale == null ? KeyTable : LocaleTableMap[locale];

			EditorGUI.BeginChangeCheck();
			{
				table.IsVisible = GUI.Toggle(
					position.TakeFromLeft(GUI.skin.toggle.padding.left), table.IsVisible, GUIContent.none);
			}
			if (EditorGUI.EndChangeCheck())
				switch (Event.current.modifiers)
				{
					case EventModifiers.Shift:
						for (var i = 0; i < GUITables.Count; i++) GUITables[i].IsVisible = table.IsVisible;

						break;

					case EventModifiers.Alt:
						for (var i = 0; i < GUITables.Count; i++) GUITables[i].IsVisible = GUITables[i] == table;

						break;
				}

			if (table.Type != OdinGUITable<TTable>.GUITableType.Key)
			{
				var removeLocaleRect = position.TakeFromRight(80);

				if (GUI.Button(removeLocaleRect, "Remove"))
				{
					Collection.RemoveTable(table.Asset, true);
					FancyColor.PopBlend();
					GUIHelper.ExitGUI(false);
				}
			}

			if (EntryScrollView.IsBeyondHorizontalBounds && table.IsVisible && !table.IsPinned)
			{
				var isMouseOver = Event.current.IsMouseOver(position);

				GUI.Label(
					position, table.DisplayName, isMouseOver ? SirenixGUIStyles.WhiteLabel : SirenixGUIStyles.Label);

				if (Event.current.OnMouseDown(position, 0)) MoveScrollPositionToTable(table);
			}
			else
			{
				GUI.Label(position, table.DisplayName, SirenixGUIStyles.Label);
			}
		}

		protected bool IsDraggingAnything()
		{
			if (EntryScrollView.IsDraggingMouse ||
				EntryScrollView.IsDraggingHorizontalScrollBar ||
				EntryScrollView.IsDraggingVerticalScrollBar)
				return true;

			for (var i = 0; i < GUITables.Count; i++)
				if (GUITables[i].IsDraggingSlider)
					return true;

			return false;
		}

#if false
		private void DrawBottomTabTemplate(Rect position)
		{
			if (!this.Collection.SharedData.Metadata.HasMetadata<OdinTemplateMetadata>())
			{
				this.Collection.SharedData.Metadata.AddMetadata(new OdinTemplateMetadata());
				EditorUtility.SetDirty(this.Collection.SharedData);
			}

			var templateMetadata = this.Collection.SharedData.Metadata.GetMetadata<OdinTemplateMetadata>();
			
			GUILayout.BeginArea(position);
			{
				GUILayout.BeginScrollView(Vector2.zero);

				int removedItemIndex = -1;

				for (var i = 0; i < templateMetadata.MetadataExpected.Count; i++)
				{
					if (OdinLocalizationStyles.Metadata(templateMetadata.MetadataExpected[i], i == 0))
					{
						removedItemIndex = i;
					}
				}

				GUILayout.EndScrollView();

				if (removedItemIndex != -1)
				{
					templateMetadata.MetadataExpected.RemoveAt(removedItemIndex);
					EditorUtility.SetDirty(this.Collection.SharedData);
				}

				Rect addMetadataRect = GUILayoutUtility.GetRect(0, (int) ButtonSizes.Large);

				if (GUI.Button(addMetadataRect, "Add Metadata"))
				{
					this.ShowAddMetadataTemplateSelector(addMetadataRect, templateMetadata);
				}

				GUILayoutUtility.GetRect(0, 5);
			}
			GUILayout.EndArea();
		}

		private void ShowAddMetadataTemplateSelector(Rect rect, OdinTemplateMetadata templateMetadata)
		{
			TypeSelector selector = this.MakeMetadataSelector();

			selector.SelectionConfirmed += types =>
			{
				foreach (Type type in types)
				{
					if (templateMetadata.MetadataExpected.Contains(type) && !OdinLocalizationMetadataRegistry.MetadataAllowsMultiple[type])
					{
						continue;
					}

					templateMetadata.MetadataExpected.Add(type);

					EditorUtility.SetDirty(this.Collection.SharedData);
				}
			};

			selector.ShowInPopup(rect);
		}

		private TypeSelector MakeMetadataSelector()
		{
			TypeSelector selector;

			switch (this.Collection)
			{
				case AssetTableCollection _:
					selector =
 new TypeSelector(OdinLocalizationMetadataRegistry.AssetEntryMetadataTypes, excludeInheritors: true);
					break;

				case StringTableCollection _:
					selector =
 new TypeSelector(OdinLocalizationMetadataRegistry.StringEntryMetadataTypes, excludeInheritors: true);
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			return selector;
		}

		protected bool HasMetadataAmountOfType(IList<IMetadata> metadata, Type metadataType, OdinTemplateMetadata templateMetadata)
		{
			int count = CountMetadataType(metadata, metadataType);

			var expectedCount = 0;

			for (var i = 0; i < templateMetadata.MetadataExpected.Count; i++)
			{
				if (templateMetadata.MetadataExpected[i] == metadataType)
				{
					expectedCount++;
				}
			}

			return count >= expectedCount;
		}

		protected static int CountMetadataType(IList<IMetadata> metadata, Type metadataType)
		{
			var result = 0;

			for (var i = 0; i < metadata.Count; i++)
			{
				if (metadata[i].GetType() == metadataType)
				{
					result++;
				}
			}

			return result;
		}
#endif


		private void DrawBottomTabSettings(Rect position)
		{
			position = position.Padding(6);

			GUILayout.BeginArea(position);
			{
				// Table Collection Name
				{
					var namePosition = EditorGUILayout.GetControlRect();

					GUI.Label(namePosition.TakeFromLeft(130), "Collection Name");

					EditorGUI.BeginChangeCheck();

					var value = SirenixEditorFields.DelayedTextField(
						namePosition, Collection.SharedData.TableCollectionName);

					if (EditorGUI.EndChangeCheck())
						if (!string.IsNullOrEmpty(value) &&
							OdinLocalizationEditorSettings.IsTableNameValid(Collection.GetType(), value))
						{
							Collection.SetTableCollectionName(value, true);
							MenuItem.Name = Collection.SharedData.TableCollectionName;
							MenuItem.Select();
						}
				}

				// Preload All Tables
				{
					EditorGUI.BeginChangeCheck();

					var value = GUILayout.Toggle(Collection.IsPreloadTableFlagSet(), "Preload All Tables");

					if (EditorGUI.EndChangeCheck()) Collection.SetPreloadTableFlag(value, true);
				}

				GUILayout.Space(4);
				SirenixEditorGUI.HorizontalLineSeparator();
				GUILayout.Space(4);

				if (SirenixEditorGUI.Button("Manage Collection", ButtonSizes.Large))
					GUIHelper.OpenInspectorWindow(Collection);
			}
			GUILayout.EndArea();
		}

		private float VerticalSlideRect(Rect rect, bool connect)
		{
			var offset = SirenixEditorGUI.SlideRect(rect, MouseCursor.SplitResizeLeftRight).x;

			var slideThumbColor = Event.current.IsMouseOver(rect)
				?
				EditorGUIUtility.isProSkin ? new Color(0.5f, 0.5f, 0.5f) : new Color(1f, 1f, 1f) :
				WindowState.RightMenuWidth > 0 ?
					EditorGUIUtility.isProSkin ? new Color(0.25f, 0.25f, 0.25f) : new Color(0.8f, 0.8f, 0.8f) :
					EditorGUIUtility.isProSkin ?
						new Color(0.4f, 0.4f, 0.4f)
						:
						new Color(1f, 1f, 1f);

			EditorGUI.DrawRect(
				rect, Event.current.IsMouseOver(rect)
					?
					EditorGUIUtility.isProSkin ? new Color(0.26f, 0.26f, 0.26f) : new Color(0.7f, 0.7f, 0.7f) :
					EditorGUIUtility.isProSkin ?
						new Color(0.2f, 0.2f, 0.2f)
						:
						new Color(0.6f, 0.6f, 0.6f));

			var h2 = connect ? WindowState.RightMenuTopPanelHeight : rect.height / 2f - 40;

			EditorGUI.DrawRect(rect.AlignLeft(1), new Color(0, 0, 0, 0.4f));
			var left = new Rect(rect.center.x - 1, 0, 1, rect.height);
			EditorGUI.DrawRect(left, slideThumbColor);

			if (!connect)
			{
				var right = new Rect(rect.center.x + 1, rect.y, 1, rect.height);
				EditorGUI.DrawRect(right, slideThumbColor);
				EditorGUI.DrawRect(rect.AlignRight(1), new Color(0, 0, 0, 0.4f));
			}
			else if (WindowState.RightMenuWidth > 0)
			{
				var crossTop = new Rect(
					rect.AlignCenterX(1).AddX(2).x, WindowState.RightMenuTopPanelHeight - (14 / 2 + 1), 4, 1);
				var crossBottom = crossTop.AddY(2);
				var rightTop = new Rect(rect.center.x + 1, 0, 1, crossTop.y + 1);
				var rightBottom = new Rect(rect.center.x + 1, crossBottom.y, 1, rect.height - crossBottom.y);
				EditorGUI.DrawRect(crossTop, slideThumbColor);
				EditorGUI.DrawRect(crossBottom, slideThumbColor);
				EditorGUI.DrawRect(rightTop, slideThumbColor);
				EditorGUI.DrawRect(rightBottom, slideThumbColor);
				EditorGUI.DrawRect(
					rect.AlignRight(1).SetHeight(connect ? h2 - 13 : rect.height), new Color(0, 0, 0, 0.4f));
				EditorGUI.DrawRect(rect.AlignRight(1).AddY(connect ? h2 - 1 : 0), new Color(0, 0, 0, 0.4f));
			}
			else
			{
				var right = new Rect(rect.center.x + 1, 0, 1, rect.height);
				EditorGUI.DrawRect(right, slideThumbColor);
				EditorGUI.DrawRect(rect.AlignRight(1), new Color(0, 0, 0, 0.4f));
			}

			return offset;
		}

		private float HorizontalSlideRect(Rect rect)
		{
			var offset = SirenixEditorGUI.SlideRect(rect, MouseCursor.SplitResizeUpDown).y;

			var slideThumbColor = Event.current.IsMouseOver(rect)
				?
				EditorGUIUtility.isProSkin ? new Color(0.5f, 0.5f, 0.5f) : new Color(1f, 1f, 1f) :
				WindowState.RightMenuWidth > 0 ?
					EditorGUIUtility.isProSkin ? new Color(0.25f, 0.25f, 0.25f) : new Color(0.8f, 0.8f, 0.8f) :
					EditorGUIUtility.isProSkin ?
						new Color(0.4f, 0.4f, 0.4f)
						:
						new Color(1f, 1f, 1f);

			EditorGUI.DrawRect(
				rect, Event.current.IsMouseOver(rect)
					?
					EditorGUIUtility.isProSkin ? new Color(0.26f, 0.26f, 0.26f) : new Color(0.7f, 0.7f, 0.7f) :
					EditorGUIUtility.isProSkin ?
						new Color(0.2f, 0.2f, 0.2f)
						:
						new Color(0.6f, 0.6f, 0.6f));

			var top = new Rect(rect.x, rect.center.y - 1, rect.width, 1);
			var bottom = new Rect(rect.x, rect.center.y + 1, rect.width, 1);

			EditorGUI.DrawRect(top, slideThumbColor);
			EditorGUI.DrawRect(bottom, slideThumbColor);
			EditorGUI.DrawRect(rect.AlignTop(1), new Color(0, 0, 0, 0.4f));
			EditorGUI.DrawRect(rect.AlignBottom(1), new Color(0, 0, 0, 0.4f));

			return offset;
		}
	}
}
