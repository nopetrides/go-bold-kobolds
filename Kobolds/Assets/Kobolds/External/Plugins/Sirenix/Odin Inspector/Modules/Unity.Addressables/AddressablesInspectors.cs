//-----------------------------------------------------------------------
// <copyright file="AddressablesInspectors.cs" company="Sirenix ApS">
// Copyright (c) Sirenix ApS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

#if !SIRENIX_INTERNAL
#pragma warning disable
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Modules.Addressables.Editor.Internal;
using Sirenix.Reflection.Editor;
using Sirenix.Serialization;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.GUI;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

[assembly: RegisterAssetReferenceAttributeForwardToChild(typeof(InlineEditorAttribute))]
[assembly: RegisterAssetReferenceAttributeForwardToChild(typeof(PreviewFieldAttribute))]

namespace Sirenix.OdinInspector
{
	/// <summary>
	///     <para>
	///         DisallowAddressableSubAssetField is used on AssetReference properties, and disallows and prevents assigned
	///         sub-assets to the asset reference.
	///     </para>
	/// </summary>
	/// <example>
	///     <code>
	/// [DisallowAddressableSubAssetField]
	/// public AssetReference Reference;
	/// </code>
	/// </example>
	/// <seealso cref="RegisterAssetReferenceAttributeForwardToChildAttribute" />
	/// <seealso cref="AssetReferenceUILabelRestriction" />
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(
		AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.ReturnValue | AttributeTargets.Parameter)]
	public class DisallowAddressableSubAssetFieldAttribute : Attribute
	{
	}

	/// <summary>
	///     <para>
	///         Registers an attribute to be applied to an AssetRefenece property, to be forwarded and applied to the
	///         AssetReference's child instead.
	///     </para>
	///     <para>
	///         This allows attributes designed for use on UnityEngine.Objects to be used on AssetReference properties as
	///         well.
	///     </para>
	///     <para>By default, <c>InlineEditorAttribute</c> and <c>PreviewFieldAttribute</c> are registered for forwarding.</para>
	/// </summary>
	/// <example>
	///     <code>
	/// [assembly: Sirenix.OdinInspector.Modules.RegisterAssetReferenceAttributeForwardToChild(typeof(InlineEditorAttribute))]
	/// [assembly: Sirenix.OdinInspector.Modules.RegisterAssetReferenceAttributeForwardToChild(typeof(PreviewFieldAttribute))]
	/// </code>
	/// </example>
	/// <seealso cref="DisallowAddressableSubAssetFieldAttribute" />
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
	public class
		RegisterAssetReferenceAttributeForwardToChildAttribute : Attribute // TODO: Should this be a global attribute?
	{
		/// <summary>
		///     The type of the attribute to forward.
		/// </summary>
		public readonly Type AttributeType;

		/// <summary>
		///     Registers the specified attribute to be copied and applied to the AssetReference's UnityEngine.Object child
		///     instead.
		/// </summary>
		/// <param name="attributeType">The attribute type to forward.</param>
		public RegisterAssetReferenceAttributeForwardToChildAttribute(Type attributeType)
		{
			AttributeType = attributeType;
		}
	}
}

