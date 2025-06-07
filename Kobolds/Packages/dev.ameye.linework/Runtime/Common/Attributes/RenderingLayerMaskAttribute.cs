using UnityEngine;

namespace Linework.Common.Attributes
{
	public class RenderingLayerMaskAttribute : PropertyAttribute
	{
		public RenderingLayerMaskAttribute(bool showLabel = true)
		{
			ShowLabel = showLabel;
		}

		public bool ShowLabel { get; }
	}
}
