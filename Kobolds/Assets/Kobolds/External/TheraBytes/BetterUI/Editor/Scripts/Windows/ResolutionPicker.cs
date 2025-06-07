using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TheraBytes.BetterUi.Editor
{
	public class ResolutionPicker : EditorWindow
	{
		private static Assembly assembly;
		private static EditorWindow gameView;

		private readonly StoredEditorBool
			applyScreenConfigResolution = new("resolutionPicker.applyScreenConfigResolution", true);

		private readonly StoredEditorBool
			displayBuiltin = new("resolutionPicker.displayBuiltin", true);

		private readonly StoredEditorBool
			displayCustom = new("resolutionPicker.displayCustom", true);

		private readonly StoredEditorBool
			displayFree = new("resolutionPicker.displayFree", true);

		private readonly StoredEditorBool
			displayLandscape = new("resolutionPicker.displayLandscape", true);

		private readonly StoredEditorBool
			displayPortrait = new("resolutionPicker.displayPortrait", true);

		private readonly StoredEditorBool
			displayScreenConfigs = new("resolutionPicker.displayScreenConfigs", true);

		private readonly StoredEditorBool
			markCustom = new("resolutionPicker.markCustom", true);

		private readonly StoredEditorBool
			showOrientationHint = new("resolutionPicker.showOrientationHint", true);

		private readonly List<GameViewSize> sizes = new();

		private readonly StoredEditorInt textMode = new("reslutionPicker.textMode", (int) TextDisplayMode.Both);

		private readonly StoredEditorBool useBigButtons = new("resolutionPicker.bigButtons", false);
		private readonly StoredEditorBool useVerticalLayout = new("resolutionPicker.verticalLayout", true);

		private int builtinCount;


		private Type gameSizeType;
		private PropertyInfo selectedIndex;

		private void OnGUI()
		{
			if (gameView == null || gameSizeType == null) RefreshSizes();

			if (gameView == null) // still null?
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.HelpBox("Please open a Game window.", MessageType.Warning);
				if (GUILayout.Button("Open Game View Window", GUILayout.Height(38)))
					gameView = GetWindow(Type.GetType("UnityEditor.GameView," + assembly));
				EditorGUILayout.EndHorizontal();
				return;
			}

			Begin(false);

			// Resolutions
			Begin(true);

			DrawToolStrip(); // settings

			EditorGUILayout.Separator();
			var style = useBigButtons ? GUI.skin.button : EditorStyles.toolbarButton;
			var prevAlign = style.alignment;
			style.alignment = TextAnchor.MiddleLeft;
			style.fontSize = 10;

			var currentIndex = (int) selectedIndex.GetValue(gameView, null);

			for (var i = 0; i < sizes.Count; i++)
			{
				if ((displayFree && i == 1) || (displayBuiltin && i == builtinCount)) EditorGUILayout.Separator();

				var size = sizes[i];

				if (!AllowedToShow(size))
					continue;

				var isOptimizedRes = ResolutionMonitor.IsOptimizedResolution(size.width, size.height);
				var isSelected = currentIndex == size.index;
				style.fontStyle = isOptimizedRes ? isSelected ? FontStyle.BoldAndItalic : FontStyle.Italic
					: isSelected ? FontStyle.Bold : FontStyle.Normal;

				if (GUILayout.Button(GetText(size), style)) SetResolution(size);
			}

			GUILayout.FlexibleSpace();

			style.fontStyle = FontStyle.Normal;
			End(true);

			// Screen Configs
			if (displayScreenConfigs)
			{
				Begin(true);

				var title = useVerticalLayout ? "♦ Screen Configurations" : "♦";
				if (GUILayout.Button(title, EditorStyles.toolbarButton, GUILayout.MinWidth(25)))
					Selection.activeObject = ResolutionMonitor.Instance;

				EditorGUILayout.Space();


				Action<ScreenTypeConditions, int, int> applyScreenConfig = (config, width, height) =>
				{
					if (config != null && ResolutionMonitor.SimulatedScreenConfig == config)
					{
						ResolutionMonitor.SimulatedScreenConfig = null;
					}
					else
					{
						ResolutionMonitor.SimulatedScreenConfig = config;

						if (applyScreenConfigResolution)
						{
							RefreshSizes();
							var gvs = sizes.FirstOrDefault(o =>
								o.width == width && o.height == height);

							if (gvs == null)
							{
								var name = config != null ? config.Name : ResolutionMonitor.Instance.FallbackName;
								AddSizeToUnity(name, width, height);

								gvs = sizes.FirstOrDefault(o =>
									o.width == width && o.height == height);
							}

							if (gvs != null) SetResolution(gvs);
						}
					}
				};

				if (GUILayout.Button(ResolutionMonitor.Instance.FallbackName + " (Fallback)", style))
				{
					var resolution = ResolutionMonitor.OptimizedResolutionFallback;
					applyScreenConfig(null, (int) resolution.x, (int) resolution.y);
				}

				EditorGUILayout.Space();

				foreach (var config in ResolutionMonitor.Instance.OptimizedScreens)
					if (GUILayout.Button(ResolutionMonitorEditor.GetButtonText(config), style))
						applyScreenConfig(config, config.OptimizedWidth, config.OptimizedHeight);

				GUILayout.FlexibleSpace();
				End(true);
			}

			End(false);

			style.alignment = prevAlign;
		}

		[MenuItem("Tools/Better UI/Pick Resolution", false, 90)]
		public static void ShowWindow()
		{
			assembly = typeof(EditorWindow).Assembly;

			var win = GetWindow<ResolutionPicker>("Pick Resolution");
			win.minSize = new Vector2(20, 40);
			win.RefreshSizes();
		}

		private void RefreshSizes()
		{
			assembly = typeof(EditorWindow).Assembly;

			var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
			gameView = Resources.FindObjectsOfTypeAll(gameViewType)
				.FirstOrDefault() as EditorWindow;

			if (gameView == null)
				return;

			var gameSizesType = Type.GetType("UnityEditor.GameViewSizes," + assembly);
			var gameSizeGroupType = Type.GetType("UnityEditor.GameViewSizeGroup," + assembly);
			gameSizeType = Type.GetType("UnityEditor.GameViewSize," + assembly);

			var gameSizesInstance = gameSizesType.BaseType
				.GetProperty("instance", BindingFlags.Public | BindingFlags.Static).GetValue(null, null);
			var gameViewGroup = gameSizesType.GetProperty("currentGroup").GetValue(gameSizesInstance, null);

			var count = (int) gameSizeGroupType.InvokeMember(
				"GetTotalCount", BindingFlags.InvokeMethod, null, gameViewGroup, null);
			builtinCount = (int) gameSizeGroupType.InvokeMember(
				"GetBuiltinCount", BindingFlags.InvokeMethod, null, gameViewGroup, null);

			sizes.Clear();
			for (var i = 0; i < count; i++)
			{
				var gameSize = gameSizeGroupType.InvokeMember(
					"GetGameViewSize", BindingFlags.InvokeMethod, null, gameViewGroup, new object[] {i});
				var isCustom = i >= builtinCount;
				AddSize(gameSizeType, gameSize, i, isCustom);
			}

			var bindingFlags =
#if UNITY_2022_1_OR_NEWER
				BindingFlags.Instance | BindingFlags.Public;
#else
                BindingFlags.Instance | BindingFlags.NonPublic;
#endif

			selectedIndex = gameView.GetType().GetProperty("selectedSizeIndex", bindingFlags);
		}

		private void AddSizeToUnity(string name, int width, int height)
		{
			RefreshSizes();
			try
			{
				var size = new GameViewSize
				{
					baseText = name,
					displayText = name,
					width = width,
					height = height,
					index = sizes.Count,
					isCustom = true,
					isAspectRatio = false
				};

				var ass = typeof(EditorApplication).Assembly;
				var gameViewSizesType = ass.GetType("UnityEditor.GameViewSizes");
				var singleType = typeof(ScriptableSingleton<>).MakeGenericType(gameViewSizesType);
				var gameViewSizesInfo = singleType.GetProperty("instance");
				var gameViewSizes = gameViewSizesInfo.GetValue(null, new object[] { });


				var gameViewSizeGroupInfo = gameViewSizesType.GetMember("currentGroup")[0] as PropertyInfo;
				var gameViewSizeGroup = gameViewSizeGroupInfo.GetValue(gameViewSizes, new object[] { });

				var addSizeMethod = gameViewSizeGroup.GetType().GetMethod("AddCustomSize");
				addSizeMethod.Invoke(gameViewSizeGroup, new[] {size.ToInternalObject()});

				var saveToHddMethod = gameViewSizesType.GetMethod("SaveToHDD");
				saveToHddMethod.Invoke(gameViewSizes, new object[] { });

				RefreshSizes();
			}
			catch (Exception ex)
			{
				Debug.LogErrorFormat("Couldn't create resolution: {0}", ex);
			}
		}

		private void SetResolution(GameViewSize size)
		{
			var type = gameView.GetType();
			selectedIndex.SetValue(gameView, size.index, null);

			if (ResolutionMonitor.IsZoomPossible())
			{
				var method = type.GetMethod("UpdateZoomAreaAndParent", BindingFlags.Instance | BindingFlags.NonPublic);
				method.Invoke(gameView, null);
			}

			var resizedNotifyMethod = type.GetMethod("OnResized", BindingFlags.Instance | BindingFlags.NonPublic);
			resizedNotifyMethod.Invoke(gameView, null);

			SceneView.RepaintAll();
		}

		private void Begin(bool mainSection)
		{
			if (useVerticalLayout == mainSection)
				EditorGUILayout.BeginVertical();
			else
				EditorGUILayout.BeginHorizontal();
		}

		private void End(bool mainSection)
		{
			if (useVerticalLayout == mainSection)
				EditorGUILayout.EndVertical();
			else
				EditorGUILayout.EndHorizontal();
		}

		private bool AllowedToShow(GameViewSize size)
		{
			// special treatment for free aspect
			if (size.width == 0 && size.height == 0)
				return displayFree;

			var allow = (size.width >= size.height && displayLandscape)
						|| (size.width < size.height && displayPortrait);

			allow = allow && ((size.isCustom && displayCustom)
							|| (!size.isCustom && displayBuiltin));

			return allow;
		}

		private string GetText(GameViewSize size)
		{
			var result = "";

			if (showOrientationHint)
			{
				if (size.width > size.height)
					result += "▬ ";
				else if (size.width < size.height) result += " ▌";
			}

			if (markCustom && size.isCustom) result += "☺ ";
			switch ((TextDisplayMode) textMode.Value)
			{
				case TextDisplayMode.Size: result += size.width == 0 && size.height == 0 ? "X:Y" : size.sizeText; break;
				case TextDisplayMode.Name:
					result += string.IsNullOrEmpty(size.baseText) ? size.sizeText : size.baseText; break;
				case TextDisplayMode.Both: result += size.displayText; break;
				default:
					throw new ArgumentException();
			}


			return result;
		}

		private void DrawToolStrip()
		{
			var title = useVerticalLayout ? "♠ Settings" : "♠";
			if (GUILayout.Button(title, EditorStyles.toolbarDropDown, GUILayout.MinWidth(25)))
			{
				var toolsMenu = new GenericMenu();
				toolsMenu.AddSeparator("");
				toolsMenu.AddDisabledItem(new GUIContent("♥ Resolution Filters"));
				toolsMenu.AddSeparator("");
				toolsMenu.AddItem(new GUIContent("Free Aspect"), displayFree, DisplayFree);
				toolsMenu.AddItem(new GUIContent("Portrait ( ▌ )"), displayPortrait, DisplayPortrait);
				toolsMenu.AddItem(new GUIContent("Landscape ( ▬ )"), displayLandscape, DisplayLandscape);
				toolsMenu.AddSeparator("");
				toolsMenu.AddItem(new GUIContent("Builtin"), displayBuiltin, DisplayBuiltin);
				toolsMenu.AddItem(new GUIContent("Custom ( ☺ )"), displayCustom, DisplayCustom);

				toolsMenu.AddSeparator("");
				toolsMenu.AddDisabledItem(new GUIContent("♦ Screen Configurations"));
				toolsMenu.AddSeparator("");

				toolsMenu.AddItem(new GUIContent("Show"), displayScreenConfigs, DisplayScreenConfigs);
				toolsMenu.AddItem(
					new GUIContent("Apply Resolution"), applyScreenConfigResolution, ApplyScreenConfigResolution);

				toolsMenu.AddSeparator("");
				toolsMenu.AddDisabledItem(new GUIContent("♣ Options"));
				toolsMenu.AddSeparator("");

				toolsMenu.AddItem(
					new GUIContent("Text Options/Name and Size"), textMode == (int) TextDisplayMode.Both, TextModeBoth);
				toolsMenu.AddItem(
					new GUIContent("Text Options/Name"), textMode == (int) TextDisplayMode.Name, TextModeName);
				toolsMenu.AddItem(
					new GUIContent("Text Options/Size"), textMode == (int) TextDisplayMode.Size, TextModeSize);
				toolsMenu.AddSeparator("Text Options/");
				toolsMenu.AddItem(
					new GUIContent("Text Options/Orientation Hint"), showOrientationHint, ShowOrientationHint);
				toolsMenu.AddSeparator("Text Options/");
				toolsMenu.AddItem(new GUIContent("Text Options/Mark Custom"), markCustom, MarkCustom);

				toolsMenu.AddItem(new GUIContent("Style/Big"), useBigButtons, UseBigButtons);
				toolsMenu.AddItem(new GUIContent("Style/Small"), !useBigButtons, UseSmallButtons);

				toolsMenu.AddItem(new GUIContent("Layout/Vertical"), useVerticalLayout, UseVerticalLayout);
				toolsMenu.AddItem(new GUIContent("Layout/Horizontal"), !useVerticalLayout, UseHorizontalLayout);

				toolsMenu.AddSeparator("");
				toolsMenu.AddItem(new GUIContent("Refresh List"), false, RefreshSizes);


				toolsMenu.DropDown(new Rect(0, 0, 0, 16));
				EditorGUIUtility.ExitGUI();
			}
		}

		private void DisplayPortrait()
		{
			displayPortrait.Value = !displayPortrait;
		}

		private void DisplayLandscape()
		{
			displayLandscape.Value = !displayLandscape;
		}

		private void DisplayFree()
		{
			displayFree.Value = !displayFree;
		}

		private void DisplayBuiltin()
		{
			displayBuiltin.Value = !displayBuiltin;
		}

		private void DisplayCustom()
		{
			displayCustom.Value = !displayCustom;
		}

		private void ShowOrientationHint()
		{
			showOrientationHint.Value = !showOrientationHint;
		}

		private void MarkCustom()
		{
			markCustom.Value = !markCustom;
		}

		private void TextModeBoth()
		{
			textMode.Value = (int) TextDisplayMode.Both;
		}

		private void TextModeSize()
		{
			textMode.Value = (int) TextDisplayMode.Size;
		}

		private void TextModeName()
		{
			textMode.Value = (int) TextDisplayMode.Name;
		}

		private void UseBigButtons()
		{
			useBigButtons.Value = true;
		}

		private void UseSmallButtons()
		{
			useBigButtons.Value = false;
		}

		private void UseVerticalLayout()
		{
			useVerticalLayout.Value = true;
		}

		private void UseHorizontalLayout()
		{
			useVerticalLayout.Value = false;
		}

		private void DisplayScreenConfigs()
		{
			displayScreenConfigs.Value = !displayScreenConfigs;
		}

		private void ApplyScreenConfigResolution()
		{
			applyScreenConfigResolution.Value = !applyScreenConfigResolution;
		}


		private void AddSize(Type gameSizeType, object gameSize, int index, bool isCustom)
		{
			var item = new GameViewSize();
			item.index = index;
			item.isCustom = isCustom;
			item.width = (int) gameSizeType.GetProperty("width").GetValue(gameSize, null);
			item.height = (int) gameSizeType.GetProperty("height").GetValue(gameSize, null);
			item.baseText = (string) gameSizeType.GetProperty("baseText").GetValue(gameSize, null);
			item.displayText = (string) gameSizeType.GetProperty("displayText").GetValue(gameSize, null);
			item.isAspectRatio = (int) gameSizeType.GetProperty("sizeType").GetValue(gameSize, null) == 0;

			sizes.Add(item);
		}

		internal enum TextDisplayMode
		{
			Size,
			Name,
			Both
		}

		internal class GameViewSize
		{
			internal string baseText;
			internal string displayText;
			internal int height;
			internal int index;
			internal bool isAspectRatio;
			internal bool isCustom;
			internal int width;

			internal string sizeText =>
				isAspectRatio ? string.Format("{0}:{1}", width, height) : string.Format("{0}x{1}", width, height);

			public object ToInternalObject()
			{
				var ass = typeof(EditorApplication).Assembly;
				var t = ass.GetType("UnityEditor.GameViewSize");
				var sizeType = ass.GetType("UnityEditor.GameViewSizeType");
				var constructor = t.GetConstructor(new[] {sizeType, typeof(int), typeof(int), typeof(string)});
				return constructor.Invoke(new object[] {isAspectRatio ? 0 : 1, width, height, baseText});
			}
		}
	}
}