#if UNITY_EDITOR
namespace Sirenix.OdinInspector.Modules.Addressables.Editor
{
	/// <summary>
	///     Draws an AssetReference property.
	/// </summary>
	/// <typeparam name="T">The concrete type of AssetReference to be drawn. For example, <c>AssetReferenceTexture</c>.</typeparam>
	[DrawerPriority(0, 1)]
	public class AssetReferenceDrawer<T> : OdinValueDrawer<T>, IDefinesGenericMenuItems
		where T : AssetReference
	{
		private bool disallowSubAssets_Backing;
		private bool hideAssetReferenceField;

		private bool isSpriteAtlas;

		private string lastGuid;
		private string NoneSelectedLabel;

		private List<AssetReferenceUIRestriction> restrictions;

		//private string[] labelRestrictions;
		private bool showSubAssetField;
		private Type targetType;
		private bool targetTypeIsNotValidMainAsset;

		private bool updateShowSubAssetField;
		private Type[] validMainAssetTypes;

		private bool ActuallyDisallowSubAssets => disallowSubAssets_Backing && !targetTypeIsNotValidMainAsset;

		public void PopulateGenericMenu(InspectorProperty property, GenericMenu genericMenu)
		{
			genericMenu.AddItem(
				new GUIContent("Set To Null"), false, () =>
				{
					EnsureNotRealNull();

					SetMainAsset(null);
				});

			if (ValueEntry.SmartValue != null && string.IsNullOrEmpty(ValueEntry.SmartValue.SubObjectName) == false)
				genericMenu.AddItem(
					new GUIContent("Remove Sub Asset"), false, () =>
					{
						EnsureNotRealNull();

						SetSubAsset(null);
					});
			else
				genericMenu.AddDisabledItem(new GUIContent("Remove Sub Asset"));

			genericMenu.AddItem(new GUIContent("Open Groups Window"), false, OdinAddressableUtility.OpenGroupsWindow);
		}

		protected override bool CanDrawValueProperty(InspectorProperty property)
		{
			return property.GetAttribute<DrawWithUnityAttribute>() == null;
		}

		protected override void Initialize()
		{
			// If a child exists, we draw that child instead of the AssetReference field.
			if (Property.Children.Count > 0)
			{
				hideAssetReferenceField = true;
				return;
			}

			EnsureNotRealNull();

			validMainAssetTypes = OdinAddressableUtility.GetAssetReferenceValidMainAssetTypes(typeof(T));
			targetType = OdinAddressableUtility.GetAssetReferenceTargetType(typeof(T));
			targetTypeIsNotValidMainAsset = validMainAssetTypes.Contains(targetType) == false;

			isSpriteAtlas = validMainAssetTypes.Length > 0 && validMainAssetTypes[0] == typeof(SpriteAtlas);

			if (targetType == typeof(Object))
				NoneSelectedLabel = "None (Addressable Asset)";
			else if (validMainAssetTypes.Length > 1 || validMainAssetTypes[0] != targetType)
				NoneSelectedLabel =
					$"None (Addressable [{string.Join("/", validMainAssetTypes.Select(n => n.GetNiceName()))}]>{targetType.GetNiceName()})";
			else
				NoneSelectedLabel = $"None (Addressable {targetType.GetNiceName()})";

			restrictions = new List<AssetReferenceUIRestriction>();
			foreach (var attr in Property.Attributes)
				if (attr is AssetReferenceUIRestriction r)
					restrictions.Add(r);

			disallowSubAssets_Backing = Property.GetAttribute<DisallowAddressableSubAssetFieldAttribute>() != null;

			updateShowSubAssetField = true;
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			if (disallowSubAssets_Backing && targetTypeIsNotValidMainAsset)
				SirenixEditorGUI.WarningMessageBox(
					$"This {typeof(T).GetNiceName()} field has been marked as not allowing sub assets, but the target type '{targetType.GetNiceName()}' is not a valid main asset for {typeof(T).GetNiceName()}, so the target value *must* be a sub asset. Therefore sub assets have been enabled. (Valid main asset types for {typeof(T).GetNiceName()} are: {string.Join(", ", validMainAssetTypes.Select(t => t.GetNiceName()))})");

			if (hideAssetReferenceField == false)
			{
				var value = ValueEntry.SmartValue;

				if (lastGuid != ValueEntry.SmartValue?.AssetGUID) updateShowSubAssetField = true;

				lastGuid = ValueEntry.SmartValue?.AssetGUID;

				// Update showSubAssetField.
				if (updateShowSubAssetField && Event.current.type == EventType.Layout)
				{
					if (value == null || value.AssetGUID == null || value.editorAsset == null)
					{
						showSubAssetField = false;
					}
					else if (string.IsNullOrEmpty(value.SubObjectName) == false)
					{
						showSubAssetField = true;
					}
					else if (ActuallyDisallowSubAssets)
					{
						showSubAssetField = false;
					}
					else
					{
						var path = AssetDatabase.GUIDToAssetPath(value.AssetGUID);

						if (path == null)
						{
							showSubAssetField = false;
						}
						else
						{
							var mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
							showSubAssetField = OdinAddressableUtility
								.EnumerateAllActualAndVirtualSubAssets(mainAsset, path).Any();
						}
					}

					updateShowSubAssetField = false;
				}

				var rect = SirenixEditorGUI.GetFeatureRichControlRect(
					label, out var controlId, out _, out var valueRect);

				var mainRect = valueRect;
				Rect subRect = default, subPickerRect = default;

				if (showSubAssetField)
				{
					subRect = mainRect.Split(1, 2).AddX(1);
					mainRect = mainRect.Split(0, 2).SubXMax(1);
					subPickerRect = subRect.AlignRight(16);
				}

				var mainPickerRect = mainRect.AlignRight(16);

				// Cursor
				EditorGUIUtility.AddCursorRect(mainPickerRect, MouseCursor.Link);
				if (showSubAssetField) EditorGUIUtility.AddCursorRect(subPickerRect, MouseCursor.Link);

				// Selector
				if (GUI.Button(mainPickerRect, "", SirenixGUIStyles.None)) OpenMainAssetSelector(valueRect);
				if (showSubAssetField && GUI.Button(subPickerRect, "", SirenixGUIStyles.None))
					OpenSubAssetSelector(valueRect);

				// Ping
				if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
					mainRect.Contains(Event.current.mousePosition) && value != null &&
					value.editorAsset != null) EditorGUIUtility.PingObject(value.editorAsset);

				// Drag and drop
				EditorGUI.BeginChangeCheck();
				var drop = DragAndDropUtilities.DropZone(rect, null, typeof(object), false, controlId);
				if (EditorGUI.EndChangeCheck())
				{
					EnsureNotRealNull();

					if (ConvertToValidAssignment(drop, out var obj, out var isSubAssetAssignment))
					{
						if (isSpriteAtlas && obj is Sprite sprite)
						{
							foreach (var spriteAtlas in AssetDatabase_Internals.FindAssets<SpriteAtlas>(
										string.Empty, false, AssetDatabaseSearchArea.AllAssets))
							{
								if (!spriteAtlas.CanBindTo(sprite)) continue;

								SetMainAndSubAsset(spriteAtlas, sprite);

								break;
							}
						}
						else
						{
							if (isSubAssetAssignment)
							{
								var path = AssetDatabase.GetAssetPath(obj);

								var mainAsset = AssetDatabase.LoadMainAssetAtPath(path);

								if (mainAsset != null)
								{
									if (mainAsset is Sprite mainAssetSprite)
										SetMainAndSubAsset(mainAssetSprite, obj);
									else
										SetMainAndSubAsset(mainAsset, obj);
								}

								updateShowSubAssetField = true;
							}
							else
							{
								var isSet = false;

								if (string.IsNullOrEmpty(ValueEntry.SmartValue.SubObjectName))
									if (obj is Sprite)
									{
										var subAssets =
											AssetDatabase.LoadAllAssetRepresentationsAtPath(
												AssetDatabase.GetAssetPath(obj));

										if (subAssets.Length > 0)
										{
											SetMainAndSubAsset(obj, subAssets[0]);

											isSet = true;
										}
									}

								if (!isSet) SetMainAsset(obj);
							}
						}

						if (ActuallyDisallowSubAssets &&
							!targetTypeIsNotValidMainAsset &&
							!string.IsNullOrEmpty(ValueEntry.SmartValue.SubObjectName))
							SetSubAsset(null);
					}
					else if (drop == null)
					{
						SetMainAsset(null);
					}
				}

				// Drawing
				if (Event.current.type == EventType.Repaint)
				{
					GUIContent valueLabel;
					if (value == null || string.IsNullOrEmpty(value.AssetGUID) || value.editorAsset == null)
					{
						valueLabel = GUIHelper.TempContent(NoneSelectedLabel);
					}
					else if (showSubAssetField)
					{
						var path = AssetDatabase.GUIDToAssetPath(value.AssetGUID);
						var assetName = Path.GetFileNameWithoutExtension(path);


						valueLabel = GUIHelper.TempContent(assetName, GetTheDamnPreview(value.editorAsset));
					}
					else
					{
						valueLabel = GUIHelper.TempContent(
							value.editorAsset.name, GetTheDamnPreview(value.editorAsset));
					}

					GUI.Label(mainRect, valueLabel, EditorStyles.objectField);
					SdfIcons.DrawIcon(mainPickerRect.SetWidth(12), SdfIconType.Record2);

					if (showSubAssetField)
					{
						if (string.IsNullOrEmpty(value.SubObjectName) || value.editorAsset == null)
							valueLabel = GUIHelper.TempContent("<none>");
						else
							valueLabel = GUIHelper.TempContent(value.SubObjectName);

						GUI.Label(subRect, valueLabel, EditorStyles.objectField);
						SdfIcons.DrawIcon(subPickerRect.SetWidth(12), SdfIconType.Record2);
					}
				}
			}
			else
			{
				Property.Children[0].Draw(label);
			}
		}

		private static Texture2D GetTheDamnPreview(Object obj)
		{
			var img = obj as Texture2D;

			if (img == null) img = (obj as Sprite)?.texture;

			if (img == null) img = AssetPreview.GetMiniThumbnail(obj);

			return img;
		}

		private bool ConvertToValidAssignment(object drop, out Object converted, out bool isSubAssetAssignment)
		{
			converted = null;
			isSubAssetAssignment = false;

			var isDefinitelyMainAssetAssignment = false;

			if (ReferenceEquals(drop, null)) return false;

			if (!ConvertUtility.TryWeakConvert(drop, targetType, out var convertedObj))
				for (var i = 0; i < validMainAssetTypes.Length; i++)
					if (ConvertUtility.TryWeakConvert(drop, validMainAssetTypes[i], out convertedObj))
					{
						isDefinitelyMainAssetAssignment = true;
						break;
					}

			if (convertedObj == null || !(convertedObj is Object unityObj) || unityObj == null) return false;

			converted = unityObj;

			if (isDefinitelyMainAssetAssignment)
			{
				isSubAssetAssignment = false;
				return true;
			}

			if (AssetDatabase.IsSubAsset(converted))
			{
				if (ActuallyDisallowSubAssets) return false;

				isSubAssetAssignment = true;
				return true;
			}

			return true;
		}

