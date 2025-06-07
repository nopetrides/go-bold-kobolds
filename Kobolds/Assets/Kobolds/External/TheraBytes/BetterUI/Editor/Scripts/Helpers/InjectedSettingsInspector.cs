using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TheraBytes.BetterUi.Editor
{
	public class InjectedSettingsInspector
	{
		// key: fallback, value: customSizers
		private readonly List<SettingsProperty> allControls = new();
		private readonly SerializedObject serializedObject;
		private SerializedProperty settingsFallback;
		private readonly SerializedProperty settingsList;

		private string settingsName;

		public InjectedSettingsInspector(
			string settingsName, SerializedObject serializedObject,
			string settingsListName, string settingsFallbackName)
		{
			this.settingsName = settingsName;
			this.serializedObject = serializedObject;
			settingsList = serializedObject.FindProperty(settingsListName);
			settingsFallback = serializedObject.FindProperty(settingsFallbackName);
		}

		public void Register(string displayName, string settingsPropName)
		{
			var p = new SettingsProperty
			{
				Label = new GUIContent(displayName),
				SettingsPropName = settingsPropName
			};

			allControls.Add(p);
		}

		public void Register(string displayName, string settingsBoolName, string otherSettingsPropertyName)
		{
			var p = new CheckWithProp
			{
				Label = new GUIContent(displayName),
				SettingsPropName = settingsBoolName,
				OtherSettingsPropertyName = otherSettingsPropertyName
			};

			allControls.Add(p);
		}

		public void Register(
			string displayName, string settingsBoolName, string customSizersName, string sizerFallbackName)
		{
			var p = new CheckWithSizer
			{
				Label = new GUIContent(displayName),
				SettingsPropName = settingsBoolName,
				SizerFallbackName = sizerFallbackName,
				CustomSizers = serializedObject.FindProperty(customSizersName),
				SizerFallback = serializedObject.FindProperty(sizerFallbackName)
			};

			allControls.Add(p);
		}

		public void RegisterSkipRest(string displayName, string settingsBoolName, bool valueToSkip)
		{
			var p = new CheckToSkipRest
			{
				Label = new GUIContent(displayName),
				SettingsPropName = settingsBoolName,
				ValueToSkip = valueToSkip
			};

			allControls.Add(p);
		}

		public void RegisterSpace()
		{
			allControls.Add(null);
		}

		public void Draw()
		{
			ScreenConfigConnectionHelper.DrawGui(
				"Settings", settingsList, ref settingsFallback, DrawSettings,
				AddSettings, DeleteSettings);
		}

		public void DrawSettings(string configName, SerializedProperty settings)
		{
			EditorGUILayout.BeginVertical("box");
			DrawControls(configName, settings);
			EditorGUILayout.EndVertical();
		}

		public void DrawControls(string configName, SerializedProperty settings)
		{
			foreach (var p in allControls)
			{
				if (p == null)
				{
					EditorGUILayout.Space();
					continue;
				}

				var prop = settings.FindPropertyRelative(p.SettingsPropName);
				if (p is CheckWithProp cwp)
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.PropertyField(prop, p.Label);
					if (prop.boolValue)
					{
						var other = settings.FindPropertyRelative(cwp.OtherSettingsPropertyName);
						EditorGUILayout.PropertyField(other, GUIContent.none);
					}

					EditorGUILayout.EndHorizontal();
				}
				else
				{
					EditorGUILayout.PropertyField(prop, p.Label);

					if (p is CheckWithSizer cws)
					{
						if (prop.boolValue) DrawSizer(configName, cws.SizerFallback, cws.CustomSizers);
					}
					else if (p is CheckToSkipRest csr)
					{
						if (prop.boolValue == csr.ValueToSkip)
							return;

						EditorGUILayout.Space();
					}
				}
			}
		}

		private void DeleteSettings(string configName, SerializedProperty property)
		{
			foreach (var p in allControls.OfType<CheckWithSizer>())
			{
				int idx;
				var sizersProp = FindSizer(configName, null, p.CustomSizers, out idx);
				if (sizersProp != null)
				{
					var items = p.CustomSizers.FindPropertyRelative("items");
					items.DeleteArrayElementAtIndex(idx);
				}
			}
		}

		private void AddSettings(string configName, SerializedProperty property)
		{
			foreach (var p in allControls.OfType<CheckWithSizer>())
			{
				var sizersProp = FindSizer(configName, null, p.CustomSizers);
				if (sizersProp == null)
				{
					var items = p.CustomSizers.FindPropertyRelative("items");
					var fallback = p.SizerFallback;
					ScreenConfigConnectionHelper.AddSizerToList(configName, ref fallback, items);

					var configs = p.CustomSizers.GetValue<ISizeConfigCollection>();
					configs.MarkDirty();
				}
			}

			// after adding the fallback values are pointing somewhere because of copying of all properties
			RestoreSizerFallbackReferences();
		}

		private void RestoreSizerFallbackReferences()
		{
			foreach (var p in allControls.OfType<CheckWithSizer>())
				p.SizerFallback = serializedObject.FindProperty(p.SizerFallbackName);
		}


		private static void DrawSizer(string configName, SerializedProperty fallback, SerializedProperty customSizers)
		{
			EditorGUI.indentLevel++;

			var prop = FindSizer(configName, fallback, customSizers);
			if (prop != null)
				EditorGUILayout.PropertyField(prop);
			else
				EditorGUILayout.HelpBox(
					string.Format("could not find sizer for config '{0}'", configName), MessageType.Error);

			EditorGUI.indentLevel--;
		}

		private static SerializedProperty FindSizer(
			string configName, SerializedProperty fallback, SerializedProperty customSizers)
		{
			int idx;
			return FindSizer(configName, fallback, customSizers, out idx);
		}

		private static SerializedProperty FindSizer(
			string configName, SerializedProperty fallback, SerializedProperty customSizers, out int sizerIndex)
		{
			var isFallback = !ResolutionMonitor.Instance.OptimizedScreens.Any(o => o.Name == configName);
			sizerIndex = -1;

			if (isFallback) return fallback;

			var items = customSizers.FindPropertyRelative("items");
			for (var i = 0; i < items.arraySize; i++)
			{
				var prop = items.GetArrayElementAtIndex(i);
				var propConfig = prop.FindPropertyRelative("screenConfigName");
				if (propConfig.stringValue == configName)
				{
					sizerIndex = i;
					return prop;
				}
			}

			return null;
		}

		private class SettingsProperty
		{
			public GUIContent Label { get; set; }
			public string SettingsPropName { get; set; }
		}

		private class CheckToSkipRest : SettingsProperty
		{
			public bool ValueToSkip { get; set; }
		}

		private class CheckWithSizer : SettingsProperty
		{
			public string SizerFallbackName { get; set; }
			public SerializedProperty SizerFallback { get; set; }
			public SerializedProperty CustomSizers { get; set; }
		}

		private class CheckWithProp : SettingsProperty
		{
			public string OtherSettingsPropertyName { get; set; }
		}
	}
}
