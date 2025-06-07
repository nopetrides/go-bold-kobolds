using UnityEngine;

namespace Lolopupka
{
	[RequireComponent(typeof(AudioSource))]
	public class StepEffects : MonoBehaviour
	{
		[SerializeField] private AudioClip[] stepSVX;
		[SerializeField] private GameObject stepVFX;
		private AudioSource audioSource;

		private ProceduralAnimation proceduralAnimation;

		private void Start()
		{
			TryGetComponent(out audioSource);

			if (TryGetComponent(out proceduralAnimation))
				proceduralAnimation.OnStepFinished += ProceduralAnimation_OnStepFinished;
			else
				Debug.LogError("procedural animation script required on " + gameObject);
		}

		private void ProceduralAnimation_OnStepFinished(object sender, Vector3 LegPosition)
		{
			if (audioSource != null) audioSource.PlayOneShot(stepSVX[Random.Range(0, stepSVX.Length - 1)]);

			if (stepVFX != null) Instantiate(stepVFX, LegPosition, Quaternion.identity);
		}
	}
}
