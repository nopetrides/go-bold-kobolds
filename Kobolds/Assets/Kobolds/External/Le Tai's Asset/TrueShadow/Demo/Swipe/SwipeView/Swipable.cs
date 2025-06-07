using System;
using System.Collections;
using UnityEngine;

namespace LeTai.SwipeView
{
	public abstract class Swipable<TData> : MonoBehaviour
	{
		private Vector3 currentPosVel;
		private Vector3 currentScaleVel;
		protected bool isSnaping;

		protected bool isSwiping;

		private Coroutine runningSnap;
		internal Vector2 snapPosition;
		internal Vector3 snapScale;

		internal SwipeView<TData> view;

		public TData Data { get; protected set; }

		internal RectTransform RectTransform { get; private set; }

		private void Awake()
		{
			RectTransform = GetComponent<RectTransform>();
		}

		public event Action<Vector2> removed;

		protected internal void StartSnap(bool snapOut = false)
		{
			if (isSnaping && runningSnap != null) StopCoroutine(runningSnap);

			isSnaping = true;
			runningSnap = StartCoroutine(Snap(snapOut));
		}

		private void DoTilt()
		{
			RectTransform.localRotation = Quaternion.LookRotation(
				RectTransform.forward,
				RectTransform.localPosition -
				view.rotationPivot.WithZ(0) *
				view.canvas.scaleFactor);
		}

		protected IEnumerator Snap(bool snapOut)
		{
			var targetPosition =
				snapOut ? RectTransform.localPosition.normalized * view.throwDistance : (Vector3) snapPosition;

			if (snapOut) OnRemoved(RectTransform.localPosition);

			currentScaleVel = Vector3.zero;
			currentPosVel = Vector3.zero;
			while (!isSwiping && (RectTransform.localPosition - targetPosition).sqrMagnitude > 1e-4f)
			{
				RectTransform.localPosition = Vector3.SmoothDamp(
					RectTransform.localPosition,
					targetPosition,
					ref currentPosVel,
					view.animationSmoothTime);
				RectTransform.localScale = Vector3.SmoothDamp(
					RectTransform.localScale,
					snapScale,
					ref currentScaleVel,
					view.animationSmoothTime);

				DoTilt();

				yield return null;
			}

			isSnaping = false;

			if (snapOut) Destroy(gameObject);
		}


		public void Swipe(Vector2 offset)
		{
			isSwiping = true;
			RectTransform.localPosition = snapPosition + offset;
			DoTilt();
		}

		public abstract void SetData(TData data);

		internal void EndSwipe(Vector2 offset)
		{
			isSwiping = false;

			var willRemove = offset.magnitude > view.distanceToRemove;

			StartSnap(willRemove);
		}

		protected virtual void OnRemoved(Vector2 offset)
		{
			removed?.Invoke(offset);
		}
	}
}
