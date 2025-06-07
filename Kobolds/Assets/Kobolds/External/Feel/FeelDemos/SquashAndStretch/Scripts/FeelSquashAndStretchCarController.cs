using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.Feel
{
	[AddComponentMenu("")]
	public class FeelSquashAndStretchCarController : MonoBehaviour
	{
		protected const string _horizontalAxis = "Horizontal";
		protected const string _verticalAxis = "Vertical";

		[Header("Car Settings")]
		public float Speed = 2f;

		public float RotationSpeed = 2f;

		[Header("Bindings")]
		public Collider BoundaryCollider;

		public List<TrailRenderer> Trails;
		public MMFeedbacks TeleportFeedbacks;
		protected Bounds _bounds;

		protected Vector2 _input;
		protected Vector3 _newPosition;
		protected Vector3 _rotationAxis = Vector3.up;
		protected Vector3 _thisPosition;
		protected float _trailTime;

		protected virtual void Start()
		{
			_bounds = BoundaryCollider.bounds;
			TeleportFeedbacks?.Initialization();
			_trailTime = Trails[0].time;
		}

		protected virtual void Update()
		{
			HandleInput();
			MoveCar();
			HandleBounds();
		}

		protected virtual void HandleInput()
		{
			_input = FeelDemosInputHelper.GetDirectionAxis(ref _input);
		}

		protected virtual void MoveCar()
		{
			transform.Rotate(_rotationAxis, _input.x * Time.deltaTime * RotationSpeed);
			transform.Translate(transform.forward * _input.y * Speed * Time.deltaTime, Space.World);
		}

		protected virtual void HandleBounds()
		{
			_newPosition = _thisPosition = transform.position;

			if (_thisPosition.x < _bounds.min.x)
				_newPosition.x = _bounds.max.x;
			else if (_thisPosition.x > _bounds.max.x) _newPosition.x = _bounds.min.x;

			if (_thisPosition.z < _bounds.min.z)
				_newPosition.z = _bounds.max.z;
			else if (_thisPosition.z > _bounds.max.z) _newPosition.z = _bounds.min.z;

			if (_newPosition != _thisPosition) StartCoroutine(TeleportSequence());
		}

		protected virtual IEnumerator TeleportSequence()
		{
			TeleportFeedbacks?.PlayFeedbacks();
			SetTrails(false);
			yield return MMCoroutine.WaitForFrames(1);
			transform.position = _newPosition;
			TeleportFeedbacks?.PlayFeedbacks();
			SetTrails(true);
		}

		protected virtual void SetTrails(bool status)
		{
			foreach (var trail in Trails) trail.Clear();
		}
	}
}
