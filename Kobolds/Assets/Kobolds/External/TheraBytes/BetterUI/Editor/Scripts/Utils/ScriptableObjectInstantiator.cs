#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace TheraBytes.BetterUi
{
	public static class ScriptableObjectInstantiator
	{
		[MenuItem("Tools/Better UI/Settings/Select Resolution Monitor", false, 0)]
		private static void SelectResolutionMonitor()
		{
			Selection.objects = new Object[] {ResolutionMonitor.Instance};
		}

		[MenuItem("Tools/Better UI/Settings/Select Material Definitions", false, 1)]
		private static void SelectMaterials()
		{
			Selection.objects = new Object[] {Materials.Instance};
		}

		[MenuItem("Tools/Better UI/Settings/Ensure Singleton Resources", false, 30)]
		private static void ManualInitialize()
		{
			if (ResolutionMonitor.HasInstance && Materials.HasInstance)
			{
				Debug.Log("Instances already present. Please Check \"Assets/Thera Bytes/Resources\"");
				return;
			}

			ResolutionMonitor.EnsureInstance();
			Materials.EnsureInstance();

			Debug.Log("Instances have been created. Please Check  \"Assets/Thera Bytes/Resources\"");
		}
	}
}
#endif
