using UnityEditor;
using UnityEngine;

namespace ModelShark
{
	public class QuickSetupMenuItem : Editor
	{
		[MenuItem("Window/ProTips/Quick Setup")]
		private static void NewMenuOption()
		{
			var tooltipManager = FindObjectOfType<TooltipManager>();
			if (tooltipManager != null)
			{
				Debug.LogWarning("Tooltip Manager already exists in this scene. Setup aborted.");
				return;
			}

			var tooltipManagerPrefab = Resources.Load<TooltipManager>("TooltipManager");
			if (tooltipManagerPrefab == null)
			{
				Debug.LogError("TooltipManager prefab could not be loaded from Resources folder. Setup aborted.");
				return;
			}

			tooltipManager = Instantiate(tooltipManagerPrefab);
			tooltipManager.name = "TooltipManager";
			tooltipManager.transform.SetAsFirstSibling();
		}
	}
}
