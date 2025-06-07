#if MM_UGUI2
using TMPro;
using UnityEngine;

namespace MoreMountains.Feedbacks
{
	[AddComponentMenu("More Mountains/Springs/MM Spring TMP Character Spacing")]
	public class MMSpringTMPCharacterSpacing : MMSpringFloatComponent<TMP_Text>
	{
		public override float TargetFloat
		{
			get => Target.characterSpacing;
			set => Target.characterSpacing = value;
		}
	}
}
#endif