		private void OpenMainAssetSelector(Rect rect)
		{
			EnsureNotRealNull();

			var selector = new AddressableSelector("Select", validMainAssetTypes, restrictions, typeof(T));

			var isUnityRoot = Property.SerializationRoot?.ValueEntry.WeakSmartValue is Object;

			if (isUnityRoot)
			{
				Undo.IncrementCurrentGroup();
				var undoIndex = Undo.GetCurrentGroup();

				selector.SelectionCancelled += () => { Undo.RevertAllDownToGroup(undoIndex); };

				selector.SelectionConfirmed += entries =>
				{
					Undo.RevertAllDownToGroup(undoIndex);

					OnMainAssetSelect(entries.FirstOrDefault());
				};
			}
			else
			{
				selector.SelectionConfirmed += entries => { OnMainAssetSelect(entries.FirstOrDefault()); };
			}

			selector.SelectionChangedWithType += (type, entries) =>
			{
				if (type == SelectionChangedType.SelectionCleared) return;

				var entry = entries.FirstOrDefault();

				OnMainAssetSelect(entry);
			};

			selector.ShowInPopup(rect);
		}

		private void OpenSubAssetSelector(Rect rect)
		{
			EnsureNotRealNull();

			if (ValueEntry.SmartValue == null || ValueEntry.SmartValue.AssetGUID == null)
				return;

			var path = AssetDatabase.GUIDToAssetPath(ValueEntry.SmartValue.AssetGUID);
			if (path == null)
				return;

			var mainAsset = AssetDatabase.LoadMainAssetAtPath(path);

			List<Object> subAssets;

			if (mainAsset != null && mainAsset is SpriteAtlas)
				subAssets = OdinAddressableUtility.EnumerateAllActualAndVirtualSubAssets(mainAsset, path)
					.Where(val => val != null && (val is Sprite || val is Texture2D))
					.ToList();
			else
				subAssets = OdinAddressableUtility.EnumerateAllActualAndVirtualSubAssets(mainAsset, path)
					.Where(val => val != null && targetType.IsInstanceOfType(val))
					.ToList();

			var items = new GenericSelectorItem<Object>[subAssets.Count + 1];

			items[0] = new GenericSelectorItem<Object>("<none>", null);
			for (var i = 0; i < subAssets.Count; i++)
			{
				var item = new GenericSelectorItem<Object>(subAssets[i].name, subAssets[i]);
				items[i + 1] = item;
			}

			var selector = new GenericSelector<Object>("Select Sub Asset", false, items);

			selector.SelectionChanged += OnSubAssetSelect;
			selector.SelectionConfirmed += OnSubAssetSelect;

			selector.ShowInPopup(rect);
		}

		private void OnMainAssetSelect(AddressableAssetEntry entry)
		{
			UpdateAssetReference(entry);
		}

		private void OnSubAssetSelect(IEnumerable<Object> selection)
		{
			if (ValueEntry == null || ValueEntry.SmartValue.AssetGUID == null) return;

			var selected = selection.FirstOrDefault();

			SetSubAsset(selected);
		}

		private void UpdateAssetReference(AddressableAssetEntry entry)
		{
			if (entry == null)
			{
				SetMainAsset(null);

				return;
			}

			if (typeof(T).InheritsFrom<AssetReferenceAtlasedSprite>())
			{
				SetMainAsset(entry.MainAsset);

				return;
			}


			if (typeof(T).InheritsFrom<AssetReferenceSprite>())
			{
				Object subObject = null;

				var path = AssetDatabase.GetAssetPath(entry.TargetAsset);

				if (AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(SpriteAtlas))
					if (!(entry.TargetAsset is SpriteAtlas))
						subObject = entry.TargetAsset;

				SetMainAndSubAsset(entry.MainAsset, subObject);
			}
			else if (!ActuallyDisallowSubAssets && AssetDatabase.IsSubAsset(entry.TargetAsset))
			{
				SetMainAndSubAsset(entry.MainAsset, entry.TargetAsset);
			}
			else
			{
				SetMainAsset(entry.MainAsset);
			}
		}

		private T CreateAssetReferenceFrom(AddressableAssetEntry entry)
		{
			if (entry != null) return CreateAssetReferenceFrom(entry.TargetAsset);

			return null;
		}

		private T CreateAssetReferenceFrom(Object mainAsset, Object subAsset)
		{
			var path = AssetDatabase.GetAssetPath(mainAsset);
			var guid = AssetDatabase.AssetPathToGUID(path);

			if (guid == null) return null;

			var instance = (T) Activator.CreateInstance(typeof(T), guid);
			instance.SetEditorSubObject(subAsset);
			return instance;
		}

		private T CreateAssetReferenceFrom(Object obj)
		{
			var path = AssetDatabase.GetAssetPath(obj);
			var guid = AssetDatabase.AssetPathToGUID(path);

			if (guid == null) return null;

			var instance = (T) Activator.CreateInstance(typeof(T), guid);

			if (typeof(T).InheritsFrom<AssetReferenceSprite>())
			{
				if (AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(SpriteAtlas))
					if (!(obj is SpriteAtlas))
						instance.SetEditorSubObject(obj);
			}
			else if (typeof(T).InheritsFrom<AssetReferenceAtlasedSprite>())
			{
				// No need to do anything here.
				// The user will need to choose a sprite
				// "sub asset" from the atlas.
			}
			else if (ActuallyDisallowSubAssets == false && AssetDatabase.IsSubAsset(obj))
			{
				instance.SetEditorSubObject(obj);
			}

			return instance;
		}

		private void SetMainAndSubAsset(Object mainAsset, Object subAsset, bool setDirtyIfChanged = true)
		{
			var subAssetName = subAsset == null ? null : subAsset.name;

			var isDifferent = ValueEntry.SmartValue.editorAsset != mainAsset ||
							ValueEntry.SmartValue.SubObjectName != subAssetName;

			if (!isDifferent) return;

			if (Property.SerializationRoot?.ValueEntry.WeakSmartValue is Object)
			{
				Undo.IncrementCurrentGroup();

				Undo.SetCurrentGroupName("Main- and Sub Asset Changed");

				var index = Undo.GetCurrentGroup();

				SetMainAsset(mainAsset, false);
				SetSubAsset(subAsset, false);

				Undo.CollapseUndoOperations(index);

				if (setDirtyIfChanged) Property.MarkSerializationRootDirty();
			}
			else
			{
				SetMainAsset(mainAsset, false);
				SetSubAsset(subAsset, false);
			}
		}

