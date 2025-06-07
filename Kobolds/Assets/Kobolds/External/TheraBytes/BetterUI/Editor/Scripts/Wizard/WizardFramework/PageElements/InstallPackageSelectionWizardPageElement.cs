using System.IO;
using UnityEditor;
using UnityEngine;

namespace TheraBytes.BetterUi.Editor
{
	public enum InstallSelectionState
	{
		None,
		Install,
		Remove
	}

	public class InstallPackageSelectionWizardPageElement : WizardPageElementBase
	{
		private readonly string title;

		public InstallPackageSelectionWizardPageElement(string title, string pathToPackage, string pathToFolder)
		{
			this.title = title;
			this.PathToPackage = pathToPackage;
			this.PathToFolder = pathToFolder;
		}

		public string PathToPackage { get; }

		public string PathToFolder { get; }

		public InstallSelectionState SelectionState { get; private set; }

		public override void DrawGui()
		{
			var isInstalled = Directory.Exists(PathToFolder);

			EditorGUILayout.BeginHorizontal();

			EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

			GUILayout.FlexibleSpace();

			if (isInstalled) EditorGUILayout.HelpBox("✓ installed", MessageType.None);


			if (isInstalled)
			{
				var remove = GUILayout.Toggle(
					SelectionState == InstallSelectionState.Remove, "Remove", EditorStyles.miniButton,
					GUILayout.Width(100));
				SelectionState = remove ? InstallSelectionState.Remove : InstallSelectionState.None;
			}
			else
			{
				var install = GUILayout.Toggle(
					SelectionState == InstallSelectionState.Install, "Install", EditorStyles.miniButton,
					GUILayout.Width(100));
				SelectionState = install ? InstallSelectionState.Install : InstallSelectionState.None;
			}

			EditorGUILayout.EndHorizontal();
		}
	}
}
