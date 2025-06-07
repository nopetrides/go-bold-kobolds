using System.Collections.Generic;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;
#if MM_UGUI2
#endif

namespace MoreMountains.Feel
{
	[AddComponentMenu("")]
	public class FeelSpringsComparisonDemo : MonoBehaviour
	{
		[Header("Spring")]
		public List<MMSpringFloat> Springs;

		public List<Transform> MovingObjects;
		public FeelSpringsDemoSlider BumpAmountSlider;

		protected Vector3 _newPosition;

		protected float _range = 0.375f;

		protected virtual void Update()
		{
			for (var i = 0; i < Springs.Count; i++)
			{
				Springs[i].UpdateSpringValue(Time.deltaTime);

				_newPosition = MovingObjects[i].transform.localPosition;
				_newPosition.x = MMMaths.Remap(Springs[i].CurrentValue, -1f, 1f, -_range, _range);
				MovingObjects[i].transform.localPosition = _newPosition;
			}
		}

		protected virtual void OnEnable()
		{
			foreach (var spring in Springs)
			{
				spring.CurrentValue = 0f;
				spring.TargetValue = 0f;
				spring.Velocity = 0f;
			}
		}

		public virtual void RandomBump()
		{
			var bumpAmount = BumpAmountSlider.value;
			foreach (var spring in Springs) spring.Bump(bumpAmount);
		}
	}
}
