using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace MoreMountains.Tools
{
	public class MMInspectorGroupData
	{
		public MMInspectorGroupAttribute GroupAttribute;
		public Color GroupColor;
		public HashSet<string> GroupHashSet = new();
		public bool GroupIsOpen;
		public List<SerializedProperty> PropertiesList = new();

		public void ClearGroup()
		{
			GroupAttribute = null;
			GroupHashSet.Clear();
			PropertiesList.Clear();
		}
	}

	[CanEditMultipleObjects]
	[CustomEditor(typeof(MMMonoBehaviour), true)]
	public class MMMonoBehaviourUITKEditor : Editor
	{
		public StyleSheet EditorStyleSheet;

		public bool DrawerInitialized;
		private readonly bool _hasMMHiddenProperties = false;
		private string[] _mmHiddenPropertiesToHide;
		private bool _requiresConstantRepaint;
		private bool _requiresConstantRepaintOnlyWhenPlaying;
		protected bool _shouldDrawBase = true;
		private MMMonoBehaviour _targetMonoBehaviourGameObject;
		private bool _targetMonoBehaviourIsNotNull;
		protected string _targetTypeName;
		public Dictionary<string, MMInspectorGroupData> GroupData;
		public List<SerializedProperty> PropertiesList;

		public override bool RequiresConstantRepaint()
		{
			if (_requiresConstantRepaintOnlyWhenPlaying)
				return Application.isPlaying && _targetMonoBehaviourIsNotNull && _targetMonoBehaviourGameObject.enabled;

			return _requiresConstantRepaint;
		}

		protected virtual void Initialization()
		{
			if (DrawerInitialized && PropertiesList != null) return;

			_shouldDrawBase = true;
			GroupData = new Dictionary<string, MMInspectorGroupData>();
			PropertiesList = new List<SerializedProperty>();
			_targetTypeName = target.GetType().Name;

			_targetMonoBehaviourGameObject = (MMMonoBehaviour) target;
			if (_targetMonoBehaviourGameObject != null) _targetMonoBehaviourIsNotNull = true;

			_requiresConstantRepaint = serializedObject.targetObject.GetType()
				.GetCustomAttribute<MMRequiresConstantRepaintAttribute>() != null;
			_requiresConstantRepaintOnlyWhenPlaying = serializedObject.targetObject.GetType()
				.GetCustomAttribute<MMRequiresConstantRepaintOnlyWhenPlayingAttribute>() != null;

			List<FieldInfo> fieldInfoList;
			MMInspectorGroupAttribute previousGroupAttribute = default;
			var fieldInfoLength = MMMonoBehaviourFieldInfo.GetFieldInfo(target, out fieldInfoList);

			for (var i = 0; i < fieldInfoLength; i++)
			{
				var group =
					Attribute.GetCustomAttribute(fieldInfoList[i], typeof(MMInspectorGroupAttribute)) as
						MMInspectorGroupAttribute;
				MMInspectorGroupData groupData;
				if (group == null)
				{
					if (previousGroupAttribute != null && previousGroupAttribute.GroupAllFieldsUntilNextGroupAttribute)
					{
						_shouldDrawBase = false;
						if (!GroupData.TryGetValue(previousGroupAttribute.GroupName, out groupData))
						{
							GroupData.Add(
								previousGroupAttribute.GroupName, new MMInspectorGroupData
								{
									GroupAttribute = previousGroupAttribute,
									GroupHashSet = new HashSet<string> {fieldInfoList[i].Name},
									GroupColor = MMColors.GetColorAt(previousGroupAttribute.GroupColorIndex)
								});
						}
						else
						{
							groupData.GroupColor = MMColors.GetColorAt(previousGroupAttribute.GroupColorIndex);
							groupData.GroupHashSet.Add(fieldInfoList[i].Name);
						}
					}

					continue;
				}

				previousGroupAttribute = group;

				if (!GroupData.TryGetValue(group.GroupName, out groupData))
				{
					var fallbackOpenState = true;
					if (group.ClosedByDefault) fallbackOpenState = false;
					var groupIsOpen = EditorPrefs.GetBool(
						string.Format($"{group.GroupName}{fieldInfoList[i].Name}{target.GetInstanceID()}"),
						fallbackOpenState);
					GroupData.Add(
						group.GroupName, new MMInspectorGroupData
						{
							GroupAttribute = group,
							GroupColor = MMColors.GetColorAt(previousGroupAttribute.GroupColorIndex),
							GroupHashSet = new HashSet<string> {fieldInfoList[i].Name}, GroupIsOpen = groupIsOpen
						});
				}
				else
				{
					groupData.GroupHashSet.Add(fieldInfoList[i].Name);
					groupData.GroupColor = MMColors.GetColorAt(previousGroupAttribute.GroupColorIndex);
				}
			}

			var iterator = serializedObject.GetIterator();

			if (iterator.NextVisible(true))
				do
				{
					FillPropertiesList(iterator);
				} while (iterator.NextVisible(false));

			DrawerInitialized = true;
		}

		public void FillPropertiesList(SerializedProperty serializedProperty)
		{
			var shouldClose = false;

			foreach (var pair in GroupData)
				if (pair.Value.GroupHashSet.Contains(serializedProperty.name))
				{
					var property = serializedProperty.Copy();
					shouldClose = true;
					pair.Value.PropertiesList.Add(property);
					break;
				}

			if (!shouldClose)
			{
				var property = serializedProperty.Copy();
				PropertiesList.Add(property);
			}
		}

		public override VisualElement CreateInspectorGUI()
		{
			Initialization();

			var root = new VisualElement();
			root.styleSheets.Add(EditorStyleSheet);

			// Draw the script field
			var scriptProperty = serializedObject.FindProperty("m_Script");

			if (PropertiesList.Count == 0) return root;

			if (_shouldDrawBase)
			{
				var defaultInspector = new VisualElement();
				InspectorElement.FillDefaultInspector(defaultInspector, serializedObject, this);
				root.Add(defaultInspector);
				return root;
			}

			var scriptField = new PropertyField(scriptProperty);
			scriptField.SetEnabled(false);
			root.Add(scriptField);

			foreach (var pair in GroupData) DrawGroup(pair.Value, root);

			serializedObject.ApplyModifiedProperties();

			return root;
		}

		protected virtual void DrawGroup(MMInspectorGroupData groupData, VisualElement root)
		{
			var foldout = new Foldout();
			foldout.text = groupData.GroupAttribute.GroupName;
			foldout.value = groupData.GroupIsOpen;
			foldout.AddToClassList("mm-foldout");
			foldout.style.borderLeftColor = groupData.GroupColor;
			foldout.viewDataKey = target.name + "-" + _targetTypeName + groupData.GroupAttribute.GroupName;
			root.Add(foldout);

			var toggleElement = foldout.Q<Toggle>();
			toggleElement.AddToClassList("mm-foldout-toggle");

			for (var i = 0; i < groupData.PropertiesList.Count; i++) DrawChild(i, foldout, root);

			void DrawChild(int i, Foldout foldout, VisualElement root)
			{
				if (_hasMMHiddenProperties &&
					_mmHiddenPropertiesToHide.Contains(groupData.PropertiesList[i].name)) return;
				var field = new PropertyField(groupData.PropertiesList[i]);
				field.label = ObjectNames.NicifyVariableName(groupData.PropertiesList[i].name);
				field.tooltip = groupData.PropertiesList[i].tooltip;
				foldout.Add(field);
			}
		}
	}
}
