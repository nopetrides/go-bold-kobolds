using FIMSpace.Basics;
using FIMSpace.GroundFitter;
using UnityEditor;
using UnityEngine;

public class FGroundFitter_Demo_RMCharacterController : FSimpleFitter
{
	protected Animator animator;
	protected FAnimationClips clips;
	private float gravity;

	protected override void Start()
	{
		base.Start();
		animator = GetComponentInChildren<Animator>();

		clips = new FAnimationClips(animator);
		clips.AddClip("Idle");
		clips.AddClip("Walk");
		clips.AddClip("RotateL");
		clips.AddClip("RotateR");
	}

	public void Update()
	{
		// We handle gravity here
		if (optionalCharContr)
		{
			if (optionalCharContr.isGrounded)
			{
				gravity = 0.0f;
			}
			else
			{
				gravity += Time.deltaTime * 10;
				optionalCharContr.Move(Vector3.down * gravity * Time.deltaTime);
			}

			// Movement forward is handled by root motion and OnAnimatorMove() method within parent class
		}

		if (Input.GetKey(KeyCode.A))
		{
			clips.CrossFade("RotateL");
		}
		else if (Input.GetKey(KeyCode.D))
		{
			clips.CrossFade("RotateR");
		}
		else
		{
			if (Input.GetKey(KeyCode.W))
				clips.CrossFade("Walk");
			else
				clips.CrossFade("Idle");
		}
	}


#if UNITY_EDITOR
	[CustomEditor(typeof(FGroundFitter_Demo_RMCharacterController))]
	public class FGroundFitter_Demo_RMCharacterControllerEditor : FSimpleFitter_Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
		}
	}
#endif
}
