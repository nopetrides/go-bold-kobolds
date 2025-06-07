using System.Collections;
using UnityEngine;

namespace FIMSpace.FSpine
{
	public partial class FSpineAnimator
	{
		private bool fixedAllow = true;

		/// <summary> Helper flag for basic animate physics mode </summary>
		private bool fixedUpdated;

		/// <summary> Helper counter for start after t-pose feature </summary>
		private int initAfterTPoseCounter;

		// Supporting second solution for fixed animate physics mode
		private bool lateFixedIsRunning;

		private IEnumerator LateFixed()
		{
			var fixedWait = new WaitForFixedUpdate();
			lateFixedIsRunning = true;

			while (true)
			{
				yield return fixedWait;
				PreCalibrateBones();
				fixedAllow = true;
				if (lateFixedIsRunning == false) yield break;
			}
		}
	}
}
