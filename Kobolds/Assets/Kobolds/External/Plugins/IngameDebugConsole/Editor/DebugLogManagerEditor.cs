using UnityEditor;
using UnityEngine;

namespace IngameDebugConsole
{
	[CustomEditor(typeof(DebugLogManager))]
	public class DebugLogManagerEditor : Editor
	{
#if UNITY_2017_3_OR_NEWER
		private readonly GUIContent popupVisibilityLogFilterLabel = new(
			"Log Filter", "Determines which log types will show the popup on screen");
#endif
		private readonly GUIContent receivedLogTypesLabel = new(
			"Received Log Types",
			"Only these logs will be received by the console window, other logs will simply be skipped");

		private readonly GUIContent receiveErrorLogsLabel = new("Error");
		private readonly GUIContent receiveExceptionLogsLabel = new("Exception");
		private readonly GUIContent receiveInfoLogsLabel = new("Info");
		private readonly GUIContent receiveWarningLogsLabel = new("Warning");
		private SerializedProperty alwaysDisplayTimestamps;
		private SerializedProperty autoFocusOnCommandInputField;
		private SerializedProperty avoidScreenCutout;
		private SerializedProperty captureLogTimestamps;
		private SerializedProperty clearCommandAfterExecution;
		private SerializedProperty commandHistorySize;
		private SerializedProperty enableHorizontalResizing;
		private SerializedProperty enableSearchbar;
		private SerializedProperty logcatArguments;
		private SerializedProperty logsToRemoveAfterMaxLogCount;
		private SerializedProperty logWindowOpacity;
		private SerializedProperty maxLogCount;
		private SerializedProperty minimumHeight;
		private SerializedProperty minimumWidth;
		private SerializedProperty popupAvoidsScreenCutout;
		private SerializedProperty popupOpacity;
		private SerializedProperty popupVisibility;
		private SerializedProperty popupVisibilityLogFilter;
		private SerializedProperty queuedLogLimit;
		private SerializedProperty receiveErrorLogs;
		private SerializedProperty receiveExceptionLogs;
		private SerializedProperty receiveInfoLogs;
		private SerializedProperty receiveLogcatLogsInAndroid;
		private SerializedProperty receiveLogsWhileInactive;
		private SerializedProperty receiveWarningLogs;
		private SerializedProperty resizeFromRight;
		private SerializedProperty showCommandSuggestions;
		private SerializedProperty singleton;
		private SerializedProperty startMinimized;
		private SerializedProperty toggleKey;
		private SerializedProperty toggleWithKey;
		private SerializedProperty topSearchbarMinWidth;

