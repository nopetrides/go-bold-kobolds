#if MM_UGUI2
using TMPro;
using UnityEngine;

namespace MoreMountains.Feedbacks
{
	[AddComponentMenu("More Mountains/Springs/MM Spring TMP Text Color")]
	public class MMSpringTMPTextColor : MMSpringColorComponent<TMP_Text>
	{
		public override Color TargetColor
		{
			get => Target.color;
			set => Target.color = value;
		}
	}
}
#endif
