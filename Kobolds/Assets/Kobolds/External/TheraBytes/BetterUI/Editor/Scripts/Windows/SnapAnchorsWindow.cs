using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheraBytes.BetterUi.Editor
{
	public class SnapAnchorsWindow : EditorWindow
	{
		public enum AnchorMode
		{
			Border,
			Point
		}

		private const string HighlightColor = "#0ef05d";

		private Texture2D allBorderPic,
						verticalBorderPic,
						horizontalBorderPic,
						matchParentPic,
						pointPic,
						verticalPointPic,
						horizontalPointPic;

		private AnchorMode mode = AnchorMode.Border;


		private List<RectTransform> objects;
		private bool parentPosition;
		private Vector2 point = new(0.5f, 0.5f);

		private GUIStyle setPivotStyle, selectPointStyle;

		private void OnEnable()
		{
			minSize = new Vector2(195, 245);

			Selection.selectionChanged += Repaint;

			allBorderPic = Resources.Load<Texture2D>("snap_all_edges");
			pointPic = Resources.Load<Texture2D>("snap_all_direction_point");
			horizontalPointPic = Resources.Load<Texture2D>("snap_horizontal_point");
			verticalPointPic = Resources.Load<Texture2D>("snap_vertical_point");
			horizontalBorderPic = Resources.Load<Texture2D>("snap_horizontal_edges");
			verticalBorderPic = Resources.Load<Texture2D>("snap_vertical_edges");
			matchParentPic = Resources.Load<Texture2D>("snap_to_parent");
		}

		private void OnGUI()
		{
#region init styles

			if (selectPointStyle == null)
			{
				selectPointStyle = new GUIStyle(EditorStyles.helpBox);
				selectPointStyle.margin = new RectOffset(0, 0, 0, 0);
				selectPointStyle.richText = true;
				selectPointStyle.alignment = TextAnchor.MiddleCenter;
			}

			if (setPivotStyle == null)
			{
				setPivotStyle = new GUIStyle(EditorStyles.miniButton);
				setPivotStyle.richText = true;
				setPivotStyle.alignment = TextAnchor.MiddleCenter;
			}

#endregion

			objects = Selection.gameObjects
				.Where(o => o.transform is RectTransform)
				.Select(o => o.transform as RectTransform)
				.ToList();


			EditorGUILayout.Space();
			DrawModeSelection();
			EditorGUILayout.Space();

			var active = objects.Count > 0;
			if (!active) EditorGUI.BeginDisabledGroup(true);

			if (objects.Count > 0)
			{
				var txt = objects.Count == 1 ? objects[0].name : string.Format("{0} UI Elements", objects.Count);
				EditorGUILayout.LabelField(txt, EditorStyles.centeredGreyMiniLabel);
			}
			else
			{
				var warn = GUI.skin.GetStyle("WarningOverlay");
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(5);
				GUILayout.TextArea("No UI Element selected.", warn);
				GUILayout.Space(5);
				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.Space();


			switch (mode)
			{
				case AnchorMode.Border:
				{
					EditorGUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();

					if (GUILayout.Button(
							new GUIContent(allBorderPic, "Snap to all borders"), GUILayout.Width(120),
							GUILayout.Height(120)))
						SnapBorder(true, true, true, true);


					// TOP DOWN
					if (GUILayout.Button(
							new GUIContent(verticalBorderPic, "Snap to top and bottom border"), GUILayout.Width(60),
							GUILayout.Height(120)))
						SnapBorder(false, false, true, true);

					GUILayout.FlexibleSpace();
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();
					// LEFT RIGHT
					if (GUILayout.Button(
							new GUIContent(horizontalBorderPic, "Snap to left and right border"), GUILayout.Width(120),
							GUILayout.Height(60)))
						SnapBorder(true, true, false, false);

					// EditorGUILayout.LabelField("", GUILayout.Width(60));

					if (GUILayout.Button(
							new GUIContent(
								matchParentPic, "Resize to the size of parent and set the anchors to the borders."),
							GUILayout.Width(60), GUILayout.Height(60))) MatchParent();
					GUILayout.FlexibleSpace();
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.Space();


					if (!active)
						EditorGUI.EndDisabledGroup();
				}
					break;

				case AnchorMode.Point:

					DrawPointButtons();

					if (!active)
						EditorGUI.EndDisabledGroup();

					GUILayout.Space(-12); // move upwards a bit since there is empty space.
					parentPosition = EditorGUILayout.ToggleLeft("Use Parent Space", parentPosition);

					point = EditorGUILayout.Vector2Field("Snap Point", point);

					var btnText = string.Format(
						"Set Pivot to <color={0}>({1:f}, {2:f})</color>", HighlightColor, point.x, point.y);
					if (GUILayout.Button(btnText, setPivotStyle))
					{
						Undo.RecordObjects(objects.Select(o => o as Object).ToArray(), "set pivots");
						foreach (var obj in objects) obj.pivot = point;
					}

					break;
			}


			EditorGUILayout.Space();
		}

		[MenuItem("Tools/Better UI/Snap Anchors", false, 60)]
		public static void ShowWindow()
		{
			GetWindow(typeof(SnapAnchorsWindow), false, "Snap Anchors");
		}

		private void MatchParent()
		{
			Undo.RecordObjects(objects.ToArray(), "Match Parent" + DateTime.Now.ToFileTime());
			foreach (var obj in objects)
			{
				obj.anchorMin = Vector2.zero;
				obj.anchorMax = Vector2.one;
				obj.anchoredPosition = Vector2.zero;
				obj.sizeDelta = Vector2.zero;
			}
		}

		private void SetPoint(float x, float y)
		{
			point = new Vector2(x, y);
		}

		private Vector2 GetPivotOffset(RectTransform obj)
		{
			if (mode == AnchorMode.Border)
				return Vector2.zero;

			Vector2 result;

			if (parentPosition && mode == AnchorMode.Point)
			{
				var parentTransform = obj.parent as RectTransform;
				var parent = parentTransform != null ?
					parentTransform.ToScreenRect(true) :
					new Rect(0, 0, Screen.width, Screen.height);

				var rect = obj.ToScreenRect(true);
				var p = point;

				result = new Vector2(p.x * parent.width, p.y * parent.height);
				result += parent.position;
				result -= rect.position;
				result = new Vector2(result.x / rect.width, result.y / rect.height) - obj.pivot;
			}
			else
			{
				result = point - obj.pivot;
			}

			return result;
		}

		private void DrawPointButtons()
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			if (GUILayout.Button(
					new GUIContent(pointPic, "Snap all directions to position"), GUILayout.Width(120),
					GUILayout.Height(100)))
				SnapPoint(true, true);

			// TOP DOWN
			if (GUILayout.Button(
					new GUIContent(verticalPointPic, "Snap vertically to position"), GUILayout.Width(60),
					GUILayout.Height(100)))
				SnapPoint(false, true);

			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			// LEFT RIGHT
			if (GUILayout.Button(
					new GUIContent(horizontalPointPic, "Snap horizontally to position"), GUILayout.Width(120),
					GUILayout.Height(60)))
				SnapPoint(true, false);


			EditorGUILayout.BeginVertical();
			// const string style = "Label";
			var style = selectPointStyle;
			EditorGUILayout.BeginHorizontal();
			DrawSelectionPoint("┌", style, 0f, 1f);
			DrawSelectionPoint("┬", style, 0.5f, 1f);
			DrawSelectionPoint("┐", style, 1f, 1f);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			DrawSelectionPoint("├", style, 0f, 0.5f);
			DrawSelectionPoint("┼", style, 0.5f, 0.5f);
			DrawSelectionPoint("┤", style, 1f, 0.5f);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			DrawSelectionPoint("└", style, 0f, 0f);
			DrawSelectionPoint("┴", style, 0.5f, 0f);
			DrawSelectionPoint("┘", style, 1f, 0f);
			EditorGUILayout.EndHorizontal();

			if (objects.Count == 1)
			{
				var p = objects[0].pivot;
				var content = "[ Pivot ]";
				content = HighlightTextIfMatchCoordinate("[ Pivot ]", p.x, p.y);
				if (GUILayout.Button(content, style, GUILayout.Width(60), GUILayout.Height(16))) SetPoint(p.x, p.y);
			}
			else
			{
				GUILayout.Label("");
			}

			EditorGUILayout.EndVertical();

			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
		}

		private void DrawSelectionPoint(string content, GUIStyle style, float x, float y)
		{
			const float size = 20;
			content = HighlightTextIfMatchCoordinate(content, x, y);

			if (GUILayout.Button(content, style, GUILayout.Width(size), GUILayout.Height(size))) SetPoint(x, y);
		}

		private string HighlightTextIfMatchCoordinate(string content, float x, float y)
		{
			if (point.x == x && point.y == y)
				content = string.Format("<color={0}>{1}</color>", HighlightColor, content);

			return content;
		}

		private void DrawModeSelection()
		{
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Toggle(mode == AnchorMode.Border, "Border", EditorStyles.miniButtonLeft)
				&& mode != AnchorMode.Border)
				mode = AnchorMode.Border;

			if (GUILayout.Toggle(mode == AnchorMode.Point, "Point", EditorStyles.miniButtonRight)
				&& mode != AnchorMode.Point)
				mode = AnchorMode.Point;

			EditorGUILayout.EndHorizontal();
		}


		private void SnapBorder(bool left, bool right, bool top, bool bottom)
		{
			Undo.SetCurrentGroupName("Border" + DateTime.Now.ToFileTime());
			var group = Undo.GetCurrentGroup();

			foreach (var obj in objects) SnapBorder(obj, left, right, top, bottom);

			Undo.CollapseUndoOperations(group);
		}

		internal static void SnapBorder(RectTransform obj, bool left, bool right, bool top, bool bottom)
		{
			Undo.RecordObject(obj.transform, "Snap Anchors Border");

			var parentRotation = obj.parent.rotation;
			var objLocalRotation = obj.localRotation;
			var objLocalScale = obj.localScale;
			obj.parent.rotation = Quaternion.identity;
			obj.localRotation = Quaternion.identity;
			obj.localScale = Vector3.one;

			var parentTransform = obj.parent as RectTransform;
			var parent = parentTransform != null ?
				parentTransform.ToScreenRect() :
				new Rect(0, 0, Screen.width, Screen.height);

			var rect = obj.ToScreenRect();

			var sx = CalculateSize(obj.sizeDelta.x, left, right);
			var sy = CalculateSize(obj.sizeDelta.y, top, bottom);
			var x = CalculateAncherPos(
				obj.pivot.x, sx, left, right,
				obj.anchoredPosition.x); // (obj.sizeDelta.x * obj.pivot.x) - (sx * obj.sizeDelta.x);
			var y = CalculateAncherPos(
				obj.pivot.y, sy, top, bottom,
				obj.anchoredPosition.y); // (obj.sizeDelta.y * obj.pivot.y) - (sy * obj.sizeDelta.y);

			if (left || bottom)
			{
				var xMin = CalculateMinAnchor(
					left, rect.xMin, parent.xMin, parent.size.x,
					obj.anchorMin.x); //(left) ? (rect.xMin - parentRect.xMin) / parentRect.size.x : obj.anchorMin.x;
				var yMin = 1 - CalculateMaxAnchor(
					top, rect.yMax, parent.yMax, parent.size.y,
					1 - obj.anchorMin.y); // (top) ? rect.yMax / parent.size.y : obj.anchorMax.y;
				obj.anchorMin = new Vector2(xMin, yMin);
			}

			if (right || top)
			{
				var xMax = CalculateMaxAnchor(
					right, rect.xMax, parent.xMax, parent.size.x,
					obj.anchorMax.x); //(right) ? rect.xMax / parent.size.x : obj.anchorMax.x;
				var yMax = 1 - CalculateMinAnchor(
					bottom, rect.yMin, parent.yMin, parent.size.y,
					1 - obj.anchorMax.y); //(bottom) ? rect.yMin / parent.size.y : obj.anchorMin.y;
				obj.anchorMax = new Vector2(xMax, yMax);
			}

			obj.anchoredPosition = new Vector2(x, y);
			obj.sizeDelta = new Vector3(sx, sy);

			obj.parent.rotation = parentRotation;
			obj.localRotation = objLocalRotation;
			obj.localScale = objLocalScale;
		}

		private void SnapPoint(bool horizontal, bool vertical)
		{
			Undo.SetCurrentGroupName("Border" + DateTime.Now.ToFileTime());
			var group = Undo.GetCurrentGroup();

			foreach (var obj in objects)
			{
				var pivotOffset = GetPivotOffset(obj);
				SnapPoint(obj, pivotOffset, horizontal, vertical);
			}

			Undo.CollapseUndoOperations(group);
		}

		private void SnapPoint(RectTransform obj, Vector2 pivotOffset, bool horizontal, bool vertical)
		{
			Undo.RecordObject(obj.transform, "Snap Anchors Point");

			var pivot = obj.pivot + pivotOffset;

			var parentRotation = obj.parent.rotation;
			var objLocalRotation = obj.localRotation;
			var objLocalScale = obj.localScale;
			obj.parent.rotation = Quaternion.identity;
			obj.localRotation = Quaternion.identity;
			obj.localScale = Vector3.one;

			var parentTransform = obj.parent as RectTransform;
			var parent = parentTransform != null ?
				parentTransform.ToScreenRect(true) :
				new Rect(0, 0, Screen.width, Screen.height);

			var rect = obj.ToScreenRect(true);


			var pos = new Vector2(pivot.x * rect.width, pivot.y * rect.height);
			pos += rect.position;
			pos -= parent.position;
			pos.x /= parent.width;
			pos.y /= parent.height;

			var diff = obj.anchoredPosition
						+ new Vector2(pivotOffset.x * rect.width, pivotOffset.y * rect.height);

			if (horizontal && vertical)
			{
				obj.anchorMin = pos;
				obj.anchorMax = pos;
				obj.sizeDelta = rect.size;
				obj.anchoredPosition -= diff;
			}
			else if (horizontal)
			{
				obj.anchorMin = new Vector2(pos.x, obj.anchorMin.y);
				obj.anchorMax = new Vector2(pos.x, obj.anchorMax.y);
				obj.sizeDelta = new Vector2(rect.size.x, obj.sizeDelta.y);
				obj.anchoredPosition -= new Vector2(diff.x, 0);
			}
			else if (vertical)
			{
				obj.anchorMin = new Vector2(obj.anchorMin.x, pos.y);
				obj.anchorMax = new Vector2(obj.anchorMax.x, pos.y);
				obj.sizeDelta = new Vector2(obj.sizeDelta.x, rect.size.y);
				obj.anchoredPosition -= new Vector2(0, diff.y);
			}

			obj.parent.rotation = parentRotation;
			obj.localRotation = objLocalRotation;
			obj.localScale = objLocalScale;
		}

		private static float CalculateMinAnchor(
			bool calculate, float innerPos, float outerPos, float outerSize, float fallback)
		{
			return calculate ? (innerPos - outerPos) / outerSize : fallback;
		}

		private static float CalculateMaxAnchor(
			bool calculate, float innerPos, float outerPos, float outerSize, float fallback)
		{
			return calculate ? 1 - (outerPos - innerPos) / outerSize : fallback;
		}

		private static float CalculateSize(float size, bool front, bool back)
		{
			if (front && back) return 0;

			if (front || back) return 0.5f * size;

			return size;
		}

		private static float CalculateAncherPos(float pivot, float size, bool front, bool back, float fallback)
		{
			if (!front && !back)
				return fallback;

			if (size == 0)
				return 0;

			return 0.5f * size - pivot * size;
		}
	}
}
