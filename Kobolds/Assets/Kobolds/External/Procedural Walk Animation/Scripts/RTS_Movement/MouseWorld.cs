using UnityEngine;

namespace Lolopupka
{
	public class MouseWorld : MonoBehaviour
	{
		public static MouseWorld instance;

		[SerializeField] private LayerMask mousePlaneLayerMask;

		private void Awake()
		{
			instance = this;
		}

		private void Update()
		{
		}

		public static Vector3 GetPosition()
		{
			var ray = Camera.main.ScreenPointToRay(InputManager.Instance.GetMouseScreenPostion());
			Physics.Raycast(ray, out var raycastHit, float.MaxValue, instance.mousePlaneLayerMask);
			return raycastHit.point;
		}
	}
}
