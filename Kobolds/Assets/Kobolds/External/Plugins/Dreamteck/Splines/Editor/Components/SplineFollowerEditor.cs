using UnityEditor;
using UnityEngine;

namespace Dreamteck.Splines.Editor
{
	[CustomEditor(typeof(SplineFollower), true)]
	[CanEditMultipleObjects]
	public class SplineFollowerEditor : SplineTracerEditor
	{
		protected SplineFollower[] followers = new SplineFollower[0];
		private SplineSample result;
		protected FollowerSpeedModifierEditor speedModifierEditor;

		protected override void OnEnable()
		{
			base.OnEnable();
			followers = new SplineFollower[users.Length];
			for (var i = 0; i < followers.Length; i++) followers[i] = (SplineFollower) users[i];

			if (followers.Length == 1) speedModifierEditor = new FollowerSpeedModifierEditor(followers[0], this);
		}

		private void OnSetDistance(float distance)
		{
			for (var i = 0; i < targets.Length; i++)
			{
				var follower = (SplineFollower) targets[i];
				var travel = follower.Travel(0.0, distance);
				var startPosition = serializedObject.FindProperty("_startPosition");
				startPosition.floatValue = (float) travel;
				follower.SetPercent(travel);
				EditorUtility.SetDirty(follower);
			}
		}

		protected override void BodyGUI()
		{
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Following", EditorStyles.boldLabel);
			var follower = (SplineFollower) target;

			var followMode = serializedObject.FindProperty("followMode");
			var preserveUniformSpeedWithOffset = serializedObject.FindProperty("preserveUniformSpeedWithOffset");
			var wrapMode = serializedObject.FindProperty("wrapMode");
			var startPosition = serializedObject.FindProperty("_startPosition");
			var autoStartPosition = serializedObject.FindProperty("autoStartPosition");
			var follow = serializedObject.FindProperty("_follow");
			var direction = serializedObject.FindProperty("_direction");
			var unityOnEndReached = serializedObject.FindProperty("_unityOnEndReached");
			var unityOnBeginningReached = serializedObject.FindProperty("_unityOnBeginningReached");

			EditorGUI.BeginChangeCheck();

			var lastFollow = follow.boolValue;
			EditorGUILayout.PropertyField(follow);
			if (lastFollow != follow.boolValue)
				if (follow.boolValue)
					if (autoStartPosition.boolValue)
					{
						var sample = new SplineSample();
						followers[0].Project(followers[0].transform.position, ref sample);
						if (Application.isPlaying)
							for (var i = 0; i < followers.Length; i++)
								followers[i].SetPercent(sample.percent);
					}

			EditorGUILayout.PropertyField(followMode);
			if (followMode.intValue == (int) SplineFollower.FollowMode.Uniform)
			{
				var followSpeed = serializedObject.FindProperty("_followSpeed");

				if (followSpeed.floatValue < 0f)
					direction.intValue = (int) Spline.Direction.Backward;
				else if (followSpeed.floatValue > 0f) direction.intValue = (int) Spline.Direction.Forward;

				var motion = serializedObject.FindProperty("_motion");
				var motionHasOffset = motion.FindPropertyRelative("_hasOffset");

				EditorGUILayout.PropertyField(followSpeed, new GUIContent("Follow Speed"));


				if (motionHasOffset.boolValue)
					EditorGUILayout.PropertyField(
						preserveUniformSpeedWithOffset, new GUIContent("Preserve Uniform Speed With Offset"));
				if (followers.Length == 1) speedModifierEditor.DrawInspector();
			}
			else
			{
				follower.followDuration = EditorGUILayout.FloatField("Follow duration", follower.followDuration);
			}


			EditorGUILayout.PropertyField(wrapMode);


			if (follower.motion.applyRotation)
				follower.applyDirectionRotation = EditorGUILayout.Toggle(
					"Face Direction", follower.applyDirectionRotation);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Start Position", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(autoStartPosition, new GUIContent("Automatic Start Position"));
			EditorGUILayout.BeginHorizontal();
			EditorGUIUtility.labelWidth = 100f;
			if (!follower.autoStartPosition && !Application.isPlaying)
			{
				var lastStartpos = startPosition.floatValue;
				EditorGUILayout.PropertyField(startPosition, new GUIContent("Start Position"));
				if (GUILayout.Button("Set Distance", GUILayout.Width(85)))
				{
					var w = EditorWindow.GetWindow<DistanceWindow>(true);
					w.Init(OnSetDistance, follower.CalculateLength());
				}
			}
			else
			{
				EditorGUILayout.LabelField("Start position", GUILayout.Width(EditorGUIUtility.labelWidth));
			}

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.PropertyField(unityOnBeginningReached);
			EditorGUILayout.PropertyField(unityOnEndReached);

			if (EditorGUI.EndChangeCheck())
			{
				serializedObject.ApplyModifiedProperties();
				if (!Application.isPlaying)
					for (var i = 0; i < followers.Length; i++)
						if (followers[i].spline.sampleCount > 0)
							if (!followers[i].autoStartPosition)
							{
								followers[i].SetPercent(startPosition.floatValue);
								if (!followers[i].follow) SceneView.RepaintAll();
							}
			}

			var lastDirection = direction.intValue;
			base.BodyGUI();

			if (lastDirection != direction.intValue)
			{
				var followSpeed = serializedObject.FindProperty("_followSpeed");
				if (direction.intValue == (int) Spline.Direction.Forward)
					followSpeed.floatValue = Mathf.Abs(followSpeed.floatValue);
				else
					followSpeed.floatValue = -Mathf.Abs(followSpeed.floatValue);
			}
		}


		protected override void DuringSceneGUI(SceneView currentSceneView)
		{
			base.DuringSceneGUI(currentSceneView);
			var user = (SplineFollower) target;
			if (user == null) return;
			if (Application.isPlaying)
			{
				if (!user.follow) DrawResult(user.result);
				return;
			}

			if (user.spline == null) return;
			if (user.autoStartPosition)
			{
				user.spline.Project(user.transform.position, ref result, user.clipFrom, user.clipTo);
				DrawResult(result);
			}
			else if (!user.follow)
			{
				DrawResult(user.result);
			}

			if (followers.Length == 1) speedModifierEditor.DrawScene();
		}
	}
}
