using UnityEngine;
#if MM_UI
using UnityEngine.UI;
#endif

namespace MoreMountains.Tools
{
	public class MMPlotterAxis : MonoBehaviour
	{
		public Transform PlotterCurvePoint;

		public Transform PositionPoint;
		public Transform PositionPointVertical;
		public Transform RotationPoint;
		public Transform ScalePoint;

		public virtual void SetLabel(string newLabel)
		{
#if MM_UI
			Label.text = newLabel;
#endif
		}
#if MM_UI
		public Text Label;
		public Text TimeLabel;
#endif
	}
}
