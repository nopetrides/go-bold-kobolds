using UnityEngine;
using UnityEngine.EventSystems;

namespace FIMSpace.BonesStimulation
{
	public partial class BonesStimulator : IDropHandler, IFHierarchyIcon
	{
		public enum EStimulationMode
		{
			Muscles,
			Vibrate,
			Squeezing,
			Collisions
		}

		public EStimulationMode _editor_SelCategory = EStimulationMode.Muscles;

#region Hierarchy Icon

		public string EditorIconPath
		{
			get
			{
				if (PlayerPrefs.GetInt("AnimsH", 1) == 0) return "";
				return "Bones Stimulator/BonesStimulator";
			}
		}

		public void OnDrop(PointerEventData data)
		{
		}

#endregion


#region Editor Helpers

		[HideInInspector] public string _editor_Title = " Bones Stimulator";

		[HideInInspector] public bool _editor_DrawSetup = true;
		[HideInInspector] public bool _editor_DrawTweaking;

		[HideInInspector] public int _editor_DisplayedPreset;
		[HideInInspector] public bool _editor_DrawGizmos = true;

		public bool DrawGizmos = true;
		//[HideInInspector] public Type _editor_ViewCategory;
		//[HideInInspector] public EMD_SetupCategory _editor_SetupCategory = EMD_SetupCategory.Movement;

#endregion


#region Performance Measuring

		private void MeasurePerformanceUpdate(bool start)
		{
#if UNITY_EDITOR
			if (start) _perf_preUpd.Start(gameObject);
			else _perf_preUpd.Finish();
#endif
		}

		private void MeasurePerformanceMain(bool start)
		{
#if UNITY_EDITOR
			if (start) _perf_main.Start(gameObject);
			else _perf_main.Finish();
#endif
		}


#if UNITY_EDITOR

		public FDebug_PerformanceTest _perf_preUpd = new();
		public FDebug_PerformanceTest _perf_main = new();

#endif

#endregion
	}
}
