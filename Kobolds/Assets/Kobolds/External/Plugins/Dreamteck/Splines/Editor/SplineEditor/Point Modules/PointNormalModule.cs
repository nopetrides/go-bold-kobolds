using System;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Dreamteck.Splines.Editor
{
	public class PointNormalModule : PointModule
	{
		public enum NormalMode
		{
			Auto,
			Free
		}

		private int _normalOperation;

		private readonly string[] _normalOperations = new string[0];

		private NormalRotationWindow _rotationWindow;
		private SplineSample evalResult;
		public NormalMode normalMode = NormalMode.Auto;

		public PointNormalModule(SplineEditor editor) : base(editor)
		{
			_normalOperations = new[]
			{
				"Flip",
				"Look At Camera",
				"Align with Camera",
				"Calculate",
				"Look Left",
				"Look Right",
				"Look Up",
				"Look Down",
				"Look Forward",
				"Look Back",
				"Look At Avg. Center",
				"Perpendicular to Spline",
				"Rotate Degrees"
			};
		}

		public override void LoadState()
		{
			base.LoadState();
			normalMode = (NormalMode) LoadInt("normalMode");
			_normalOperation = LoadInt("normalOperation");
		}

		public override void SaveState()
		{
			base.SaveState();
			SaveInt("normalMode", (int) normalMode);
			SaveInt("normalOperation", _normalOperation);
			if (_rotationWindow != null) _rotationWindow.Close();
		}

		public override GUIContent GetIconOff()
		{
			return IconContent("N", "normal", "Set Point Normals");
		}

		public override GUIContent GetIconOn()
		{
			return IconContent("N", "normal_on", "Set Point Normals");
		}

		private void OnNormalRotationApplied()
		{
			editor.ApplyModifiedProperties(true);
			RegisterChange();
			SceneView.RepaintAll();
		}

		private void SetNormals(int mode)
		{
			if (mode == 12)
			{
				_rotationWindow = EditorWindow.GetWindow<NormalRotationWindow>(true);
				_rotationWindow.Init(this, OnNormalRotationApplied);
				return;
			}

			var avg = Vector3.zero;
			for (var i = 0; i < selectedPoints.Count; i++) avg += points[selectedPoints[i]].position;
			if (selectedPoints.Count > 1) avg /= selectedPoints.Count;
			var editorCamera = SceneView.lastActiveSceneView.camera;

			for (var i = 0; i < selectedPoints.Count; i++)
				switch (mode)
				{
					case 0: points[selectedPoints[i]].normal *= -1; break;
					case 1:
						points[selectedPoints[i]].normal = Vector3.Normalize(
							editorCamera.transform.position - points[selectedPoints[i]].position); break;
					case 2: points[selectedPoints[i]].normal = editorCamera.transform.forward; break;
					case 3:
						points[selectedPoints[i]].normal =
							CalculatePointNormal(points, selectedPoints[i], isClosed); break;
					case 4: points[selectedPoints[i]].normal = Vector3.left; break;
					case 5: points[selectedPoints[i]].normal = Vector3.right; break;
					case 6: points[selectedPoints[i]].normal = Vector3.up; break;
					case 7: points[selectedPoints[i]].normal = Vector3.down; break;
					case 8: points[selectedPoints[i]].normal = Vector3.forward; break;
					case 9: points[selectedPoints[i]].normal = Vector3.back; break;
					case 10:
						points[selectedPoints[i]].normal =
							Vector3.Normalize(avg - points[selectedPoints[i]].position); break;
					case 11:
						var result = new SplineSample();
						editor.evaluateAtPoint(selectedPoints[i], ref result);
						points[selectedPoints[i]].normal = Vector3.Cross(result.forward, result.right).normalized;
						break;
				}

			RegisterChange();
			SceneView.RepaintAll();
		}

		public static Vector3 CalculatePointNormal(SerializedSplinePoint[] points, int index, bool isClosed)
		{
			if (points.Length < 3)
			{
				Debug.Log("Spline needs to have at least 3 control points in order to calculate normals");
				return Vector3.zero;
			}

			var side1 = Vector3.zero;
			var side2 = Vector3.zero;
			if (index == 0)
			{
				if (isClosed)
				{
					side1 = points[index].position - points[index + 1].position;
					side2 = points[index].position - points[points.Length - 2].position;
				}
				else
				{
					side1 = points[0].position - points[1].position;
					side2 = points[0].position - points[2].position;
				}
			}
			else if (index == points.Length - 1)
			{
				side1 = points[points.Length - 1].position - points[points.Length - 3].position;
				side2 = points[points.Length - 1].position - points[points.Length - 2].position;
			}
			else
			{
				side1 = points[index].position - points[index + 1].position;
				side2 = points[index].position - points[index - 1].position;
			}

			return Vector3.Cross(side1.normalized, side2.normalized).normalized;
		}

		protected override void OnDrawInspector()
		{
			if (editor.is2D)
			{
				EditorGUILayout.LabelField("Normal editing unavailable in 2D Mode", EditorStyles.centeredGreyMiniLabel);
				return;
			}

			normalMode = (NormalMode) EditorGUILayout.EnumPopup("Normal Mode", normalMode);


			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Normal Operations");

			EditorGUILayout.BeginVertical();

			_normalOperation = EditorGUILayout.Popup(_normalOperation, _normalOperations);
			if (GUILayout.Button("Apply")) SetNormals(_normalOperation);
			EditorGUILayout.EndVertical();

			EditorGUILayout.EndHorizontal();
		}

		protected override void OnDrawScene()
		{
			if (editor.is2D) return;
			for (var i = 0; i < selectedPoints.Count; i++)
				if (normalMode == NormalMode.Free) FreeNormal(selectedPoints[i]);
				else AutoNormal(selectedPoints[i]);
		}

		private void AutoNormal(int index)
		{
			editor.evaluateAtPoint(index, ref evalResult);
			Handles.color = highlightColor;
			Handles.DrawWireDisc(
				points[index].position, evalResult.forward, HandleUtility.GetHandleSize(points[index].position) * 0.5f);
			Handles.color = color;
			var matrix = Matrix4x4.TRS(points[index].position, evalResult.rotation, Vector3.one);
			var pos = points[index].position +
					points[index].normal * HandleUtility.GetHandleSize(points[index].position) * 0.5f;
			Handles.DrawLine(points[index].position, pos);
			var lastPos = pos;
			var lastLocalPos = matrix.inverse.MultiplyPoint(pos);
			pos = SplineEditorHandles.FreeMoveHandle(
				pos, HandleUtility.GetHandleSize(pos) * 0.1f, Vector3.zero, Handles.CircleHandleCap);
			if (pos != lastPos)
			{
				pos = matrix.inverse.MultiplyPoint(pos);
				var delta = pos - lastLocalPos;
				for (var n = 0; n < selectedPoints.Count; n++)
				{
					if (selectedPoints[n] == index) continue;
					editor.evaluateAtPoint(selectedPoints[n], ref evalResult);
					var localMatrix = Matrix4x4.TRS(
						points[selectedPoints[n]].position, evalResult.rotation, Vector3.one);
					var localPos = localMatrix.inverse.MultiplyPoint(
						points[selectedPoints[n]].position + points[selectedPoints[n]].normal *
						HandleUtility.GetHandleSize(points[selectedPoints[n]].position) * 0.5f);
					localPos += delta;
					localPos.z = 0f;
					points[selectedPoints[n]].normal =
						(localMatrix.MultiplyPoint(localPos) - points[selectedPoints[n]].position).normalized;
				}

				pos.z = 0f;
				pos = matrix.MultiplyPoint(pos);
				points[index].normal = (pos - points[index].position).normalized;
				RegisterChange();
			}
		}

		private void FreeNormal(int index)
		{
			Handles.color = highlightColor;
			Handles.DrawWireDisc(
				points[index].position, points[index].normal,
				HandleUtility.GetHandleSize(points[index].position) * 0.25f);
			Handles.DrawWireDisc(
				points[index].position, points[index].normal,
				HandleUtility.GetHandleSize(points[index].position) * 0.5f);
			Handles.color = color;
			Handles.DrawLine(
				points[index].position,
				points[index].position + HandleUtility.GetHandleSize(points[index].position) * points[index].normal);
			var normalPos = points[index].position +
							points[index].normal * HandleUtility.GetHandleSize(points[index].position);
			var lastNormal = points[index].normal;
			normalPos = SplineEditorHandles.FreeMoveCircle(normalPos, HandleUtility.GetHandleSize(normalPos) * 0.1f);
			normalPos -= points[index].position;
			normalPos.Normalize();
			if (normalPos == Vector3.zero) normalPos = Vector3.up;
			if (lastNormal != normalPos)
			{
				Debug.Log(Random.Range(0, 10000));
				points[index].normal = normalPos;
				var delta = Quaternion.FromToRotation(lastNormal, normalPos);
				for (var n = 0; n < selectedPoints.Count; n++)
				{
					if (selectedPoints[n] == index) continue;
					points[selectedPoints[n]].normal = delta * points[selectedPoints[n]].normal;
				}

				RegisterChange();
			}
		}

		private class NormalRotationWindow : EditorWindow
		{
			private float _angle;
			private PointNormalModule _normalModule;
			private Action _onRotationApplied;

			private void OnGUI()
			{
				if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.KeypadEnter ||
																Event.current.keyCode == KeyCode.Return))
					ApplyRotationAndClose();

				_angle = EditorGUILayout.FloatField("Angle", _angle);
				if (GUILayout.Button("Rotate")) ApplyRotationAndClose();
			}

			public void Init(PointNormalModule module, Action onRotationApplied)
			{
				_normalModule = module;
				_onRotationApplied = onRotationApplied;
				titleContent = new GUIContent("Rotate Normal");
				minSize = maxSize = new Vector2(240, 90);
				_angle = EditorPrefs.GetFloat(
					"Dreamteck.Splines.Editor.PointNormalModule.NormalRotationWindow.angle", 0f);
			}

			private void ApplyRotationAndClose()
			{
				var sample = new SplineSample();
				for (var i = 0; i < _normalModule.selectedPoints.Count; i++)
				{
					var pointIndex = _normalModule.selectedPoints[i];
					_normalModule.editor.evaluateAtPoint(pointIndex, ref sample);
					var rotation = Quaternion.AngleAxis(-_angle, sample.forward);
					_normalModule.points[pointIndex].normal = rotation * _normalModule.points[pointIndex].normal;
					_normalModule.points[pointIndex].changed = true;
				}

				if (_onRotationApplied != null) _onRotationApplied();
				EditorPrefs.SetFloat("Dreamteck.Splines.Editor.PointNormalModule.NormalRotationWindow.angle", _angle);
				Close();
			}
		}
	}
}
