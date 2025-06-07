using UnityEditor;
using UnityEngine;

namespace FIMSpace.BonesStimulation
{
	public partial class BonesStimulator_Editor
	{
		private static Texture2D __texStimulIcon;
		private static Texture2D __texRot;
		private static Texture2D __texSquash;
		private static Texture2D __texMuscle;


		private static Object _manualFile;
		private static GUIStyle _smallStyle;

		private static bool drawDefaultInspector;
		private BonesStimulator _get;
		private Color c;
		private bool e;

		private SerializedProperty sp_Amount;
		private SerializedProperty sp_AutoHelperOffset;
		private SerializedProperty sp_BlendXAxis;
		private SerializedProperty sp_Bones;
		private SerializedProperty sp_Compens;
		private SerializedProperty sp_DistanceFrom;
		private SerializedProperty sp_Grav;
		private SerializedProperty sp_GravH;
		private SerializedProperty sp_InflAxes;
		private SerializedProperty sp_MovementMuscles;
		private SerializedProperty sp_MusclesCurve;
		private SerializedProperty sp_Rate;
		private SerializedProperty sp_RotationSpaceMuscles;
		private SerializedProperty sp_SqueezingAmount;
		private SerializedProperty sp_UseCollisions;

		private SerializedProperty sp_VibrateAmount;
		// RESOURCES ----------------------------------------

		public static Texture2D _TexStimulIcon
		{
			get
			{
				if (__texStimulIcon != null) return __texStimulIcon;
				__texStimulIcon = Resources.Load<Texture2D>("Bones Stimulator/BonesStimulator");
				return __texStimulIcon;
			}
		}

		public static Texture2D _TexRotationAuto
		{
			get
			{
				if (__texRot != null) return __texRot;
				__texRot = Resources.Load<Texture2D>("Bones Stimulator/GearIconAuto");
				return __texRot;
			}
		}

		public static Texture2D _TexSquashing
		{
			get
			{
				if (__texSquash != null) return __texSquash;
				__texSquash = Resources.Load<Texture2D>("Bones Stimulator/SquashIcon");
				return __texSquash;
			}
		}

		public static Texture2D _TexMuscleIcon
		{
			get
			{
				if (__texMuscle != null) return __texMuscle;
				__texMuscle = Resources.Load<Texture2D>("Bones Stimulator/MusclesIcon");
				return __texMuscle;
			}
		}

		private static GUIStyle smallStyle
		{
			get
			{
				if (_smallStyle == null)
					_smallStyle = new GUIStyle(EditorStyles.miniLabel) {fontStyle = FontStyle.Italic};
				return _smallStyle;
			}
		}

		// HELPER VARIABLES ----------------------------------------

		private BonesStimulator Get
		{
			get
			{
				if (_get == null) _get = target as BonesStimulator;
				return _get;
			}
		}

		private void OnEnable()
		{
			sp_Amount = serializedObject.FindProperty("StimulatorAmount");
			sp_Bones = serializedObject.FindProperty("Bones");
			sp_MovementMuscles = serializedObject.FindProperty("MovementMuscles");
			sp_RotationSpaceMuscles = serializedObject.FindProperty("RotationSpaceMuscles");
			sp_MusclesCurve = serializedObject.FindProperty("MusclesBlend");
			sp_VibrateAmount = serializedObject.FindProperty("VibrateAmount");
			sp_SqueezingAmount = serializedObject.FindProperty("SqueezingAmount");
			sp_Rate = serializedObject.FindProperty("UpdateRate");
			sp_Grav = serializedObject.FindProperty("GravityEffectForce");
			sp_GravH = serializedObject.FindProperty("GravityHeavyness");
			sp_Compens = serializedObject.FindProperty("CompensationTransform");
			sp_InflAxes = serializedObject.FindProperty("InfluenceAxes");
			sp_DistanceFrom = serializedObject.FindProperty("DistanceFrom");
			sp_UseCollisions = serializedObject.FindProperty("UseCollisions");
			sp_AutoHelperOffset = serializedObject.FindProperty("AutoHelperOffset");
			sp_BlendXAxis = serializedObject.FindProperty("BlendXAxis");
		}


		protected virtual void OnSceneGUI()
		{
			if (Application.isPlaying) return;
			if (!Get.DrawGizmos) return;

			if (Get.MovementMuscles > 0f || Get.RotationSpaceMuscles > 0f)
				if (Get.AutoHelperOffset == false)
					if (Get._editor_DrawSetup)
						if (Get.GetLastTransform() != null)
							if (Get.GetLastTransform().parent)
								if (!Get.HelperOffset.VIsZero())
								{
									Undo.RecordObject(Get, "position of bones stimulator offset");
									var root = Get.GetLastTransform();

									var off = root.TransformVector(Get.HelperOffset);
									var pos = root.position + off;
									var transformed = FEditor_TransformHandles.PositionHandle(
										pos, root.rotation, .3f, true, false);

									if (Vector3.Distance(transformed, pos) > 0.00001f)
									{
										var diff = transformed - pos;
										Get.HelperOffset = root.InverseTransformVector(off + diff);
										var obj = new SerializedObject(Get);
										if (obj != null)
										{
											obj.ApplyModifiedProperties();
											obj.Update();
										}
									}
								}
		}
	}
}
