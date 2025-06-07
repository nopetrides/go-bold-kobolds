using UnityEngine;

namespace FIMSpace.Basics
{
	public class FBasic_OffsetMovement : MonoBehaviour
	{
		public Vector3 positionRange = new(1f, 0f, 0f);
		public Vector3 rotRange = Vector3.zero;
		public float speed = 2f;

		public bool fixedUpdate;
		private Vector3 startPos;
		private Quaternion startRot;

		private void Start()
		{
			startPos = transform.position;
			startRot = transform.rotation;
		}

		private void Update()
		{
			if (fixedUpdate == false)
			{
				transform.position = startPos + positionRange * Mathf.Sin(Time.time * speed);
				transform.rotation = startRot * Quaternion.Euler(rotRange * Mathf.Sin(Time.time * speed));
			}
		}

		private void FixedUpdate()
		{
			if (fixedUpdate)
			{
				var rig = GetComponent<Rigidbody>();
				if (rig)
				{
					rig.MovePosition(startPos + positionRange * Mathf.Sin(Time.time * speed));
					rig.MoveRotation(startRot * Quaternion.Euler(rotRange * Mathf.Sin(Time.time * speed)));
				}
				else
				{
					transform.position = startPos + positionRange * Mathf.Sin(Time.time * speed);
					transform.rotation = startRot * Quaternion.Euler(rotRange * Mathf.Sin(Time.time * speed));
				}
			}
		}
	}
}
