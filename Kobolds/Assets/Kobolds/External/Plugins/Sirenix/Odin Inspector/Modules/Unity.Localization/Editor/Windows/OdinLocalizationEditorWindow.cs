//-----------------------------------------------------------------------
// <copyright file="OdinLocalizationEditorWindow.cs" company="Sirenix ApS">
// Copyright (c) Sirenix ApS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
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

namespace Sirenix.OdinInspector.Modules.Localization.Editor
{
	public class OdinLocalizationCreateTableMenu
	{
		public enum TableCollectionType
		{
			StringTableCollection,
			AssetTableCollection
		}

		internal bool EnableFolder = true;

		[EnableIf("@this." + nameof(EnableFolder))]
		[InfoBox(
			"The directory is not found, this will create a new directory on creation.", nameof(ShowFolderInfoBox))]
		[HorizontalGroup("Split")]
		[VerticalGroup("Split/Left")]
		[FolderPath(ParentFolder = "Assets")]
		public string Folder;

		[HorizontalGroup("Split")]
		[VerticalGroup("Split/Right")]
		[InlineProperty]
		[ListDrawerSettings(
			ListElementLabelName = "@this.Locale.LocaleName",
			HideAddButton = true,
			HideRemoveButton = true,
			DefaultExpandedState = true,
			ShowFoldout = false,
			ShowItemCount = false,
			DraggableItems = false)]
		public List<LocaleItem> Locales = new();

		[ValidateInput(nameof(ValidateName), "@this." + nameof(nameErrorMessage))]
		[VerticalGroup("Split/Left")]
		[PropertySpace(SpaceAfter = 2, SpaceBefore = 2)]
		public string Name;

		private string nameErrorMessage = string.Empty;

		[VerticalGroup("Split/Left")]
		[PropertySpace(SpaceAfter = 2, SpaceBefore = 2)]
		[HideLabel]
		[EnumToggleButtons]
		public TableCollectionType Type;

		private string FolderPath => string.IsNullOrEmpty(Folder) ? "Assets" : $"Assets/{Folder}";

		[EnableIf(nameof(EnableCreateIf))]
		[VerticalGroup("Split/Left")]
		[PropertySpace(SpaceBefore = 4)]
		[Button(ButtonSizes.Large)]
		public void Create()
		{
			var localizationWindow = EditorWindow.focusedWindow as OdinLocalizationEditorWindow;

			if (!HasAnyLocaleSelected())
			{
				if (localizationWindow)
					localizationWindow.ShowToast(
						ToastPosition.BottomLeft,
						SdfIconType.ExclamationOctagonFill,
						"At least 1 Locale must be selected.",
						new Color(0.68f, 0.2f, 0.2f),
						5.0f);

				return;
			}

			if (!Directory.Exists(FolderPath)) Directory.CreateDirectory(FolderPath);

			var collectionLocales = new List<Locale>(Locales.Count);

			foreach (var localeItem in Locales)
				if (localeItem.Enabled)
					collectionLocales.Add(localeItem.Locale);

			switch (Type)
			{
				case TableCollectionType.StringTableCollection:
					LocalizationEditorSettings.CreateStringTableCollection(Name, FolderPath, collectionLocales);
					break;

				case TableCollectionType.AssetTableCollection:
					LocalizationEditorSettings.CreateAssetTableCollection(Name, FolderPath, collectionLocales);
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			if (localizationWindow)
			{
				string typeNiceName;

				switch (Type)
				{
					case TableCollectionType.StringTableCollection:
						typeNiceName = "String Table Collection";
						break;

					case TableCollectionType.AssetTableCollection:
						typeNiceName = "Asset Table Collection";
						break;

					default:
						throw new ArgumentOutOfRangeException();
				}

				localizationWindow.ShowToast(
					ToastPosition.BottomLeft,
					SdfIconType.Check2,
					$"{typeNiceName} '{Name}' created at: {FolderPath}.",
					new Color(0.29f, 0.57f, 0.42f),
					16.0f);
			}
		}

		[HorizontalGroup("Split/Right/Split")]
		[Button]
		public void LocaleGenerator()
		{
			try
			{
				TwoWaySerializationBinder.Default.BindToType(
						"UnityEditor.Localization.UI.LocaleGeneratorWindow, Unity.Localization.Editor")
					.GetMethod("ShowWindow", BindingFlags.Static | BindingFlags.Public)
					.Invoke(null, null);
			}
			catch (NullReferenceException nullReferenceException)
			{
				Debug.LogError(
					$"[Odin]: Failed to find LocaleGeneratorWindow.ShowWindow.\n{nullReferenceException.Message}");
			}
		}

		[HorizontalGroup("Split/Right/Split")]
		[Button]
		public void SelectNone()
		{
			for (var i = 0; i < Locales.Count; i++) Locales[i].Enabled = false;
		}

		[HorizontalGroup("Split/Right/Split")]
		[Button]
		public void SelectAll()
		{
			for (var i = 0; i < Locales.Count; i++) Locales[i].Enabled = true;
		}

		private bool ValidateName(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				nameErrorMessage = $"{nameof(Name)} can't be empty.";
				return false;
			}

			Type collectionType;

			switch (Type)
			{
				case TableCollectionType.StringTableCollection:
					collectionType = typeof(StringTableCollection);
					break;

				case TableCollectionType.AssetTableCollection:
					collectionType = typeof(AssetTableCollection);
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			var isTableNameValid = OdinLocalizationEditorSettings.IsTableNameValid(
				collectionType, name, out var localizationErrorMsg);

			if (isTableNameValid) return true;

			nameErrorMessage = localizationErrorMsg;

			return false;
		}

		private bool ShowFolderInfoBox()
		{
			if (string.IsNullOrEmpty(Folder)) return false;

			return !Directory.Exists(FolderPath);
		}

		private bool EnableCreateIf()
		{
			return Locales.Count > 0 && ValidateName(Name);
		}

		private bool HasAnyLocaleSelected()
		{
			for (var i = 0; i < Locales.Count; i++)
				if (Locales[i].Enabled)
					return true;

			return false;
		}

		[Serializable]
		public class LocaleItem
		{
			[HideInInspector]
			public Locale Locale;

			[HideLabel]
			public bool Enabled;
		}
	}

