using UnityEditor;
using UnityEngine;

namespace Dreamteck.Splines.Editor
{
	public class SplineComputerDebugEditor : SplineEditorBase
	{
		private readonly SerializedProperty _editorAlwaysDraw;
		private readonly SerializedProperty _editorBillboardThickness;

		private readonly SerializedProperty _editorDrawPivot;
		private readonly SerializedProperty _editorDrawThickness;
		private readonly SerializedProperty _editorPathColor;
		private readonly SerializedProperty _editorUpdateMode;
		private float _length;
		private readonly DreamteckSplinesEditor _pathEditor;
		private readonly SplineComputer _spline;

		public SplineComputerDebugEditor(
			SplineComputer spline, SerializedObject serializedObject, DreamteckSplinesEditor pathEditor) : base(
			serializedObject)
		{
			_spline = spline;
			_pathEditor = pathEditor;
			GetSplineLength();
			_editorPathColor = serializedObject.FindProperty("editorPathColor");
			_editorAlwaysDraw = serializedObject.FindProperty("editorAlwaysDraw");
			_editorDrawThickness = serializedObject.FindProperty("editorDrawThickness");
			_editorBillboardThickness = serializedObject.FindProperty("editorBillboardThickness");
			_editorUpdateMode = serializedObject.FindProperty("editorUpdateMode");
			_editorDrawPivot = serializedObject.FindProperty("editorDrawPivot");
		}

		public SplineComputer.EditorUpdateMode editorUpdateMode =>
			(SplineComputer.EditorUpdateMode) _editorUpdateMode.enumValueIndex;

		private void GetSplineLength()
		{
			_length = Mathf.RoundToInt(_spline.CalculateLength() * 100f) / 100f;
		}

		public override void DrawInspector()
		{
			base.DrawInspector();
			if (Event.current.type == EventType.MouseUp) GetSplineLength();
			EditorGUI.BeginChangeCheck();

			EditorGUILayout.PropertyField(_editorUpdateMode, new GUIContent("Editor Update Mode"));
			EditorGUILayout.PropertyField(_editorPathColor, new GUIContent("Color in Scene"));
			var lastAlwaysDraw = _editorAlwaysDraw.boolValue;
			EditorGUILayout.PropertyField(_editorDrawPivot, new GUIContent("Draw Transform Pivot"));
			EditorGUILayout.PropertyField(_editorAlwaysDraw, new GUIContent("Always Draw Spline"));
			if (lastAlwaysDraw != _editorAlwaysDraw.boolValue)
			{
				if (_editorAlwaysDraw.boolValue)
				{
					for (var i = 0; i < _serializedObject.targetObjects.Length; i++)
						if (_serializedObject.targetObjects[i] is SplineComputer)
							DSSplineDrawer.RegisterComputer((SplineComputer) _serializedObject.targetObjects[i]);
				}
				else
				{
					for (var i = 0; i < _serializedObject.targetObjects.Length; i++)
						if (_serializedObject.targetObjects[i] is SplineComputer)
							DSSplineDrawer.UnregisterComputer((SplineComputer) _serializedObject.targetObjects[i]);
				}
			}

			EditorGUILayout.PropertyField(_editorDrawThickness, new GUIContent("Draw thickness"));
			if (_editorDrawThickness.boolValue)
			{
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(_editorBillboardThickness, new GUIContent("Always face camera"));
				EditorGUI.indentLevel--;
			}

			EditorGUILayout.Space();
			if (_serializedObject.targetObjects.Length == 1)
				EditorGUILayout.HelpBox(
					"Samples: " + _spline.sampleCount + "\n\r" + "Length: " + _length, MessageType.Info);
			else
				EditorGUILayout.HelpBox("Multiple spline objects selected" + _length, MessageType.Info);

			if (EditorGUI.EndChangeCheck())
			{
				if (editorUpdateMode == SplineComputer.EditorUpdateMode.Default)
				{
					for (var i = 0; i < _serializedObject.targetObjects.Length; i++)
						if (_serializedObject.targetObjects[i] is SplineComputer)
							((SplineComputer) _serializedObject.targetObjects[i]).RebuildImmediate(true);

					SceneView.RepaintAll();
				}

				_pathEditor.ApplyModifiedProperties();
			}
		}

		public override void DrawScene(SceneView current)
		{
			base.DrawScene(current);
			if (Event.current.type == EventType.MouseUp && open) GetSplineLength();
		}
	}
}
