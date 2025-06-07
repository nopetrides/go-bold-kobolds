using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Dreamteck.Splines.Editor
{
	public class DSCreatePointModule : CreatePointModule
	{
		private readonly DreamteckSplinesEditor dsEditor;
		private bool createNode;

		public DSCreatePointModule(SplineEditor editor) : base(editor)
		{
			dsEditor = (DreamteckSplinesEditor) editor;
		}

		public override void LoadState()
		{
			base.LoadState();
			createNode = LoadBool("createNode");
		}

		public override void SaveState()
		{
			base.SaveState();
			SaveBool("createNode", createNode);
		}

		protected override void OnDrawInspector()
		{
			base.OnDrawInspector();
			createNode = EditorGUILayout.Toggle("Create Node", createNode);
		}

		protected override void CreateSplinePoint(Vector3 position, Vector3 normal)
		{
			GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
			var indices = new List<int>();
			var nodes = new List<Node>();
			var spline = dsEditor.spline;

			dsEditor.CacheTriggerPositions();

			if (!isClosed && points.Length >= 3)
			{
				var first = HandleUtility.WorldToGUIPoint(points[0].position);
				var last = HandleUtility.WorldToGUIPoint(position);
				if (Vector2.Distance(first, last) <= 20f)
					if (EditorUtility.DisplayDialog(
							"Close spline?", "Do you want to make the spline path closed ?", "Yes", "No"))
					{
						editor.SetSplineClosed(true);
						spline.EditorSetAllPointsDirty();
						RegisterChange();
						SceneView.currentDrawingSceneView.Focus();
						SceneView.RepaintAll();
						return;
					}
			}

			AddPoint();

			if (appendMode == AppendMode.End)
				for (var i = 0; i < indices.Count; i++)
					nodes[i].AddConnection(spline, indices[i] + 1);

			dsEditor.ApplyModifiedProperties(true);
			dsEditor.WriteTriggerPositions();
			RegisterChange();
			if (appendMode == AppendMode.Beginning) spline.ShiftNodes(0, spline.pointCount - 1, 1);

			if (createNode)
			{
				dsEditor.ApplyModifiedProperties();
				if (appendMode == 0)
					CreateNodeForPoint(0);
				else
					CreateNodeForPoint(points.Length - 1);
			}
		}

		protected override void InsertMode(Vector3 screenCoordinates)
		{
			base.InsertMode(screenCoordinates);
			var percent = ProjectScreenSpace(screenCoordinates);
			editor.evaluate(percent, ref evalResult);
			if (editor.eventModule.mouseRight)
			{
				SplineEditorHandles.DrawCircle(
					evalResult.position, Quaternion.LookRotation(editorCamera.transform.position - evalResult.position),
					HandleUtility.GetHandleSize(evalResult.position) * 0.2f);
				return;
			}

			if (SplineEditorHandles.CircleButton(
					evalResult.position, Quaternion.LookRotation(editorCamera.transform.position - evalResult.position),
					HandleUtility.GetHandleSize(evalResult.position) * 0.2f, 1.5f, color))
			{
				dsEditor.CacheTriggerPositions();
				var newPoint = new SplinePoint(evalResult.position, evalResult.position);
				newPoint.size = evalResult.size;
				newPoint.color = evalResult.color;
				newPoint.normal = evalResult.up;


				var pointIndex = dsEditor.spline.PercentToPointIndex(percent);
				editor.AddPointAt(pointIndex + 1);
				points[pointIndex + 1].SetPoint(newPoint);
				var spline = dsEditor.spline;
				lastCreated = points.Length - 1;
				editor.ApplyModifiedProperties(true);
				spline.ShiftNodes(pointIndex + 1, spline.pointCount - 1, 1);
				if (createNode) CreateNodeForPoint(pointIndex + 1);
				RegisterChange();
				dsEditor.WriteTriggerPositions();
			}
		}

		private void CreateNodeForPoint(int index)
		{
			var obj = new GameObject("Node_" + (points.Length - 1));
			obj.transform.parent = dsEditor.spline.transform;
			var node = obj.AddComponent<Node>();
			node.transform.localRotation = Quaternion.identity;
			node.transform.position = points[index].position;
			Undo.SetCurrentGroupName("Create Node For Point " + index);
			Undo.RegisterCreatedObjectUndo(obj, "Create Node object");
			Undo.RegisterCompleteObjectUndo(dsEditor.spline, "Link Node");
			dsEditor.spline.ConnectNode(node, index);
		}
	}
}