	public class OdinLocalizationEditorWindow : OdinMenuEditorWindow, IDisposable
	{
		public enum RightMenuBottomTabs
		{
			[LabelText(SdfIconType.FlagFill)]
			Locale,

#if false
			[LabelText(SdfIconType.BorderWidth)]
			Template,
#endif

			[LabelText(SdfIconType.GearFill)]
			Settings
		}

		public enum RightMenuTopTabs
		{
			[LabelText(SdfIconType.Braces)]
			Metadata,

			[LabelText(SdfIconType.GearFill)]
			Settings
		}

		private object lastSelection;

		public WindowState State;

		protected override void OnDisable()
		{
			base.OnDisable();
			State.Save();

			DisposeActiveCollection();
			State.Dispose();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			DisposeActiveCollection();
			State.Dispose();
		}

		public void Dispose()
		{
			DisposeActiveCollection();
			State.Dispose();
		}

		[MenuItem("Tools/Odin/Localization Editor", priority = 10_100)]
		public static void OpenFromMenu()
		{
			var wnd = GetWindow<OdinLocalizationEditorWindow>();
			wnd.MenuWidth = 300.0f;
		}

		protected override void Initialize()
		{
			State = new WindowState();
			State.Load();
		}

		protected override void OnImGUI()
		{
			if (LocalizationEditorSettings.ActiveLocalizationSettings == null)
			{
				var popupPosition = position.SetPosition(Vector2.zero).AlignCenter(360, 160);

				if (EditorGUIUtility.isProSkin)
				{
					//OdinLocalizationGUI.DrawRoundGlowRect(popupPosition.Expand(54), FancyColor.CreateHex(0x323232));
					OdinLocalizationGUI.DrawRoundBlur6(popupPosition, new Color(0, 0, 0, 0.025f));
					SirenixEditorGUI.DrawRoundRect(popupPosition, FancyColor.CreateHex(0x383838), 5.0f);
				}
				else
				{
					OdinLocalizationGUI.DrawRoundBlur6(popupPosition, new Color(0, 0, 0, 0.02f));
					SirenixEditorGUI.DrawRoundRect(
						popupPosition, new FancyColor(0.84f), 5.0f); //, new Color(0, 0, 0, 0.2f), 1);

					SirenixEditorGUI.DrawRoundRect(
						popupPosition.AlignBottom(32 + 12 + 8 + 6), new Color(1, 1, 1, 0.2f), 0.0f, 0.0f, 5.0f, 5.0f);
				}

				popupPosition = popupPosition.Padding(12);

				var buttonsArea = popupPosition.TakeFromBottom(32);

				popupPosition.height -= 16;

				var labelStyle = EditorGUIUtility.isProSkin ?
					SirenixGUIStyles.WhiteLabelCentered :
					SirenixGUIStyles.BlackLabelCentered;

				if (EditorGUIUtility.isProSkin)
				{
					GUI.Label(popupPosition, "No Localization Settings found in project.", labelStyle);
				}
				else
				{
					GUIHelper.PushColor(new Color(1, 1, 1, 0.75f));
					GUI.Label(
						popupPosition, "No Localization Settings found in project.",
						SirenixGUIStyles.BlackLabelCentered);
				}

				if (OdinLocalizationGUI.OverlaidButton(
						buttonsArea.AlignCenter(120), "Create", labelStyle: labelStyle, invert: true))
					if (OdinLocalizationEditorSettings.CreateDefaultLocalizationSettingsAsset())
						ShowToast(
							ToastPosition.BottomLeft,
							SdfIconType.GearWide,
							"Default Localization Settings created.",
							new Color(0.13f, 0.26f, 0.39f),
							8.0f);

				if (!EditorGUIUtility.isProSkin) GUIHelper.PopColor();

				Repaint();
				return;
			}

			base.OnImGUI();
		}

