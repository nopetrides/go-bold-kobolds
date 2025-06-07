using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor.UIElements;
using UnityEditor;
#endif

namespace MoreMountains.Tools
{
	[AttributeUsage(AttributeTargets.Field)]
	public class MMInspectorButtonBarAttribute : PropertyAttribute
	{
		public MMInspectorButtonBarAttribute(
			string[] labels, string[] methods, bool[] onlyWhenPlaying, string[] ussClass)
		{
			Labels = labels;
			Methods = methods;
			OnlyWhenPlaying = onlyWhenPlaying;
			UssClass = ussClass;
		}

		public string[] Labels { get; set; }
		public string[] Methods { get; set; }
		public bool[] OnlyWhenPlaying { get; set; }
		public string[] UssClass { get; set; }
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(MMInspectorButtonBarAttribute))]
	public class MMInspectorButtonBarPropertyDrawer : PropertyDrawer
	{
		private MethodInfo[] _eventMethodInfos;

		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			var inspectorButtonBarAttribute = (MMInspectorButtonBarAttribute) attribute;
			var eventOwnerType = property.serializedObject.targetObject.GetType();

			// add our root
			var root = new VisualElement();

			// add toolbar
			var moveToControls = new Toolbar();
			moveToControls.AddToClassList("mm-toolbar");

			if (_eventMethodInfos == null)
				_eventMethodInfos = new MethodInfo[inspectorButtonBarAttribute.Methods.Length];

			// add each button
			for (var i = 0; i < inspectorButtonBarAttribute.Labels.Length; i++)
			{
				var newButton = new ToolbarButton();
				newButton.text = inspectorButtonBarAttribute.Labels[i];
				newButton.style.flexGrow = 1;

				if (inspectorButtonBarAttribute.UssClass[i] != "")
					newButton.AddToClassList(inspectorButtonBarAttribute.UssClass[i]);

				if (_eventMethodInfos[i] == null)
					_eventMethodInfos[i] = eventOwnerType.GetMethod(
						inspectorButtonBarAttribute.Methods[i],
						BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				if (_eventMethodInfos[i] != null)
				{
					var i1 = i;
					newButton.clicked += () => _eventMethodInfos[i1].Invoke(
						property.serializedObject.targetObject, null);
				}
				else
				{
					Debug.LogWarning(
						string.Format(
							"InspectorButton: Unable to find method {0} in {1}", inspectorButtonBarAttribute.Methods[i],
							eventOwnerType));
				}

				if (inspectorButtonBarAttribute.OnlyWhenPlaying[i] && !Application.isPlaying)
					newButton.SetEnabled(false);

				moveToControls.Add(newButton);
			}

			root.Add(moveToControls);

			return root;
			/*

			if (GUI.Button(buttonRect, inspectorButtonBarAttribute.MethodName))
			{
				System.Type eventOwnerType = prop.serializedObject.targetObject.GetType();
				string eventName = inspectorButtonBarAttribute.MethodName;

				if (_eventMethodInfo == null)
				{
					_eventMethodInfo = eventOwnerType.GetMethod(eventName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				}

				if (_eventMethodInfo != null)
				{
					_eventMethodInfo.Invoke(prop.serializedObject.targetObject, null);
				}
				else
				{
					Debug.LogWarning(string.Format("InspectorButton: Unable to find method {0} in {1}", eventName, eventOwnerType));
				}
			}*/
		}
	}
#endif
}
