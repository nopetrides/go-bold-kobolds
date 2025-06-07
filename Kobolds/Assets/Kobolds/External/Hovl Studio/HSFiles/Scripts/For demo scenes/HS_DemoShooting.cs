using UnityEngine;

public class HS_DemoShooting : MonoBehaviour
{
	[Range(0.0f, 1.0f)]
	public float fireRate = 0.1f;

	public GameObject FirePoint;
	public Camera Cam;

	//How far you can point raycast for projectiles
	public float MaxLength;
	public GameObject[] Prefabs;

	//For Camera shake 
	public Animation camAnim;

	//Double-click protection
	private float buttonSaver;
	private Vector3 direction;
	private float fireCountdown;

	[Header("Fire rate")]
	private int Prefab;

	private Ray RayMouse;
	private Quaternion rotation;

	private void Start()
	{
		Counter(0);
	}

	private void Update()
	{
		//Single shoot
		if (Input.GetButtonDown("Fire1"))
		{
			camAnim.Play(camAnim.clip.name);
			Instantiate(Prefabs[Prefab], FirePoint.transform.position, FirePoint.transform.rotation);
		}

		//Fast shooting
		if (Input.GetMouseButton(1) && fireCountdown <= 0f)
		{
			Instantiate(Prefabs[Prefab], FirePoint.transform.position, FirePoint.transform.rotation);
			fireCountdown = 0;
			fireCountdown += fireRate;
		}

		fireCountdown -= Time.deltaTime;

		//To change projectiles
		if ((Input.GetKey(KeyCode.A) || Input.GetAxis("Horizontal") < 0) && buttonSaver >= 0.4f) // left button
		{
			buttonSaver = 0f;
			Counter(-1);
		}

		if ((Input.GetKey(KeyCode.D) || Input.GetAxis("Horizontal") > 0) && buttonSaver >= 0.4f) // right button
		{
			buttonSaver = 0f;
			Counter(+1);
		}

		buttonSaver += Time.deltaTime;

		//To rotate fire point
		if (Cam != null)
		{
			RaycastHit hit;
			var mousePos = Input.mousePosition;
			RayMouse = Cam.ScreenPointToRay(mousePos);
			if (Physics.Raycast(RayMouse.origin, RayMouse.direction, out hit, MaxLength))
				RotateToMouseDirection(gameObject, hit.point);
		}
		else
		{
			Debug.Log("No camera");
		}
	}

	// To change prefabs (count - prefab number)
	private void Counter(int count)
	{
		Prefab += count;
		if (Prefab > Prefabs.Length - 1)
			Prefab = 0;
		else if (Prefab < 0) Prefab = Prefabs.Length - 1;
	}

	//To rotate fire point
	private void RotateToMouseDirection(GameObject obj, Vector3 destination)
	{
		direction = destination - obj.transform.position;
		rotation = Quaternion.LookRotation(direction);
		obj.transform.localRotation = Quaternion.Lerp(obj.transform.rotation, rotation, 1);
	}
}
