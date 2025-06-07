using UnityEngine;

namespace Lolopupka
{
	public class GroundAlliment : MonoBehaviour
	{
		[SerializeField] private float checkRange = 1f;
		[SerializeField] private float orientationSpeed = 50f;
		[SerializeField] private LayerMask layerMask;

		private Vector3 lastUp;
		private float t;

		private void Update()
		{
			t += Time.deltaTime;

			RaycastHit hit;
			Physics.Raycast(transform.position + Vector3.up * 1f, -Vector3.up, out hit, checkRange, layerMask);

			var newUp = Vector3.Lerp(lastUp, hit.normal, t * orientationSpeed);
			var targetRotation = GetTargetRotation(transform.forward, newUp);

			transform.rotation = Quaternion.RotateTowards(
				transform.rotation, targetRotation, orientationSpeed * Time.deltaTime);

			lastUp = transform.up;
		}

		private Quaternion GetTargetRotation(Vector3 approximateForward, Vector3 exactUp)
		{
			var zToUp = Quaternion.LookRotation(exactUp, -approximateForward);
			var yToz = Quaternion.Euler(90, 0, 0);
			return zToUp * yToz;
		}
	}
}
