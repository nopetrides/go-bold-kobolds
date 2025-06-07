using UnityEngine;

namespace FIMSpace.Basics
{
	/// <summary>
	///     FM: Example of using FBasic_Pullable to create hinge pullable object
	/// </summary>
	public class FBasic_PullableHinge : FBasic_Pullable
	{
		public Renderer ToEmmit;

		public Vector2 RotationRanges = new(0f, 90f);
		public Vector3 RotationAxis = new(0f, 1f, 0f);
		public bool ReversePull;

		[Range(0.5f, 10f)]
		public float Deceleration = 3f;
		//private Quaternion closeRotation;

		//private Vector3 initForward;

		private bool animationFinished;

		protected Quaternion initialRotation;
		private float lookDot;

		private Quaternion offsetRotation;
		private float rotationIncreaser = 1f;
		private float startSensitivity;
		private float velocity;
		public float PullValue { get; protected set; }


		protected override void Start()
		{
			initialRotation = transform.localRotation;

			base.Start();

			//closeRotation = Quaternion.Euler(RotationAxis.x * RotationRanges.x, RotationAxis.y * RotationRanges.x, RotationAxis.z * RotationRanges.x);
			//initForward = closeRotation * Vector3.forward;

			startSensitivity = Sensitivity;

			PullValue = StartValueY;

			UpdatePullableOrientation();
			transform.localRotation = initialRotation * offsetRotation;

			velocity = 0f;
		}


		private void OnDrawGizmosSelected()
		{
			if (!Application.isPlaying)
			{
				var size = new Vector3(1.5f, 0.2f, 0.2f);
				Gizmos.color = new Color(0.4f, 0.4f, 1f, 0.95f);
				Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
				Gizmos.DrawWireCube(Vector3.right * size.x / 2, size);

				Gizmos.color = new Color(1f, 0.4f, 0.4f, 0.95f);
				var eulLimB = new Vector3(
					RotationAxis.x * RotationRanges.y, RotationAxis.y * RotationRanges.y,
					RotationAxis.z * RotationRanges.y);
				var limitAngleB = Quaternion.Euler(eulLimB);
				Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation * limitAngleB, Vector3.one);
				Gizmos.DrawWireCube(Vector3.right * size.x / 2, size);

				Gizmos.color = new Color(1f, 0.4f, 0.4f, 0.95f);
				size = new Vector3(1.3f, 0.15f, 0.15f);
				var eulLimA = new Vector3(
					RotationAxis.x * RotationRanges.x, RotationAxis.y * RotationRanges.x,
					RotationAxis.z * RotationRanges.x);
				var limitAngleA = Quaternion.Euler(eulLimA);
				Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation * limitAngleA, Vector3.one);
				Gizmos.DrawWireCube(Vector3.right * size.x / 2, size);

				Gizmos.color = new Color(0.6f, 1f, 0.6f, 0.8f);
				var startRotation = Quaternion.Euler(Vector3.Lerp(eulLimA, eulLimB, StartValueY));
				Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation * startRotation, Vector3.one);
				size = new Vector3(1.2f, 0.1f, 0.1f);
				Gizmos.DrawWireCube(Vector3.right * size.x / 2, size);
			}
		}


		protected override void UpdatePullableOrientation()
		{
			base.UpdatePullableOrientation();
			var range = Mathf.Lerp(RotationRanges.x, RotationRanges.y, PullValue);
			offsetRotation = Quaternion.Euler(range * RotationAxis.x, range * RotationAxis.y, range * RotationAxis.z);
		}

		protected override void UpdateIn()
		{
			base.UpdateIn();

			transform.localRotation = Quaternion.Slerp(
				transform.localRotation, initialRotation * offsetRotation, Time.deltaTime * 9f * rotationIncreaser);

			var angleDiff = Quaternion.Angle(transform.localRotation, offsetRotation);

			if (Holding)
			{
				var forwardA = transform.localRotation * Vector3.forward;
				var forwardB = initialRotation * offsetRotation * Vector3.forward;
				var angleA = Mathf.Atan2(forwardA.x, forwardA.z) * Mathf.Rad2Deg;
				var angleB = Mathf.Atan2(forwardB.x, forwardB.z) * Mathf.Rad2Deg;
				var diff = -Mathf.DeltaAngle(angleA, angleB);
				if (ReversePull) diff = -diff;

				if (YValue < 0f) YValue = 0f;
				if (YValue > 100f) YValue = 100f;

				velocity = Mathf.Lerp(velocity, diff, Time.deltaTime * 9f);
			}
			else
			{
				velocity = Mathf.Lerp(velocity, 0f, Time.deltaTime * Deceleration);
				//YValue += velocity * 0.5f;
				//PullValue += (velocity / 100f) * 0.5f;
				var yAdd = velocity * 2.5f; // * Mathf.Abs(RotationRanges.x - RotationRanges.y)
				YValue += yAdd * Time.deltaTime * 12f;

				if (YValue < 0f) YValue = 0f;
				if (YValue > 100f) YValue = 100f;

				PullValue = YValue / 100f;
				//PullValue += yAdd / 100f;

				var range = Mathf.Lerp(RotationRanges.x, RotationRanges.y, PullValue);
				offsetRotation = Quaternion.Euler(
					range * RotationAxis.x, range * RotationAxis.y, range * RotationAxis.z);
			}

			rotationIncreaser = Mathf.Lerp(
				rotationIncreaser, Mathf.Lerp(2f, 4f, Mathf.InverseLerp(5f, 45f, angleDiff)), Time.deltaTime * 10f);

			if (angleDiff < 0.01f && velocity < 0.01f) animationFinished = true;
			else animationFinished = false;

			if (Holding)
			{
				canvasGroup.alpha = 0f;
				Sensitivity = startSensitivity;
				if (ReversePull) Sensitivity *= -1f;

				// Checking if pull should behave in reverse way
				if (EnteredTransform)
					if (lookDot < 0f)
						Sensitivity *= -1f;

				PullValue = Mathf.Clamp(YValue, 0f, 100f);
				PullValue /= 100f;

				UpdatePullableOrientation();
			}
			else
			{
				if (animationFinished) conditionalExit = false;
			}

			if (ToEmmit)
			{
				if (mouseEntered)
					ToEmmit.material.LerpMaterialColor("_EmissionColor", Color.white * 0.3f);
				else
					ToEmmit.material.LerpMaterialColor("_EmissionColor", Color.black);
			}
		}


		protected override void StartHolding()
		{
			lookDot = Vector3.Dot(Camera.main.transform.forward, transform.forward);
			conditionalExit = true;
			base.StartHolding();
		}
	}
}