		private void SetMainAsset(Object asset, bool setDirtyIfChanged = true)
		{
			if (ValueEntry.SmartValue.editorAsset == asset) return;

			Property.RecordForUndo("Main Asset Changed");

			ValueEntry.SmartValue.SetEditorAsset(asset);

			updateShowSubAssetField = true;

			if (setDirtyIfChanged) Property.MarkSerializationRootDirty();
		}

		private void SetSubAsset(Object asset, bool setDirtyIfChanged = true)
		{
#if SIRENIX_INTERNAL
            if (this.ValueEntry.SmartValue.editorAsset == null)
            {
                Debug.LogError("[SIRENIX INTERNAL] Attempted to assign the Sub Asset on an AssetReference without the Main Asset being assigned first.");
                return;
            }
#endif

			var assetName = asset == null ? null : asset.name;

			if (ValueEntry.SmartValue.SubObjectName == assetName) return;

			Property.RecordForUndo("Sub Asset Changed");

			ValueEntry.SmartValue.SetEditorSubObject(asset);

			updateShowSubAssetField = true;

			if (setDirtyIfChanged) Property.MarkSerializationRootDirty();
		}

		private void EnsureNotRealNull()
		{
			if (ValueEntry.WeakSmartValue == null)
				ValueEntry.SmartValue = OdinAddressableUtility.CreateAssetReferenceGuid<T>(null);
		}
	}

	/// <summary>
	///     Draws an AssetLabelReference field.
	/// </summary>
	[DrawerPriority(0, 1)]
	public class AssetLabelReferenceDrawer : OdinValueDrawer<AssetLabelReference>, IDefinesGenericMenuItems
	{
		public void PopulateGenericMenu(InspectorProperty property, GenericMenu genericMenu)
		{
			genericMenu.AddItem(new GUIContent("Set To Null"), false, () => property.ValueEntry.WeakSmartValue = null);
			genericMenu.AddItem(
				new GUIContent("Open Label Window"), false, () => OdinAddressableUtility.OpenLabelsWindow());
		}

		protected override bool CanDrawValueProperty(InspectorProperty property)
		{
			return property.GetAttribute<DrawWithUnityAttribute>() == null;
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			var rect = SirenixEditorGUI.GetFeatureRichControlRect(
				label, out var controlId, out var hasKeyboardFocus, out var valueRect);

			string valueLabel;
			if (ValueEntry.SmartValue == null || string.IsNullOrEmpty(ValueEntry.SmartValue.labelString))
				valueLabel = "<none>";
			else
				valueLabel = ValueEntry.SmartValue.labelString;

			if (GUI.Button(valueRect, valueLabel, EditorStyles.popup))
			{
				var selector = new AddressableLabelSelector();

				selector.SelectionChanged += SetLabel;
				selector.SelectionConfirmed += SetLabel;

				selector.ShowInPopup(valueRect);
			}
		}

		private void SetLabel(IEnumerable<string> selection)
		{
			var selected = selection.FirstOrDefault();
			ValueEntry.SmartValue = new AssetLabelReference
			{
				labelString = selected
			};
		}
	}

	/// <summary>
	///     Odin Selector for Addressables.
	/// </summary>
	public class AddressableSelector : OdinSelector<AddressableAssetEntry>
	{
		private static readonly EditorPrefEnum<SelectorListMode> listMode = new(
			"AddressablesSelector.ListMode", SelectorListMode.Group);

		private readonly Type[] filterTypes;
		private readonly List<AssetReferenceUIRestriction> restrictions;

		private readonly string title;

		internal bool ShowNonAddressables;

		/// <summary>
		///     Initializes a AddressableSelector.
		/// </summary>
		/// <param name="title">The title of the selector. Set to null for no title.</param>
		/// <param name="filterType">
		///     The type of UnityEngine.Object to be selectable. For example, UnityEngine.Texture. For no
		///     restriction, pass in UnityEngine.Object.
		/// </param>
		/// <param name="labelRestrictions">
		///     The Addressable labels to restrict the selector to. Set to null for no label
		///     restrictions.
		/// </param>
		/// <exception cref="ArgumentNullException">Throws if the filter type is null.</exception>
		public AddressableSelector(
			string title, Type filterType, List<AssetReferenceUIRestriction> restrictions, Type assetReferenceType)
			: this(title, new[] {filterType}, restrictions, assetReferenceType)
		{
		}

		/// <summary>
		///     Initializes a AddressableSelector.
		/// </summary>
		/// <param name="title">The title of the selector. Set to null for no title.</param>
		/// <param name="filterTypes">
		///     The types of UnityEngine.Object to be selectable. For example, UnityEngine.Texture. For no
		///     restriction, pass in an array containing UnityEngine.Object.
		/// </param>
		/// <param name="labelRestrictions">
		///     The Addressable labels to restrict the selector to. Set to null for no label
		///     restrictions.
		/// </param>
		/// <exception cref="ArgumentNullException">Throws if the filter type is null.</exception>
		public AddressableSelector(
			string title, Type[] filterTypes, List<AssetReferenceUIRestriction> restrictions, Type assetReferenceType)
		{
			this.title = title;
			this.filterTypes = filterTypes ?? throw new ArgumentNullException(nameof(filterTypes));
			this.restrictions = restrictions;

			if (assetReferenceType != null)
			{
				if (assetReferenceType.InheritsFrom<AssetReference>() == false)
					throw new ArgumentException("Must inherit AssetReference", nameof(assetReferenceType));

				if (assetReferenceType.IsAbstract)
					throw new ArgumentException("Cannot be abstract type.", nameof(assetReferenceType));
			}
		}

		public override string Title => title;

		//private static EditorPrefBool flatten = new EditorPrefBool("AddressablesSelector.Flatten", false);
		public event Action<SelectionChangedType, IEnumerable<AddressableAssetEntry>> SelectionChangedWithType;

		protected override void DrawToolbar()
		{
			var drawTitle = !string.IsNullOrEmpty(Title);
			var drawSearchToolbar = SelectionTree.Config.DrawSearchToolbar;
			var drawButton = DrawConfirmSelectionButton;

			if (drawTitle || drawSearchToolbar || drawButton)
			{
				SirenixEditorGUI.BeginHorizontalToolbar(SelectionTree.Config.SearchToolbarHeight);
				{
					DrawToolbarTitle();
					DrawToolbarSearch();
					EditorGUI.DrawRect(GUILayoutUtility.GetLastRect().AlignLeft(1), SirenixGUIStyles.BorderColor);

					SdfIconType icon;
					if (listMode.Value == SelectorListMode.Path)
						icon = SdfIconType.ListNested;
					else if (listMode.Value == SelectorListMode.Group)
						icon = SdfIconType.ListStars;
					else if (listMode.Value == SelectorListMode.Flat)
						icon = SdfIconType.List;
					else
						icon = SdfIconType.X;

					if (SirenixEditorGUI.ToolbarButton(icon, true))
					{
						var m = (int) listMode.Value + 1;

						if (m >= (int) SelectorListMode.Max) m = 0;

						listMode.Value = (SelectorListMode) m;

						RebuildMenuTree();
					}

					EditorGUI.BeginChangeCheck();
					ShowNonAddressables = SirenixEditorGUI.ToolbarToggle(ShowNonAddressables, EditorIcons.UnityLogo);
					if (EditorGUI.EndChangeCheck()) RebuildMenuTree();

					if (SirenixEditorGUI.ToolbarButton(SdfIconType.GearFill, true))
						OdinAddressableUtility.OpenGroupsWindow();

					DrawToolbarConfirmButton();
				}
				SirenixEditorGUI.EndHorizontalToolbar();
			}
		}

		protected override void BuildSelectionTree(OdinMenuTree tree)
		{
			if (SelectionChangedWithType != null)
				tree.Selection.SelectionChanged += type =>
				{
					var selection = GetCurrentSelection();

					if (IsValidSelection(selection)) SelectionChangedWithType(type, selection);
				};

			tree.Config.EXPERIMENTAL_INTERNAL_SparseFixedLayouting = true;

			tree.Config.SelectMenuItemsOnMouseDown = true;

			if (AddressableAssetSettingsDefaultObject.SettingsExists)
			{
				var settings = AddressableAssetSettingsDefaultObject.Settings;

				foreach (var group in settings.groups)
				{
					if (group == null || group.name == "Built In Data") continue;

					foreach (var entry in group.entries) AddEntriesToTree(tree, group.name, entry);
				}
			}

			foreach (var item in tree.EnumerateTree())
				if (item.Value == null)
					item.SdfIcon = SdfIconType.Folder;

			if (ShowNonAddressables)
			{
				var searchFilter = "";

				foreach (var filterType in filterTypes) searchFilter += $"t:{filterType.Name} ";

				var enumerator = AssetDatabase_Internals.EnumerateAllAssets(
					searchFilter, false, AssetDatabaseSearchArea.InAssetsOnly);

				if (enumerator.MoveNext())
				{
					var addedGuids = new HashSet<string>();

					foreach (var item in tree.EnumerateTree())
						if (item.Value != null)
							addedGuids.Add((item.Value as AddressableAssetEntry).guid);

					const string NON_ADDRESSABLES_ITEM_NAME = "Non Addressables";

					var nonAddressablesItem = new OdinMenuItem(tree, NON_ADDRESSABLES_ITEM_NAME, null)
						{Icon = EditorIcons.UnityLogo};

					tree.MenuItems.Add(nonAddressablesItem);

					do
					{
						var current = enumerator.Current;

						if (addedGuids.Contains(current.guid) || !current.isMainRepresentation) continue;

						var entry = OdinAddressableUtility.CreateFakeAddressableAssetEntry(current.guid);

						if (listMode == SelectorListMode.Flat)
						{
							var item = new OdinMenuItem(tree, current.name, entry) {Icon = current.icon};

							nonAddressablesItem.ChildMenuItems.Add(item);
						}
						else
						{
							var path = AssetDatabase.GetAssetPath(current.instanceID);

							if (!current.isFolder)
							{
								var extensionEndingIndex = GetExtensionsEndingIndex(path);

								if (extensionEndingIndex != -1) path = path.Substring(0, extensionEndingIndex);
							}

							path = RemoveBaseDirectoryFromAssetPath(path);

							tree.Add($"{NON_ADDRESSABLES_ITEM_NAME}/{path}", entry, current.icon);
						}
					} while (enumerator.MoveNext());

					nonAddressablesItem.ChildMenuItems.SortMenuItemsByName();
				}
			}

			OdinMenuItem noneItem;

			if (filterTypes.Contains(typeof(Object)))
			{
				noneItem = new OdinMenuItem(tree, "<none> (Addressable Asset)", null);
			}
			else
			{
				string filterTypesJoined;

				if (filterTypes.Length == 1)
					filterTypesJoined = filterTypes[0].GetNiceName();
				else
					filterTypesJoined = string.Join("/", filterTypes.Select(t => t.GetNiceName()));

				noneItem = new OdinMenuItem(tree, $"<none> (Addressable {filterTypesJoined})", null);
			}

			noneItem.SdfIcon = SdfIconType.X;
			tree.MenuItems.Insert(0, noneItem);
		}

		private static int GetExtensionsEndingIndex(string path)
		{
			for (var i = path.Length - 1; i >= 0; i--)
			{
				if (path[i] == '\\' || path[i] == '/') return -1;

				if (path[i] == '.') return i;
			}

			return -1;
		}

		private static string RemoveBaseDirectoryFromAssetPath(string path)
		{
			if (path.StartsWith("Assets/")) return path.Remove(0, "Assets/".Length);

			return path;
		}

		private void AddEntriesToTree(OdinMenuTree tree, string groupName, AddressableAssetEntry entry)
		{
			if (entry == null) return;

			var isFolder = entry.IsFolder || AssetDatabase.IsValidFolder(entry.AssetPath);

			if (isFolder)
			{
				entry.GatherAllAssets(null, false, false, true);

				if (entry.SubAssets != null)
					foreach (var e in entry.SubAssets)
						AddEntriesToTree(tree, groupName, e);
			}
			else
			{
				var asset = entry.TargetAsset;

				if (asset == null) return;

				var assetType = asset.GetType();
				var inheritsFromFilterType = false;

				for (var i = 0; i < filterTypes.Length; i++)
					if (filterTypes[i].IsAssignableFrom(assetType))
					{
						inheritsFromFilterType = true;
						break;
					}

				if (inheritsFromFilterType && PassesRestrictions(entry))
				{
					string name;
					if (listMode.Value == SelectorListMode.Group)
						name = entry.address;
					else if (listMode.Value == SelectorListMode.Path)
						name = Path.GetFileNameWithoutExtension(entry.AssetPath);
					else if (listMode.Value == SelectorListMode.Flat)
						name = entry.address;
					else
						throw new Exception("Unsupported list mode: " + listMode.Value);

					var item = new OdinMenuItem(tree, name, entry)
					{
						Icon = AssetPreview.GetMiniThumbnail(asset)
					};

					if (listMode.Value == SelectorListMode.Group)
					{
						var groupItem = tree.GetMenuItem(groupName);

						if (groupItem == null)
						{
							groupItem = new OdinMenuItem(tree, groupName, null);
							tree.MenuItems.Add(groupItem);
						}

						if (entry.ParentEntry != null && entry.ParentEntry.IsFolder)
						{
							OdinMenuItem folderItem = null;

							for (var i = 0; i < groupItem.ChildMenuItems.Count; i++)
								if (groupItem.ChildMenuItems[i].Name == entry.ParentEntry.address)
								{
									folderItem = groupItem.ChildMenuItems[i];
									break;
								}

							if (folderItem == null)
							{
								folderItem = new OdinMenuItem(tree, entry.ParentEntry.address, null);
								groupItem.ChildMenuItems.Add(folderItem);
							}

							folderItem.ChildMenuItems.Add(item);
						}
						else
						{
							groupItem.ChildMenuItems.Add(item);
						}
					}
					else if (listMode.Value == SelectorListMode.Path)
					{
						tree.AddMenuItemAtPath(Path.GetDirectoryName(entry.AssetPath), item);
					}
					else if (listMode.Value == SelectorListMode.Flat)
					{
						tree.MenuItems.Add(item);
					}
				}
			}
		}

		private bool PassesRestrictions(AddressableAssetEntry entry)
		{
			if (restrictions == null) return true;

			return OdinAddressableUtility.ValidateAssetReferenceRestrictions(restrictions, entry.MainAsset);

			//for (int i = 0; i < this.restrictions.Count; i++)
			//{
			//    if (this.restrictions[i].ValidateAsset(entry.AssetPath) == false)
			//    {
			//        return false;
			//    }
			//}

			//return true;

			/* If for whatever reason Unity haven't actually implemented their restriction methods, then we can use this code to atleast implement label restriction. */
			//if (this.labelRestrictions == null) return true;

			//for (int i = 0; i < labelRestrictions.Length; i++)
			//{
			//    if (entry.labels.Contains(labelRestrictions[i])) return true;
			//}

			//return false;
		}

		private enum SelectorListMode
		{
			Group,
			Path,
			Flat,

			Max
		}
	}

	public class AddressableLabelSelector : OdinSelector<string>
	{
		protected override void DrawToolbar()
		{
			var drawTitle = !string.IsNullOrEmpty(Title);
			var drawSearchToolbar = SelectionTree.Config.DrawSearchToolbar;
			var drawButton = DrawConfirmSelectionButton;

			if (drawTitle || drawSearchToolbar || drawButton)
			{
				SirenixEditorGUI.BeginHorizontalToolbar(SelectionTree.Config.SearchToolbarHeight);
				{
					DrawToolbarTitle();
					DrawToolbarSearch();
					EditorGUI.DrawRect(GUILayoutUtility.GetLastRect().AlignLeft(1), SirenixGUIStyles.BorderColor);

					if (SirenixEditorGUI.ToolbarButton(SdfIconType.GearFill, true))
						OdinAddressableUtility.OpenLabelsWindow();

					DrawToolbarConfirmButton();
				}
				SirenixEditorGUI.EndHorizontalToolbar();
			}
		}

		protected override void BuildSelectionTree(OdinMenuTree tree)
		{
			IList<string> labels = null;

			if (AddressableAssetSettingsDefaultObject.SettingsExists)
			{
				var settings = AddressableAssetSettingsDefaultObject.Settings;
				labels = settings.GetLabels();
			}

			if (labels == null) labels = Array.Empty<string>();

			tree.MenuItems.Add(new OdinMenuItem(tree, "<none>", null));

			for (var i = 0; i < labels.Count; i++) tree.MenuItems.Add(new OdinMenuItem(tree, labels[i], labels[i]));
		}
	}

	/// <summary>
	///     Resolves children for AssetReference properties, and implements the
	///     <c>RegisterAssetReferenceAttributeForwardToChild</c> behaviour.
	/// </summary>
	/// <typeparam name="T">The concrete type of AssetReference to be drawn. For example, <c>AssetReferenceTexture</c>.</typeparam>
	public class AssetReferencePropertyResolver<T> : OdinPropertyResolver<T>
		where T : AssetReference
	{
		private static readonly Type[] attributesToForward;

		static AssetReferencePropertyResolver()
		{
			attributesToForward = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(x => x.GetCustomAttributes<RegisterAssetReferenceAttributeForwardToChildAttribute>())
				.Select(x => x.AttributeType)
				.ToArray();
		}

		public override int ChildNameToIndex(string name)
		{
			return 0;
		}

		public override int ChildNameToIndex(ref StringSlice name)
		{
			return 0;
		}

		public override InspectorPropertyInfo GetChildInfo(int childIndex)
		{
			var targetType = OdinAddressableUtility.GetAssetReferenceTargetType(typeof(T));
			var getterSetterType = typeof(AssetReferenceValueGetterSetter<>).MakeGenericType(typeof(T), targetType);

			var getterSetter = Activator.CreateInstance(getterSetterType) as IValueGetterSetter;

			var attributes = new List<Attribute>
			{
				new ShowInInspectorAttribute()
			};

			foreach (var type in attributesToForward)
			{
				var attr = Property.Attributes.FirstOrDefault(x => x.GetType() == type);
				if (attr != null) attributes.Add(attr);
			}

			var label = "Asset";

			return InspectorPropertyInfo.CreateValue(label, 0, SerializationBackend.None, getterSetter, attributes);
		}

		protected override int GetChildCount(T value)
		{
			foreach (var attr in attributesToForward)
				if (Property.Attributes.Any(x => x.GetType() == attr))
					return 1;

			return 0;
		}

		private class AssetReferenceValueGetterSetter<TTarget> : IValueGetterSetter<T, TTarget>
			where TTarget : Object
		{
			public bool IsReadonly => false;

			public Type OwnerType => typeof(T);

			public Type ValueType => typeof(TTarget);

			public TTarget GetValue(ref T owner)
			{
				var v = owner.editorAsset;
				return v as TTarget;
			}

			public object GetValue(object owner)
			{
				var v = (owner as T)?.editorAsset;
				return v as TTarget;
			}

			public void SetValue(ref T owner, TTarget value)
			{
				owner.SetEditorAsset(value);
			}

			public void SetValue(object owner, object value)
			{
				(owner as T).SetEditorAsset(value as TTarget);
			}
		}
	}

	/// <summary>
	///     Processes attributes for AssetReference properties.
	/// </summary>
	/// <typeparam name="T">The concrete type of AssetReference to be drawn. For example, <c>AssetReferenceTexture</c>.</typeparam>
	public class AssetReferenceAttributeProcessor<T> : OdinAttributeProcessor<T>
		where T : AssetReference
	{
		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			attributes.Add(new DoNotDrawAsReferenceAttribute());
			attributes.Add(new HideReferenceObjectPickerAttribute());
			attributes.Add(
				new SuppressInvalidAttributeErrorAttribute()); // TODO: Remove this with proper attribute forwarding support.
		}
	}

	/// <summary>
	///     Processes attributes for AssetLabelReference properties.
	/// </summary>
	public class AssetLabelReferenceAttributeProcessor : OdinAttributeProcessor<AssetLabelReference>
	{
		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			attributes.Add(new DoNotDrawAsReferenceAttribute());
			attributes.Add(new HideReferenceObjectPickerAttribute());
		}
	}

	/// <summary>
	///     Implements conversion behaviour for addressables.
	/// </summary>
	[InitializeOnLoad]
	internal class AssetReferenceConverter : ConvertUtility.ICustomConverter
	{
		private readonly Type type_AssetEntryTreeViewItem;
		private readonly WeakValueGetter<AddressableAssetEntry> get_AssetEntryTreeViewItem_entry;

		static AssetReferenceConverter()
		{
			ConvertUtility.AddCustomConverter(new AssetReferenceConverter());
		}

		public AssetReferenceConverter()
		{
			type_AssetEntryTreeViewItem =
				TwoWaySerializationBinder.Default.BindToType(
					"UnityEditor.AddressableAssets.GUI.AssetEntryTreeViewItem") ?? throw new Exception(
					"Failed to find UnityEditor.AddressableAssets.GUI.AddressableAssetEntryTreeViewItem type.");
			var field_AssetEntryTreeViewItem_entry = type_AssetEntryTreeViewItem.GetField("entry", Flags.AllMembers) ??
													throw new Exception(
														"Failed to find entry field in UnityEditor.AddressableAssets.GUI.AddressableAssetEntryTreeViewItem type.");
			get_AssetEntryTreeViewItem_entry = EmitUtilities.CreateWeakInstanceFieldGetter<AddressableAssetEntry>(
				type_AssetEntryTreeViewItem, field_AssetEntryTreeViewItem_entry);
		}

		// UnityEngine.Object > AssetReference/T
		// AddressableAssetEntry > AssetReference
		// AssetReference/T > UnityEngine.Object
		// AssetReference/T > AssetReference/T
		// AddressableAssetEntry > UnityEngine.Object

		public bool CanConvert(Type from, Type to)
		{
			var comparer = FastTypeComparer.Instance;

			if (to.InheritsFrom(typeof(AssetReference)))
			{
				if (comparer.Equals(from, typeof(AddressableAssetEntry)) ||
					comparer.Equals(from, type_AssetEntryTreeViewItem)) return true;

				if (from.InheritsFrom<Object>())
				{
					if (to.InheritsFrom(typeof(AssetReferenceT<>)))
					{
						var baseType = to.GetGenericBaseType(typeof(AssetReferenceT<>));

						var targetType = baseType.GetGenericArguments()[0];

						return from.InheritsFrom(targetType);
					}

					return true;
				}

				if (from.InheritsFrom(typeof(AssetReference))) return to.InheritsFrom(from);

				return false;
			}

			if (from.InheritsFrom(typeof(AssetReference)) && to.InheritsFrom<Object>()) return false;

			return false;
		}

		public bool TryConvert(object obj, Type to, out object result)
		{
			if (obj == null)
			{
				result = null;
				return false;
			}

			var comparer = FastTypeComparer.Instance;

			// AssetEntryTreeViewItems is a UI element container for AddressableAssetEntry.
			// With this we can just treat AssetEntryTreeViewItems as an AddressableAssetEntry.
			if (comparer.Equals(obj.GetType(), type_AssetEntryTreeViewItem))
				obj = get_AssetEntryTreeViewItem_entry(ref obj);

			if (to.InheritsFrom(typeof(AssetReference)))
			{
				Type assetType;
				if (to.InheritsFrom(typeof(AssetReferenceT<>)))
				{
					var baseType = to.GetGenericBaseType(typeof(AssetReferenceT<>));
					assetType = baseType.GetGenericArguments()[0];
				}
				else
				{
					assetType = typeof(Object);
				}

				if (obj is Object uObj)
				{
					if (obj.GetType().InheritsFrom(assetType))
					{
						var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(uObj));

						if (string.IsNullOrEmpty(guid) == false)
						{
							result = CreateReference(to, uObj);
							return true;
						}

						result = null;
						return false;
					}

					result = null;
					return false;
				}

				if (obj is AddressableAssetEntry entry)
				{
					if (entry.TargetAsset.GetType().InheritsFrom(assetType))
					{
						result = CreateReference(to, entry.TargetAsset);
						return true;
					}

					result = null;
					return false;
				}

				if (obj is AssetReference reference)
				{
					if (TryGetReferencedAsset(reference, assetType, out var asset))
					{
						result = CreateReference(to, asset);
						return true;
					}

					result = null;
					return false;
				}

				result = null;
				return false;
			}
			else if (to.InheritsFrom(typeof(Object)) && obj is AssetReference reference)
			{
				if (TryGetReferencedAsset(reference, to, out var asset))
				{
					result = asset;
					return true;
				}

				result = null;
				return false;
			}
			else if (to.InheritsFrom(typeof(Object)) && obj is AddressableAssetEntry entry)
			{
				var target = entry.TargetAsset;
				if (target == null)
				{
					result = null;
					return false;
				}

				if (target.GetType().InheritsFrom(to))
				{
					result = target;
					return true;
				}

				if (ConvertUtility.TryWeakConvert(target, to, out var converted))
				{
					result = converted;
					return true;
				}

				result = null;
				return false;
			}
			else
			{
				result = null;
				return false;
			}
		}

		private bool TryGetReferencedAsset(AssetReference reference, Type to, out Object asset)
		{
			if (reference.AssetGUID == null)
			{
				asset = null;
				return false;
			}

			var path = AssetDatabase.GUIDToAssetPath(reference.AssetGUID);

			if (reference.SubObjectName != null)
			{
				asset = null;

				foreach (var subAsset in OdinAddressableUtility.EnumerateAllActualAndVirtualSubAssets(
							reference.editorAsset, path))
					if (subAsset.name == reference.SubObjectName)
					{
						asset = subAsset;
						break;
					}
			}
			else
			{
				asset = AssetDatabase.LoadAssetAtPath<Object>(path);
			}

			if (asset != null)
			{
				if (asset.GetType().InheritsFrom(to)) return true;

				if (ConvertUtility.TryWeakConvert(asset, to, out var converted))
				{
					asset = (Object) converted;
					return true;
				}

				asset = null;
				return false;
			}

			return false;
		}

		private AssetReference CreateReference(Type type, Object obj)
		{
			var reference = (AssetReference) Activator.CreateInstance(
				type, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj)));
			if (AssetDatabase.IsSubAsset(obj)) reference.SetEditorAsset(obj);

			return reference;
		}
	}

	/// <summary>
	///     Odin Inspector utility methods for working with addressables.
	/// </summary>
	public static class OdinAddressableUtility
	{
		private static readonly Action openAddressableWindowAction;

		static OdinAddressableUtility()
		{
			var type = TwoWaySerializationBinder.Default.BindToType(
				"UnityEditor.AddressableAssets.GUI.AddressableAssetsWindow") ?? throw new Exception("");
			var method = type.GetMethod("Init", Flags.AllMembers) ?? throw new Exception("");
			openAddressableWindowAction = (Action) Delegate.CreateDelegate(typeof(Action), method);
		}

		public static IEnumerable<Object> EnumerateAllActualAndVirtualSubAssets(Object mainAsset, string mainAssetPath)
		{
			if (mainAsset == null) yield break;

			var subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(mainAssetPath);

			foreach (var subAsset in subAssets) yield return subAsset;

			// The sprites/textures in a sprite atlas are not sub assets of the atlas, but they are apparently
			// still part of the atlas in a way that the addressables system considers a sub asset.
			if (mainAsset is SpriteAtlas atlas)
			{
				var packables = atlas.GetPackables();

				foreach (var packable in packables)
				{
					if (packable == null) continue;

					if (!(packable is DefaultAsset packableFolder))
					{
						yield return packable;
						continue;
					}

					var packablePath = AssetDatabase.GetAssetPath(packableFolder);

					if (!AssetDatabase.IsValidFolder(packablePath)) continue;

					var files = Directory.GetFiles(packablePath, "*.*", SearchOption.AllDirectories);

					foreach (var file in files)
					{
						if (file.EndsWith(".meta")) continue;

						var assetType = AssetDatabase.GetMainAssetTypeAtPath(file);

						if (assetType != typeof(Sprite) && assetType != typeof(Texture2D)) continue;

						yield return AssetDatabase.LoadMainAssetAtPath(file);
					}
				}
			}
		}

		/// <summary>
		///     Opens the addressables group settings window.
		/// </summary>
		public static void OpenGroupsWindow()
		{
			openAddressableWindowAction();
		}

		/// <summary>
		///     Opens the addressables labels settings window.
		/// </summary>
		public static void OpenLabelsWindow()
		{
			if (!AddressableAssetSettingsDefaultObject.SettingsExists) return;
			EditorWindow.GetWindow<LabelWindow>().Intialize(AddressableAssetSettingsDefaultObject.Settings);
		}

		/// <summary>
		///     Converts the specified object into an addressable.
		/// </summary>
		/// <param name="obj">The object to make addressable.</param>
		/// <param name="group">The addressable group to add the object to.</param>
		public static void MakeAddressable(Object obj, AddressableAssetGroup group)
		{
			if (!AddressableAssetSettingsDefaultObject.SettingsExists) return;

			var settings = AddressableAssetSettingsDefaultObject.Settings;
			var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
			var entry = settings.CreateOrMoveEntry(guid, group, false, false);
			entry.address = AssetDatabase.GUIDToAssetPath(guid);
			settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryCreated, entry, false, true);
		}

		/// <summary>
		///     Gets the type targeted by an AssetReference type. For example, returns Texture for AssetReferenceTexture.
		///     Returns UnityEngine.Object for AssetReference type.
		/// </summary>
		/// <param name="assetReferenceType">A type of AssetReference, for example, AssetReferenceTexture.</param>
		/// <returns>
		///     If the given type inherits AssetRefernceT&lt;T&gt;, then the method returns the generic T argument.
		///     If the given type is AssetReference, then the method returns UnityEngine.Object.
		/// </returns>
		/// <exception cref="ArgumentNullException">Throws if given parameter is null.</exception>
		/// <exception cref="ArgumentException">Throws if the given type does not inherit or is AssetReference.</exception>
		public static Type GetAssetReferenceTargetType(Type assetReferenceType)
		{
			if (assetReferenceType == null) throw new ArgumentNullException(nameof(assetReferenceType));

			if (assetReferenceType.InheritsFrom(typeof(AssetReferenceT<>)))
			{
				var genericBase = assetReferenceType.GetGenericBaseType(typeof(AssetReferenceT<>));
				return genericBase.GetGenericArguments()[0];
			}

			return typeof(Object);
		}

		public static Type[] GetAssetReferenceValidMainAssetTypes(Type assetReferenceType)
		{
			if (assetReferenceType == null) throw new ArgumentNullException(nameof(assetReferenceType));

			if (assetReferenceType.InheritsFrom(typeof(AssetReferenceSprite)))
				return new[]
				{
					typeof(Sprite),
					typeof(SpriteAtlas),
					typeof(Texture2D)
				};

			if (assetReferenceType.InheritsFrom(typeof(AssetReferenceAtlasedSprite)))
				return new[] {typeof(SpriteAtlas)};

			return new[] {GetAssetReferenceTargetType(assetReferenceType)};
		}

		/// <summary>
		///     Validate an asset against a list of AssetReferenceUIRestrictions.
		/// </summary>
		/// <param name="restrictions">The restrictions to apply.</param>
		/// <param name="asset">The asset to validate.</param>
		/// <returns>Returns true if the asset passes all restrictions. Otherwise false.</returns>
		/// <exception cref="Exception">Throws if Addressable Settings have not been created.</exception>
		/// <exception cref="ArgumentNullException">Throws if restrictions or asset is null.</exception>
		public static bool ValidateAssetReferenceRestrictions(
			List<AssetReferenceUIRestriction> restrictions, Object asset)
		{
			return ValidateAssetReferenceRestrictions(restrictions, asset, out _);
		}

		/// <summary>
		///     Validate an asset against a list of AssetReferenceUIRestrictions.
		/// </summary>
		/// <param name="restrictions">The restrictions to apply.</param>
		/// <param name="asset">The asset to validate.</param>
		/// <param name="failedRestriction">The first failed restriction. <c>null</c> if no restrictions failed.</param>
		/// <returns>Returns true if the asset passes all restrictions. Otherwise false.</returns>
		/// <exception cref="Exception">Throws if Addressable Settings have not been created.</exception>
		/// <exception cref="ArgumentNullException">Throws if restrictions or asset is null.</exception>
		public static bool ValidateAssetReferenceRestrictions(
			List<AssetReferenceUIRestriction> restrictions, Object asset,
			out AssetReferenceUIRestriction failedRestriction)
		{
			if (AddressableAssetSettingsDefaultObject.SettingsExists == false)
				throw new Exception("Addressable Settings have not been created.");

			_ = restrictions ?? throw new ArgumentNullException(nameof(restrictions));
			_ = asset ?? throw new ArgumentNullException(nameof(asset));

			for (var i = 0; i < restrictions.Count; i++)
				if (restrictions[i] is AssetReferenceUILabelRestriction labels)
				{
					/* Unity, in all its wisdom, have apparently decided not to implement their AssetReferenceRestriction attributes in some versions(?)
					* So, to compensate, we're going to manually validate the label restriction attribute, so atleast that works. */
					var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));

					var entry = AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid, true);

					if (entry.labels.Any(x => labels.m_AllowedLabels.Contains(x)) == false)
					{
						failedRestriction = labels;
						return false;
					}
				}
				else if (restrictions[i].ValidateAsset(asset) == false)
				{
					failedRestriction = restrictions[i];
					return false;
				}

			failedRestriction = null;
			return true;
		}

		internal static TAssetReference CreateAssetReferenceGuid<TAssetReference>(string guid)
			where TAssetReference : AssetReference
		{
			return (TAssetReference) Activator.CreateInstance(typeof(TAssetReference), guid);
		}

		internal static TAssetReference CreateAssetReference<TAssetReference>(Object obj)
			where TAssetReference : AssetReference
		{
			if (obj == null) return CreateAssetReferenceGuid<TAssetReference>(null);

			var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));

			return CreateAssetReferenceGuid<TAssetReference>(guid);
		}

		internal static AddressableAssetEntry CreateFakeAddressableAssetEntry(string guid)
		{
			var entry = (AddressableAssetEntry) FormatterServices.GetUninitializedObject(typeof(AddressableAssetEntry));

			OdinAddressableReflection.AddressableAssetEntry_mGUID_Field.SetValue(entry, guid);

			return entry;
		}
	}
}
#endif