		protected override OdinMenuTree BuildMenuTree()
		{
			var tree = new OdinMenuTree
			{
				Config =
				{
					AutoHandleKeyboardNavigation = false,
					DrawSearchToolbar = true
				},
				DefaultMenuStyle =
				{
					Height = 28,
					AlignTriangleLeft = true,
					TrianglePadding = 0.0f
				}
			};

			MenuBackgroundColor = OdinLocalizationGUI.MenuBackground;

			if (LocalizationEditorSettings.ActiveLocalizationSettings == null) return tree;

			var createMenu = new OdinLocalizationCreateTableMenu();

			tree.Add("Create Table", createMenu, SdfIconType.Plus);
			tree.Add("User Config", OdinLocalizationConfig.Instance, SdfIconType.GearFill);

#if true
			tree.Selection.SelectionChanged += type =>
			{
				switch (type)
				{
					case SelectionChangedType.ItemAdded:
						if (lastSelection != null)
						{
							switch (lastSelection)
							{
								case OdinAssetTableCollectionEditor assetCollection:
								{
									assetCollection.DetachEvents();
									break;
								}

								case OdinStringTableCollectionEditor stringCollection:
								{
									stringCollection.DetachEvents();
									break;
								}
							}

							State.MetadataTree?.Dispose();
							State.MetadataTree = null;
						}

						switch (tree.Selection.SelectedValue)
						{
							case OdinAssetTableCollectionEditor assetCollection:
							{
								assetCollection.OnSelectInWindow();

								if (assetCollection.SelectionType == OdinTableSelectionType.TableEntry &&
									State.CurrentTopTab == RightMenuTopTabs.Metadata)
									assetCollection.UpdateMetadataViewForEntry(assetCollection.CurrentSelectedEntry);

								break;
							}

							case OdinStringTableCollectionEditor stringCollection:
							{
								stringCollection.OnSelectInWindow();

								if (stringCollection.SelectionType == OdinTableSelectionType.TableEntry &&
									State.CurrentTopTab == RightMenuTopTabs.Metadata)
									stringCollection.UpdateMetadataViewForEntry(stringCollection.CurrentSelectedEntry);

								break;
							}

							case OdinLocalizationCreateTableMenu createTableMenu:
								createTableMenu.Locales.Clear();

								foreach (var locale in LocalizationEditorSettings.GetLocales())
									createTableMenu.Locales.Add(
										new OdinLocalizationCreateTableMenu.LocaleItem
											{Locale = locale, Enabled = true});

								break;
						}

						lastSelection = MenuTree.Selection.SelectedValue;

						break;
				}
			};
#endif

			var collectionGUIDs = AssetDatabase.FindAssets($"t:{nameof(LocalizationTableCollection)}");

			for (var i = 0; i < collectionGUIDs.Length; i++)
			{
				var assetPath = AssetDatabase.GUIDToAssetPath(collectionGUIDs[i]);

				var collection = AssetDatabase.LoadAssetAtPath<LocalizationTableCollection>(assetPath);

				var assetTableCollection =
					LocalizationEditorSettings.GetAssetTableCollection(collection.TableCollectionNameReference);

				if (assetTableCollection != null)
				{
					var guiCollection = new OdinAssetTableCollectionEditor(assetTableCollection, this, State);

					assetPath = assetPath.Replace(".asset", string.Empty);

					if (assetPath.StartsWith("Assets/")) assetPath = assetPath.Remove(0, "Assets/".Length);

					tree.Add(assetPath, guiCollection, SdfIconType.Table);

					continue;
				}

				var stringTableCollection =
					LocalizationEditorSettings.GetStringTableCollection(collection.TableCollectionNameReference);

				if (stringTableCollection != null)
				{
					var guiCollection = new OdinStringTableCollectionEditor(stringTableCollection, this, State);

					assetPath = assetPath.Replace(".asset", string.Empty);

					if (assetPath.StartsWith("Assets/")) assetPath = assetPath.Remove(0, "Assets/".Length);

					tree.Add(assetPath, guiCollection, SdfIconType.LayoutTextWindow);
				}
			}

			foreach (var treeMenuItem in tree.EnumerateTree())
			{
				if (treeMenuItem.Value != null)
				{
					if (treeMenuItem.Value is OdinAssetTableCollectionEditor assetEditor)
					{
						treeMenuItem.Name = assetEditor.Collection.SharedData.TableCollectionName;

						assetEditor.MenuItem = treeMenuItem;

						treeMenuItem.OnDrawItem += item =>
						{
							if (Event.current.OnMouseDown(item.Rect, 0, false))
								if (Event.current.clickCount > 1)
									EditorGUIUtility.PingObject(assetEditor.Collection);
						};

						continue;
					}

					if (treeMenuItem.Value is OdinStringTableCollectionEditor stringEditor)
					{
						treeMenuItem.Name = stringEditor.Collection.SharedData.TableCollectionName;

						stringEditor.MenuItem = treeMenuItem;

						treeMenuItem.OnDrawItem += item =>
						{
							if (Event.current.OnMouseDown(item.Rect, 0, false))
								if (Event.current.clickCount > 1)
									EditorGUIUtility.PingObject(stringEditor.Collection);
						};
					}

					continue;
				}

				treeMenuItem.Value = createMenu;

				treeMenuItem.SdfIcon = SdfIconType.FolderFill;

				treeMenuItem.OnDrawItem += item =>
				{
					var addTableRect = item.Rect.AlignRight(20).SubX(14);

					var isMouseOver = Event.current.IsMouseOver(addTableRect);

					if (EditorGUIUtility.isProSkin)
						SdfIcons.DrawIcon(
							addTableRect.AlignCenter(16, 16),
							SdfIconType.Plus,
							isMouseOver ? new Color(1, 1, 1, 0.8f) : new Color(1, 1, 1, 0.4f));
					else
						SdfIcons.DrawIcon(
							addTableRect.AlignCenter(16, 16),
							SdfIconType.Plus,
							isMouseOver ? new Color(0, 0, 0, 0.8f) : new Color(0, 0, 0, 0.4f));

					if (Event.current.OnMouseDown(item.Rect, 0, false)) createMenu.Folder = treeMenuItem.GetFullPath();
				};
			}

			return tree;
		}

