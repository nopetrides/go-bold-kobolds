using System.Collections.Generic;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace MoreMountains.Feel
{
	[AddComponentMenu("")]
	public class FeelCardsUIStackButton : MonoBehaviour
	{
		/// the MMFeedback to play when pressing the stack button
		public MMFeedbacks StackFeedback;

		/// a list of feedbacks that should prevent the button from working if any of them is still playing
		public List<MMFeedbacks> BlockerFeedbacks;

		public virtual void Stack()
		{
			var blocked = false;
			foreach (var feedbacks in BlockerFeedbacks)
				if (feedbacks.IsPlaying)
					blocked = true;

			if (blocked) return;

			StackFeedback?.PlayFeedbacks();
			gameObject.SetActive(false);
		}
	}
}
