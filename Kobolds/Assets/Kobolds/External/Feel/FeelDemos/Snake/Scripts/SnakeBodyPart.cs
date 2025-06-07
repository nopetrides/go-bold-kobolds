using System.Collections;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.Feel
{
	/// <summary>
	///     A class used to handle Feel's Snake demo's snake bodyparts
	/// </summary>
	[AddComponentMenu("")]
	public class SnakeBodyPart : MonoBehaviour
	{
		/// a position recorder this body part will look at to know where to go to
		public MMPositionRecorder TargetRecorder;

		/// a feedback to play when food gets eaten
		public MMFeedbacks EatFeedback;

		/// a feedback to play when this part appears
		public MMFeedbacks NewFeedback;

		public int Offset = 20;
		public int Index;
		protected BoxCollider2D _collider2D;

		protected Snake _snake;

		/// <summary>
		///     On awake we store our collider and enable it after a delay
		/// </summary>
		protected virtual void Awake()
		{
			_collider2D = gameObject.MMGetComponentNoAlloc<BoxCollider2D>();
			StartCoroutine(ActivateCollider());
		}

		/// <summary>
		///     On update, we move to the recorded position of our predecessor
		/// </summary>
		protected void Update()
		{
			transform.position = TargetRecorder.Positions[Offset];
		}

		/// <summary>
		///     If we connect with the snake's head, we lose a part
		/// </summary>
		/// <param name="other"></param>
		protected void OnTriggerEnter2D(Collider2D other)
		{
			if (Index == 0) return;

			_snake = other.GetComponent<Snake>();

			if (_snake != null) _snake.Lose(this);
		}

		/// <summary>
		///     Activates this part's collider
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator ActivateCollider()
		{
			yield return MMCoroutine.WaitFor(1f);
			_collider2D.enabled = true;
		}

		/// <summary>
		///     Called when the snake eats a new food
		/// </summary>
		/// <param name="intensity"></param>
		public virtual void Eat(float intensity)
		{
			EatFeedback?.PlayFeedbacks(transform.position, intensity);
		}

		/// <summary>
		///     Called when instantiating a new body part
		/// </summary>
		public virtual void New()
		{
			NewFeedback?.Initialization();
			NewFeedback?.PlayFeedbacks();
		}
	}
}
