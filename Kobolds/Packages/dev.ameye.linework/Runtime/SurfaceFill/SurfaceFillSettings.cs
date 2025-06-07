using System;
using System.Collections.Generic;
using Linework.Common.Utils;
using UnityEditor;
using UnityEngine;

namespace Linework.SurfaceFill
{
	[CreateAssetMenu(fileName = "Surface Fill Settings", menuName = "Linework/Surface Fill Settings")]
	[Icon("Packages/dev.ameye.linework/Editor/Common/Icons/d_Fill.png")]
	public class SurfaceFillSettings : ScriptableObject
	{
		[SerializeField] private InjectionPoint injectionPoint = InjectionPoint.AfterRenderingPostProcessing;
		[SerializeField] private bool showInSceneView = true;
		[SerializeField] private List<Fill> fills = new(8);
		internal Action OnSettingsChanged;

		public InjectionPoint InjectionPoint => injectionPoint;
		public bool ShowInSceneView => showInSceneView;
		public List<Fill> Fills => fills;

		private void OnDestroy()
		{
			OnSettingsChanged = null;
			fills = null;
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
			foreach (var fill in fills) fill.SetActive(active);
		}

#if UNITY_EDITOR
		private class OnDestroyProcessor : AssetModificationProcessor
		{
			private const string FileEnding = ".asset";
			private static readonly Type Type = typeof(SurfaceFillSettings);

			public static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions _)
			{
				if (!path.EndsWith(FileEnding))
					return AssetDeleteResult.DidNotDelete;

				var assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
				if (assetType == null || (assetType != Type && !assetType.IsSubclassOf(Type)))
					return AssetDeleteResult.DidNotDelete;
				var asset = AssetDatabase.LoadAssetAtPath<SurfaceFillSettings>(path);
				foreach (var fill in asset.Fills) fill.Cleanup();
				asset.OnDestroy();

				return AssetDeleteResult.DidNotDelete;
			}
		}
#endif
	}
}
