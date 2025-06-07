using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UIElements;

namespace MoreMountains.FeedbacksForThirdParty
{
	/// <summary>
	///     This feedback will let you scale an element on a target UI Document
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("This feedback will let you scale an element on a target UI Document")]
	[FeedbackPath("UI Toolkit/UITK Scale")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks.UIToolkit")]
	public class MMF_UIToolkitScale : MMF_UIToolkitVector2Base
	{
		protected override void SetValue(Vector2 newValue)
		{
			foreach (var element in _visualElements)
			{
				element.style.scale = new StyleScale(new Scale(newValue));
				HandleMarkDirty(element);
			}
		}

		protected override Vector2 GetInitialValue()
		{
			return _visualElements[0].resolvedStyle.scale.value;
		}
	}
}