		private void DisposeActiveCollection()
		{
			if (MenuTree == null) return;

			switch (MenuTree.Selection.SelectedValue)
			{
				case OdinAssetTableCollectionEditor assetCollection:
					assetCollection.DetachEvents();
					break;

				case OdinStringTableCollectionEditor stringCollection:
					stringCollection.DetachEvents();
					break;
			}
		}

		public class WindowState : IDisposable
		{
			public static string EditorPrefsKey = "OdinLocalizationEditorWindow_EditorPrefs";
			public RightMenuBottomTabs CurrentBottomTab;

			public RightMenuTopTabs CurrentTopTab;
			public float LastOpenRightMenuWidth;

			public float LeftMenuWidth;
			public PropertyTree MetadataTree;
			public float RightMenuTopPanelHeight;
			public float RightMenuWidth;
			public bool ShowSharedMetadata = true;

			public void Dispose()
			{
				MetadataTree?.Dispose();
				MetadataTree = null;
			}

			public void Save()
			{
				EditorPrefs.SetFloat($"{EditorPrefsKey}_LeftMenuWidth", LeftMenuWidth);
				EditorPrefs.SetFloat($"{EditorPrefsKey}_RightMenuWidth", RightMenuWidth);
				EditorPrefs.SetFloat($"{EditorPrefsKey}_RightMenuTopHeight", RightMenuTopPanelHeight);
				EditorPrefs.SetFloat($"{EditorPrefsKey}_LastOpenRightMenuWidth", LastOpenRightMenuWidth);
			}

			public void Load()
			{
				LeftMenuWidth = EditorPrefs.GetFloat($"{EditorPrefsKey}_LeftMenuWidth", 300);
				RightMenuWidth = EditorPrefs.GetFloat($"{EditorPrefsKey}_RightMenuWidth", 300);
				RightMenuTopPanelHeight = EditorPrefs.GetFloat($"{EditorPrefsKey}_RightMenuTopHeight");
				LastOpenRightMenuWidth = EditorPrefs.GetFloat($"{EditorPrefsKey}_LastOpenRightMenuWidth");
			}
		}
	}
}
