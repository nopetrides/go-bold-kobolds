using System.Collections;
using UnityEngine;

public class HS_ProjectileMover : MonoBehaviour
{
	[SerializeField] protected float speed = 15f;
	[SerializeField] protected float hitOffset;
	[SerializeField] protected bool UseFirePointRotation;
	[SerializeField] protected Vector3 rotationOffset = new(0, 0, 0);
	[SerializeField] protected GameObject hit;
	[SerializeField] protected ParticleSystem hitPS;
	[SerializeField] protected GameObject flash;
	[SerializeField] protected Rigidbody rb;
	[SerializeField] protected Collider col;
	[SerializeField] protected Light lightSourse;
	[SerializeField] protected GameObject[] Detached;
	[SerializeField] protected ParticleSystem projectilePS;
	[SerializeField] protected bool notDestroy;
	private bool startChecker;

	protected virtual void Start()
	{
		if (!startChecker)
			/*lightSourse = GetComponent<Light>();
			rb = GetComponent<Rigidbody>();
			col = GetComponent<Collider>();
			if (hit != null)
				hitPS = hit.GetComponent<ParticleSystem>();*/
			if (flash != null)
				flash.transform.parent = null;

		if (notDestroy)
			StartCoroutine(DisableTimer(5));
		else
			Destroy(gameObject, 5);
		startChecker = true;
	}

	protected virtual void FixedUpdate()
	{
		if (speed != 0) rb.linearVelocity = transform.forward * speed;
	}

	protected virtual void OnEnable()
	{
		if (startChecker)
		{
			if (flash != null) flash.transform.parent = null;
			if (lightSourse != null)
				lightSourse.enabled = true;
			col.enabled = true;
			rb.constraints = RigidbodyConstraints.None;
		}
	}

	//https ://docs.unity3d.com/ScriptReference/Rigidbody.OnCollisionEnter.html
	protected virtual void OnCollisionEnter(Collision collision)
	{
		//Lock all axes movement and rotation
		rb.constraints = RigidbodyConstraints.FreezeAll;
		//speed = 0;
		if (lightSourse != null)
			lightSourse.enabled = false;
		col.enabled = false;
		projectilePS.Stop();
		projectilePS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

		var contact = collision.contacts[0];
		var rot = Quaternion.FromToRotation(Vector3.up, contact.normal);
		var pos = contact.point + contact.normal * hitOffset;

		//Spawn hit effect on collision
		if (hit != null)
		{
			hit.transform.rotation = rot;
			hit.transform.position = pos;
			if (UseFirePointRotation)
				hit.transform.rotation = gameObject.transform.rotation * Quaternion.Euler(0, 180f, 0);
			else if (rotationOffset != Vector3.zero)
				hit.transform.rotation = Quaternion.Euler(rotationOffset);
			else
				hit.transform.LookAt(contact.point + contact.normal);
			hitPS.Play();
		}

		//Removing trail from the projectile on cillision enter or smooth removing. Detached elements must have "AutoDestroying script"
		foreach (var detachedPrefab in Detached)
			if (detachedPrefab != null)
			{
				var detachedPS = detachedPrefab.GetComponent<ParticleSystem>();
				detachedPS.Stop();
			}

		if (notDestroy)
		{
			StartCoroutine(DisableTimer(hitPS.main.duration));
		}
		else
		{
			if (hitPS != null)
				Destroy(gameObject, hitPS.main.duration);
			else
				Destroy(gameObject, 1);
		}
	}

	protected virtual IEnumerator DisableTimer(float time)
	{
		yield return new WaitForSeconds(time);
		if (gameObject.activeSelf)
			gameObject.SetActive(false);
		yield break;
	}
}
