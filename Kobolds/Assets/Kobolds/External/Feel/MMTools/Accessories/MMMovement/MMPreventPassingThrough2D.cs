using UnityEngine;
using UnityEngine.Serialization;

namespace MoreMountains.Tools
{
	/// <summary>
	///     Prevents fast moving objects from going through colliders by casting a ray backwards after each movement
	/// </summary>
	[AddComponentMenu("More Mountains/Tools/Movement/MM Prevent Passing Through 2D")]
	public class MMPreventPassingThrough2D : MonoBehaviour
	{
		public enum Modes
		{
			Raycast,
			BoxCast
		}

		/// whether to cast a ray or a boxcast to look for targets
		public Modes Mode = Modes.Raycast;

		/// the layer mask to search obstacles on
		public LayerMask ObstaclesLayerMask;

		/// the bounds adjustment variable
		public float SkinWidth = 0.1f;

		/// whether or not to reposition the rb if hitting a trigger collider
		public bool RepositionRigidbodyIfHitTrigger = true;

		/// whether or not to reposition the rb if hitting a non trigger collider
		[FormerlySerializedAs("RepositionRigidbody")]
		public bool RepositionRigidbodyIfHitNonTrigger = true;

		protected float _adjustedSmallestBoundsWidth;
		protected Collider2D _collider;
		protected Vector2 _colliderSize;
		protected RaycastHit2D _hitInfo;
		protected Vector2 _lastMovement;
		protected float _lastMovementSquared;
		protected Vector3 _positionLastFrame;
		protected Rigidbody2D _rigidbody;

		protected float _smallestBoundsWidth;
		protected float _squaredBoundsWidth;

		[Header("Debug")]
		[MMReadOnly]
		public RaycastHit2D Hit;

		/// <summary>
		///     On Start we initialize our object
		/// </summary>
		protected virtual void Start()
		{
			Initialization();
		}

		/// <summary>
		///     On fixedUpdate, checks the last movement and if needed casts a ray to detect obstacles
		/// </summary>
		protected virtual void Update()
		{
			_lastMovement = transform.position - _positionLastFrame;
			_lastMovementSquared = _lastMovement.sqrMagnitude;

			// if we've moved further than our bounds, we may have missed something
			if (_lastMovementSquared > _squaredBoundsWidth)
			{
				var movementMagnitude = Mathf.Sqrt(_lastMovementSquared);

				// we cast a ray backwards to see if we should have hit something
				if (Mode == Modes.Raycast)
					_hitInfo = MMDebug.RayCast(
						_positionLastFrame, _lastMovement.normalized, movementMagnitude, ObstaclesLayerMask, Color.blue,
						true);
				else
					_hitInfo = Physics2D.BoxCast(
						_positionLastFrame,
						_colliderSize,
						0,
						layerMask: ObstaclesLayerMask,
						direction: _lastMovement.normalized,
						distance: movementMagnitude);

				if (_hitInfo.collider != null)
				{
					if (_hitInfo.collider.isTrigger)
					{
						_hitInfo.collider.SendMessage(
							"OnTriggerEnter2D", _collider, SendMessageOptions.DontRequireReceiver);
						if (RepositionRigidbodyIfHitTrigger)
						{
							transform.position = _hitInfo.point -
												_lastMovement / movementMagnitude * _adjustedSmallestBoundsWidth;
							_rigidbody.position = _hitInfo.point -
												_lastMovement / movementMagnitude * _adjustedSmallestBoundsWidth;
						}
					}

					if (!_hitInfo.collider.isTrigger)
					{
						Hit = _hitInfo;
						gameObject.SendMessage("PreventedCollision2D", Hit, SendMessageOptions.DontRequireReceiver);
						if (RepositionRigidbodyIfHitNonTrigger)
						{
							transform.position = _hitInfo.point -
												_lastMovement / movementMagnitude * _adjustedSmallestBoundsWidth;
							_rigidbody.position = _hitInfo.point -
												_lastMovement / movementMagnitude * _adjustedSmallestBoundsWidth;
						}
					}
				}
			}

			_positionLastFrame = transform.position;
		}

		/// <summary>
		///     On Enable, we initialize our last frame position
		/// </summary>
		protected virtual void OnEnable()
		{
			_positionLastFrame = transform.position;
		}

		/// <summary>
		///     Grabs the rigidbody and computes the bounds width
		/// </summary>
		protected virtual void Initialization()
		{
			_rigidbody = GetComponent<Rigidbody2D>();
			_positionLastFrame = _rigidbody.position;

			_collider = GetComponent<Collider2D>();
			if (_collider as BoxCollider2D != null) _colliderSize = (_collider as BoxCollider2D).size;

			_smallestBoundsWidth = Mathf.Min(
				Mathf.Min(_collider.bounds.extents.x, _collider.bounds.extents.y), _collider.bounds.extents.z);
			_adjustedSmallestBoundsWidth = _smallestBoundsWidth * (1.0f - SkinWidth);
			_squaredBoundsWidth = _smallestBoundsWidth * _smallestBoundsWidth;
		}
	}
}