		private void OnEnable()
		{
			singleton = serializedObject.FindProperty("singleton");
			minimumHeight = serializedObject.FindProperty("minimumHeight");
			enableHorizontalResizing = serializedObject.FindProperty("enableHorizontalResizing");
			resizeFromRight = serializedObject.FindProperty("resizeFromRight");
			minimumWidth = serializedObject.FindProperty("minimumWidth");
			logWindowOpacity = serializedObject.FindProperty("logWindowOpacity");
			popupOpacity = serializedObject.FindProperty("popupOpacity");
			popupVisibility = serializedObject.FindProperty("popupVisibility");
			popupVisibilityLogFilter = serializedObject.FindProperty("popupVisibilityLogFilter");
			startMinimized = serializedObject.FindProperty("startMinimized");
			toggleWithKey = serializedObject.FindProperty("toggleWithKey");
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
			toggleKey = serializedObject.FindProperty( "toggleBinding" );
#else
			toggleKey = serializedObject.FindProperty("toggleKey");
#endif
			enableSearchbar = serializedObject.FindProperty("enableSearchbar");
			topSearchbarMinWidth = serializedObject.FindProperty("topSearchbarMinWidth");
			receiveLogsWhileInactive = serializedObject.FindProperty("receiveLogsWhileInactive");
			receiveInfoLogs = serializedObject.FindProperty("receiveInfoLogs");
			receiveWarningLogs = serializedObject.FindProperty("receiveWarningLogs");
			receiveErrorLogs = serializedObject.FindProperty("receiveErrorLogs");
			receiveExceptionLogs = serializedObject.FindProperty("receiveExceptionLogs");
			captureLogTimestamps = serializedObject.FindProperty("captureLogTimestamps");
			alwaysDisplayTimestamps = serializedObject.FindProperty("alwaysDisplayTimestamps");
			maxLogCount = serializedObject.FindProperty("maxLogCount");
			logsToRemoveAfterMaxLogCount = serializedObject.FindProperty("logsToRemoveAfterMaxLogCount");
			queuedLogLimit = serializedObject.FindProperty("queuedLogLimit");
			clearCommandAfterExecution = serializedObject.FindProperty("clearCommandAfterExecution");
			commandHistorySize = serializedObject.FindProperty("commandHistorySize");
			showCommandSuggestions = serializedObject.FindProperty("showCommandSuggestions");
			receiveLogcatLogsInAndroid = serializedObject.FindProperty("receiveLogcatLogsInAndroid");
			logcatArguments = serializedObject.FindProperty("logcatArguments");
			avoidScreenCutout = serializedObject.FindProperty("avoidScreenCutout");
			popupAvoidsScreenCutout = serializedObject.FindProperty("popupAvoidsScreenCutout");
			autoFocusOnCommandInputField = serializedObject.FindProperty("autoFocusOnCommandInputField");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(singleton);

			EditorGUILayout.Space();

			EditorGUILayout.PropertyField(minimumHeight);

			EditorGUILayout.PropertyField(enableHorizontalResizing);
			if (enableHorizontalResizing.boolValue)
			{
				DrawSubProperty(resizeFromRight);
				DrawSubProperty(minimumWidth);
			}

			EditorGUILayout.PropertyField(avoidScreenCutout);
			DrawSubProperty(popupAvoidsScreenCutout);

			EditorGUILayout.Space();

			EditorGUILayout.PropertyField(startMinimized);
			EditorGUILayout.PropertyField(logWindowOpacity);
			EditorGUILayout.PropertyField(popupOpacity);

			EditorGUILayout.PropertyField(popupVisibility);
			if (popupVisibility.intValue == (int) PopupVisibility.WhenLogReceived)
			{
				EditorGUI.indentLevel++;
#if UNITY_2017_3_OR_NEWER
				var rect = EditorGUILayout.GetControlRect();
				EditorGUI.BeginProperty(rect, GUIContent.none, popupVisibilityLogFilter);
				popupVisibilityLogFilter.intValue = (int) (DebugLogFilter) EditorGUI.EnumFlagsField(
					rect, popupVisibilityLogFilterLabel, (DebugLogFilter) popupVisibilityLogFilter.intValue);
#else
				EditorGUI.BeginProperty( new Rect(), GUIContent.none, popupVisibilityLogFilter );
				EditorGUI.BeginChangeCheck();

				bool infoLog =
 EditorGUILayout.Toggle( "Info", ( (DebugLogFilter) popupVisibilityLogFilter.intValue & DebugLogFilter.Info ) == DebugLogFilter.Info );
				bool warningLog =
 EditorGUILayout.Toggle( "Warning", ( (DebugLogFilter) popupVisibilityLogFilter.intValue & DebugLogFilter.Warning ) == DebugLogFilter.Warning );
				bool errorLog =
 EditorGUILayout.Toggle( "Error", ( (DebugLogFilter) popupVisibilityLogFilter.intValue & DebugLogFilter.Error ) == DebugLogFilter.Error );

				if( EditorGUI.EndChangeCheck() )
					popupVisibilityLogFilter.intValue =
 ( infoLog ? (int) DebugLogFilter.Info : 0 ) | ( warningLog ? (int) DebugLogFilter.Warning : 0 ) | ( errorLog ? (int) DebugLogFilter.Error : 0 );
#endif
				EditorGUI.EndProperty();
				EditorGUI.indentLevel--;
			}

			EditorGUILayout.PropertyField(toggleWithKey);
			if (toggleWithKey.boolValue)
				DrawSubProperty(toggleKey);

			EditorGUILayout.Space();

			EditorGUILayout.PropertyField(enableSearchbar);
			if (enableSearchbar.boolValue)
				DrawSubProperty(topSearchbarMinWidth);

			EditorGUILayout.Space();

			EditorGUILayout.PropertyField(receiveLogsWhileInactive);

			EditorGUILayout.PrefixLabel(receivedLogTypesLabel);
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(receiveInfoLogs, receiveInfoLogsLabel);
			EditorGUILayout.PropertyField(receiveWarningLogs, receiveWarningLogsLabel);
			EditorGUILayout.PropertyField(receiveErrorLogs, receiveErrorLogsLabel);
			EditorGUILayout.PropertyField(receiveExceptionLogs, receiveExceptionLogsLabel);
			EditorGUI.indentLevel--;

			EditorGUILayout.PropertyField(receiveLogcatLogsInAndroid);
			if (receiveLogcatLogsInAndroid.boolValue)
				DrawSubProperty(logcatArguments);

			EditorGUILayout.PropertyField(captureLogTimestamps);
			if (captureLogTimestamps.boolValue)
				DrawSubProperty(alwaysDisplayTimestamps);

			EditorGUILayout.PropertyField(maxLogCount);
			DrawSubProperty(logsToRemoveAfterMaxLogCount);

			EditorGUILayout.PropertyField(queuedLogLimit);

			EditorGUILayout.Space();

			EditorGUILayout.PropertyField(clearCommandAfterExecution);
			EditorGUILayout.PropertyField(commandHistorySize);
			EditorGUILayout.PropertyField(showCommandSuggestions);
			EditorGUILayout.PropertyField(autoFocusOnCommandInputField);

			EditorGUILayout.Space();

			DrawPropertiesExcluding(serializedObject, "m_Script");
			serializedObject.ApplyModifiedProperties();
		}

		private void DrawSubProperty(SerializedProperty property)
		{
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(property);
			EditorGUI.indentLevel--;
		}
	}
}
