using UnityEditor;

namespace Febucci.UI.Core.Editors
{
	[CustomEditor(typeof(TypewriterByCharacter), true)]
	internal class TypewriterByCharacterDrawer : TypewriterCoreDrawer
	{
		private SerializedProperty avoidMultiplePunctuationWait;
		private PropertyWithDifferentLabel disappearanceSpeedMultiplier;
		private PropertyWithDifferentLabel disappearanceWaitTime;

		private PropertyWithDifferentLabel useTypewriterWaitForDisappearances;
		private SerializedProperty waitForLastCharacter;
		private SerializedProperty waitForNewLines;
		private SerializedProperty waitForNormalChars;
		private SerializedProperty waitLong;
		private SerializedProperty waitMiddle;

		protected override void OnEnable()
		{
			base.OnEnable();

			waitForNormalChars = serializedObject.FindProperty("waitForNormalChars");
			waitLong = serializedObject.FindProperty("waitLong");
			waitMiddle = serializedObject.FindProperty("waitMiddle");
			avoidMultiplePunctuationWait = serializedObject.FindProperty("avoidMultiplePunctuationWait");
			waitForNewLines = serializedObject.FindProperty("waitForNewLines");
			waitForLastCharacter = serializedObject.FindProperty("waitForLastCharacter");
			useTypewriterWaitForDisappearances = new PropertyWithDifferentLabel(
				serializedObject, "useTypewriterWaitForDisappearances", "Use Typewriter Wait Times");
			disappearanceSpeedMultiplier = new PropertyWithDifferentLabel(
				serializedObject, "disappearanceSpeedMultiplier", "Typewriter Speed Multiplier");
			disappearanceWaitTime = new PropertyWithDifferentLabel(
				serializedObject, "disappearanceWaitTime", "Disappearances Wait");
		}

		protected override string[] GetPropertiesToExclude()
		{
			var newProperties = new[]
			{
				"script",
				"waitForNormalChars",
				"waitLong",
				"waitMiddle",
				"avoidMultiplePunctuactionWait",
				"avoidMultiplePunctuationWait",
				"waitForNewLines",
				"waitForLastCharacter",
				"useTypewriterWaitForDisappearances",
				"disappearanceSpeedMultiplier",
				"disappearanceWaitTime"
			};

			var baseProperties = base.GetPropertiesToExclude();

			var mergedArray = new string[newProperties.Length + baseProperties.Length];

			for (var i = 0; i < baseProperties.Length; i++) mergedArray[i] = baseProperties[i];

			for (var i = 0; i < newProperties.Length; i++) mergedArray[i + baseProperties.Length] = newProperties[i];

			return mergedArray;
		}

		protected override void OnTypewriterSectionGUI()
		{
			EditorGUILayout.PropertyField(waitForNormalChars);
			EditorGUILayout.PropertyField(waitLong);
			EditorGUILayout.PropertyField(waitMiddle);

			EditorGUILayout.PropertyField(avoidMultiplePunctuationWait);
			EditorGUILayout.PropertyField(waitForNewLines);
			EditorGUILayout.PropertyField(waitForLastCharacter);
		}

		protected override void OnDisappearanceSectionGUI()
		{
			useTypewriterWaitForDisappearances.PropertyField();

			if (useTypewriterWaitForDisappearances.property.boolValue)
				disappearanceSpeedMultiplier.PropertyField();
			else
				disappearanceWaitTime.PropertyField();
		}
	}
}
