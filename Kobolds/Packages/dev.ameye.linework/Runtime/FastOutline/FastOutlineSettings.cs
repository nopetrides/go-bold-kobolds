using System;
using System.Collections.Generic;
using Linework.Common.Utils;
using UnityEditor;
using UnityEngine;

namespace Linework.FastOutline
{
	[CreateAssetMenu(fileName = "Fast Outline Settings", menuName = "Linework/Fast Outline Settings")]
	[Icon("Packages/dev.ameye.linework/Editor/Common/Icons/d_FastOutline.png")]
	public class FastOutlineSettings : ScriptableObject
	{
		[SerializeField] private InjectionPoint injectionPoint = InjectionPoint.AfterRenderingPostProcessing;
		[SerializeField] private bool showInSceneView = true;
		[SerializeField] private List<Outline> outlines = new(10);
		internal Action OnSettingsChanged;

		public InjectionPoint InjectionPoint => injectionPoint;
		public bool ShowInSceneView => showInSceneView;
		public List<Outline> Outlines => outlines;

		private void OnDestroy()
		{
			OnSettingsChanged = null;
			outlines = null;
		}

		private void OnValidate()
		{
#if UNITY_EDITOR
			if (Application.isPlaying)
				return;
			OnSettingsChanged?.Invoke();
#endif
		}

		public void Changed()
		{
			OnSettingsChanged?.Invoke();
		}

		public void SetActive(bool active)
		{
			foreach (var outline in outlines) outline.SetActive(active);
		}

#if UNITY_EDITOR
		private class OnDestroyProcessor : AssetModificationProcessor
		{
			private const string FileEnding = ".asset";
			private static readonly Type Type = typeof(FastOutlineSettings);

			public static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions _)
			{
				if (!path.EndsWith(FileEnding))
					return AssetDeleteResult.DidNotDelete;

				var assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
				if (assetType == null || (assetType != Type && !assetType.IsSubclassOf(Type)))
					return AssetDeleteResult.DidNotDelete;
				var asset = AssetDatabase.LoadAssetAtPath<FastOutlineSettings>(path);
				foreach (var outline in asset.Outlines) outline.Cleanup();
				asset.OnDestroy();

				return AssetDeleteResult.DidNotDelete;
			}
		}
#endif
	}
}
