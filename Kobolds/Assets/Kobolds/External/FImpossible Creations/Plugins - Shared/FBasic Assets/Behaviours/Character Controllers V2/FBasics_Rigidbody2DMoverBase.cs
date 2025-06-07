using UnityEngine;

namespace FIMSpace.Basics
{
	public abstract class FBasics_Rigidbody2DMoverBase : MonoBehaviour
	{
		/// <summary> Base reference to collider component </summary>
		protected Collider2D charCollider;

		protected bool isGrounded;
		protected float moveSpeed = 5f;

		/// <summary> Base reference to rigidbody component </summary>
		protected Rigidbody2D rigbody;

		protected float rotateSpeed = 10f;

		protected Vector3 smoothedAcceleration;
		protected Quaternion smoothedRotation;

		/// <summary> Jump power to be executed in next fixed update frame </summary>
		protected float triggerJumping;

		/// <summary> Variable for helping smooth damp </summary>
		protected Vector3 veloHelper = Vector3.zero;


		//public float MovementSpeed = 4;
		//[Range(0f,1f)]
		//public float SmoothRotation = 0f;
		//public float JumpPower = 12f;

		//public bool DoubleJump = true;

		//[Tooltip("Use keyboard keys movement implementation for quick debugging?")]
		//public bool WSADMovement = true;

		//[Range(0f, 0.5f)]
		//[Tooltip("How slow accelerate/decelerate should be")]
		//public float accelerationTime = 0.1f;

		//protected float moveSpeed = 5f;
		//protected float rotateSpeed = 10f;

		//protected Vector3 smoothedAcceleration;

		//private Vector3 moveDir = Vector3.zero;
		//protected Quaternion targetRot;
		//protected Vector3 veloHelper = Vector3.zero;
		//protected bool isGrounded = false;

		//protected float triggerJumping = 0f;
		//private int jumps = 0;


		protected virtual void Start()
		{
			rigbody = GetComponentInChildren<Rigidbody2D>();
			charCollider = GetComponentInChildren<Collider2D>();

			// Making character slide on colliders when jumping forward vertical walls etc.
			if (charCollider)
				charCollider.sharedMaterial = FEngineering.PMFrict2D;

			rigbody.interpolation =
				RigidbodyInterpolation2D
					.Interpolate; // Interpolation for smooth motion with rig.velocity and rig.angularVelocity changes

			smoothedAcceleration = Vector3.zero;
			smoothedRotation = transform.rotation;
		}


		protected virtual void FixedUpdate()
		{
			Vector3 velocityMemory = rigbody.linearVelocity;

			// Supporting model scale and moving in forward direction with computed acceleration
			var targetVelo = transform.TransformVector(smoothedAcceleration) * moveSpeed;

			if (triggerJumping != 0f)
			{
				targetVelo.y = triggerJumping;
				rigbody.MovePosition(rigbody.position + Vector2.up * triggerJumping * 0.05f);
				OnJump();
				triggerJumping = 0f;
				isGrounded = false;
			}
			else
			{
				targetVelo.y = velocityMemory.y; // Not changing velocity in y only accelerate in x and z
			}


			// Applying sliding material to mover when there is desired movement
			if (!isGrounded || targetVelo.sqrMagnitude > moveSpeed * 0.2f)
				charCollider.sharedMaterial = FEngineering.PMSliding2D;
			else // When stopped we preventing from sliding on walls
				charCollider.sharedMaterial = FEngineering.PMFrict2D;


			rigbody.linearVelocity = targetVelo;
		}


		/// <summary>
		///     Checking grounded state
		/// </summary>
		protected virtual void OnCollisionEnter2D(Collision2D collision)
		{
			if (!isGrounded)
				if (collision != null)
					if (collision.contacts.Length > 0)
						for (var i = 0; i < collision.contacts.Length; i++)
						{
							var dot = Vector2.Dot(rigbody.transform.up, collision.contacts[i].normal);

							// Dot == 1 -> Standing on flat surface
							// Dot == 0 -> pushing against vertical wall
							// Dot == 0.5 -> standing on 45 degrees wall
							if (dot > 0.25f) // Standing max on 22,5 degrees wall
							{
								OnGrounded();
								return;
							}
						}
		}


		protected virtual void UpdateMotor()
		{
			// Example to be overrided ----------------------------------

			var targetRot = transform.rotation;

			// Move left / right
			if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
				targetRot = Quaternion.Euler(0f, 180f, 0f);
			else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
				targetRot = Quaternion.Euler(0f, 0f, 0f);

			if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Space))
				triggerJumping = 8;

			transform.rotation = targetRot;
		}


		/// <summary>
		///     Executed when character triggers jumping
		/// </summary>
		protected virtual void OnJump()
		{
		}


		/// <summary>
		///     To be overrided for particel effects / animations
		/// </summary>
		protected virtual void OnGrounded()
		{
			isGrounded = true;
		}
	}
}
