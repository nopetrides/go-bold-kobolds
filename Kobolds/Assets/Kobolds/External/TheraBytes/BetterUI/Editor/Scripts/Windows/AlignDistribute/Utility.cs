using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TheraBytes.BetterUi.Editor.AlignDistribute
{
	public static class Utility
	{
		internal static GameObject helperObject;

		internal static void CleanUp()
		{
			if (helperObject != null) GameObject.DestroyImmediate(helperObject);
		}

		public static Transform[] SortHierarchically(Transform[] input)
		{
			var parent = input[0].parent;
			var result = new Transform[input.Length];
			var currentIndex = 0;

			for (var i = 0; i < parent.childCount && currentIndex < input.Length; i++)
				foreach (var transform in input)
				{
					if (parent.GetChild(i) != transform) continue;

					result[currentIndex] = transform;
					currentIndex++;
					break;
				}

			return result;
		}

		public static Transform[] SortByArea(Transform[] input)
		{
			Array.Sort(input, new AreaComparer());
			return input;
		}

		public static Transform[] SortByWidth(Transform[] input)
		{
			Array.Sort(input, new WidthComparer());
			return input;
		}

		public static Transform[] SortByHeight(Transform[] input)
		{
			Array.Sort(input, new HeightComparer());
			return input;
		}

		public static Transform[] SortByPositionX(Transform[] input)
		{
			Array.Sort(input, new PositionComparerX());
			return input;
		}

		public static Transform[] SortByPositionY(Transform[] input)
		{
			Array.Sort(input, new PositionComparerY());
			return input;
		}

		public static Vector2 GetTransformSize(Transform rectTransform)
		{
			return GetTransformSize(rectTransform as RectTransform);
		}

		public static Vector2 GetTransformSize(RectTransform rectTransform)
		{
			var result = new Vector2();
			result.x = Mathf.Abs(rectTransform.rect.width * rectTransform.lossyScale.x);
			result.y = Mathf.Abs(rectTransform.rect.height * rectTransform.lossyScale.y);
			return result;
		}

		public static Vector2 GetLocalPivotPosition(RectTransform rectTransform)
		{
			var result = GetTransformSize(rectTransform);
			result.x *= rectTransform.pivot.x;
			result.y *= rectTransform.pivot.y;

			return result;
		}

		public static Vector2 GetPivotAndCenterLocalDistance(RectTransform rectTransform)
		{
			var size = GetTransformSize(rectTransform);

			var y = size.y * (rectTransform.pivot.y - 0.5f);
			var x = size.x * (rectTransform.pivot.x - 0.5f);

			return new Vector2(x, y);
		}

		public static SelectionStatus IsSelectionValid()
		{
			var transforms = Selection.GetTransforms(SelectionMode.Unfiltered);

			if (transforms == null || transforms.Length < 1) return SelectionStatus.NothingSelected;

			var sharedParent = transforms[0].parent;

			if (sharedParent == null) return SelectionStatus.ParentIsNull;

			if (sharedParent.GetComponent(typeof(RectTransform)) == null)
				return SelectionStatus.ParentIsNoRectTransform;

			for (var i = 1; i < transforms.Length; i++)
			{
				if (transforms[i].GetComponent(typeof(RectTransform)) == null)
					return SelectionStatus.ContainsNoRectTransform;

				if (sharedParent != transforms[i].parent) return SelectionStatus.UnequalParents;
			}

			return SelectionStatus.Valid;
		}

		public static void AdjustAnchors(RectTransform rectTransform, Vector2 oldPosition)
		{
			switch (AlignDistributeWindow.anchorMode)
			{
				case AnchorMode.StayAtCurrentPosition:
					return;

				case AnchorMode.SnapToBorder:
					SnapAnchorsWindow.SnapBorder(rectTransform, true, true, true, true);
					return;

				case AnchorMode.FollowObject:
					FollowAnchor(rectTransform, oldPosition);
					return;

				default:
					Debug.LogError("Unknown AnchorMode: " + AlignDistributeWindow.anchorMode);
					throw new ArgumentOutOfRangeException();
			}
		}

		private static void FollowAnchor(RectTransform rectTransform, Vector2 oldPosition)
		{
			var currentPosition = rectTransform.position;
			var halfSize = GetTransformSize(rectTransform) * 0.5f;
			var parentSize = GetTransformSize(rectTransform.parent);
			var pivotAndCenterDistance = GetPivotAndCenterLocalDistance(rectTransform);

			var max = (Vector2) rectTransform.position + halfSize - pivotAndCenterDistance;
			var min = (Vector2) rectTransform.position - halfSize - pivotAndCenterDistance;

			var oldMax = oldPosition + halfSize - pivotAndCenterDistance;
			var oldMin = oldPosition - halfSize - pivotAndCenterDistance;

			var diffMax = oldMax - max;
			var diffMin = oldMin - min;

			// Normalize
			diffMin.x /= parentSize.x;
			diffMin.y /= parentSize.y;

			diffMax.x /= parentSize.x;
			diffMax.y /= parentSize.y;

			// Apply Values
			rectTransform.anchorMax = rectTransform.anchorMax - diffMax;
			rectTransform.anchorMin = rectTransform.anchorMin - diffMin;

			rectTransform.position = currentPosition;
		}

		public static RectTransform GetBoundingBoxRectTransform(Transform[] selection)
		{
			// Instantiating a RectTransform doesn't work, therefore we need a temporal GameObject...
			helperObject = new GameObject("Bounding Box Rect", typeof(RectTransform));
			helperObject.transform.SetParent(selection[0].parent);

			var result = helperObject.GetComponent<RectTransform>();
			var min = new Vector2(Mathf.Infinity, Mathf.Infinity);
			var max = new Vector2(Mathf.NegativeInfinity, Mathf.NegativeInfinity);

			foreach (var transform in selection)
			{
				var rectTransform = transform as RectTransform;

				var size = GetTransformSize(rectTransform);

				var upperRight = (Vector2) rectTransform.position + size * 0.5f -
								GetPivotAndCenterLocalDistance(rectTransform);
				var lowerLeft = (Vector2) rectTransform.position - size * 0.5f -
								GetPivotAndCenterLocalDistance(rectTransform);

				min.x = Mathf.Min(min.x, lowerLeft.x);
				min.y = Mathf.Min(min.y, lowerLeft.y);

				max.x = Mathf.Max(max.x, upperRight.x);
				max.y = Mathf.Max(max.y, upperRight.y);
			}

			result.position = new Vector3(min.x + max.x, min.y + max.y) * 0.5f;
			result.sizeDelta = new Vector2(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y));

			return result;
		}

		// Sort from smallest to largest
		private class AreaComparer : IComparer<Transform>
		{
			public int Compare(Transform a, Transform b)
			{
				return GetTransformArea(a).CompareTo(GetTransformArea(b));
			}

			private float GetTransformArea(Transform transform)
			{
				var size = GetTransformSize(transform);
				return size.x * size.y;
			}
		}

		private class WidthComparer : IComparer<Transform>
		{
			public int Compare(Transform a, Transform b)
			{
				var sizeA = GetTransformSize(a);
				var sizeB = GetTransformSize(b);
				return sizeA.x.CompareTo(sizeB.x);
			}
		}

		private class HeightComparer : IComparer<Transform>
		{
			public int Compare(Transform a, Transform b)
			{
				var sizeA = GetTransformSize(a);
				var sizeB = GetTransformSize(b);
				return sizeA.y.CompareTo(sizeB.y);
			}
		}

		private class PositionComparerX : IComparer<Transform>
		{
			public int Compare(Transform a, Transform b)
			{
				return a.position.x.CompareTo(b.position.x);
			}
		}

		private class PositionComparerY : IComparer<Transform>
		{
			public int Compare(Transform a, Transform b)
			{
				return a.position.y.CompareTo(b.position.y);
			}
		}
	}
}
