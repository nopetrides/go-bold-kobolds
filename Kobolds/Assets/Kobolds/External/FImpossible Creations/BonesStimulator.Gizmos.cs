#if UNITY_EDITOR

using FIMSpace.FEditor;
using UnityEditor;
using UnityEngine;

namespace FIMSpace.BonesStimulation
{
	public partial class BonesStimulator
	{
		private void OnDrawGizmosSelected()
		{
			if (!DrawGizmos) return;

			var c = Handles.color;
			Handles.color = new Color(0.2f, 0.8f, 0.3f, 0.6f);

			if (Bones != null)
				if (Bones.Count > 0)
				{
					var min = 1;
					if (Application.isPlaying) min = 2;
					for (var i = 0; i < Bones.Count - min; i++)
						if (Bones[i].transform != null)
						{
							if (Bones[i + 1] != null)
								if (Bones[i + 1].transform != null)
									FGUI_Handles.DrawBoneHandle(
										Bones[i].transform.position, Bones[i + 1].transform.position);
						}
						else if (i == 0)
						{
							FGUI_Handles.DrawBoneHandle(transform.position, Bones[i + 1].transform.position);
						}
				}


			if (MovementMuscles > 0f || RotationSpaceMuscles > 0f)
				if (HelperOffset != Vector3.zero)
					if (GetLastTransform() != null)
						if (GetLastTransform().parent)
						{
							if (!Application.isPlaying)
							{
								var off = GetLastTransform().TransformVector(HelperOffset);
								Handles.SphereHandleCap(
									0, GetLastTransform().position, Quaternion.identity, off.magnitude * 0.1f,
									EventType.Repaint);
								Handles.SphereHandleCap(
									0, GetLastTransform().position + off, Quaternion.identity, off.magnitude * 0.1f,
									EventType.Repaint);
								Handles.DrawDottedLine(
									GetLastTransform().position, GetLastTransform().position + off, 2f);
							}
							else
							{
								Handles.SphereHandleCap(
									0, Bones[Bones.Count - 2].transform.position, Quaternion.identity,
									HelperOffset.magnitude * 0.1f, EventType.Repaint);
								Handles.SphereHandleCap(
									0, Bones[Bones.Count - 1].transform.position, Quaternion.identity,
									HelperOffset.magnitude * 0.1f, EventType.Repaint);
								Handles.DrawDottedLine(
									Bones[Bones.Count - 2].transform.position,
									Bones[Bones.Count - 1].transform.position, 2f);
							}
						}

			Handles.color = c;

			c = Gizmos.color;

			if (FadeOutDistance > 0f)
			{
				Gizmos.color = new Color(0.4f, 0.55f, 1f, 0.3f);
				Gizmos.DrawWireSphere(transform.position, FadeOutDistance);
				var from = GetDistanceMeasureTransform();
				if (from != null) Gizmos.DrawLine(transform.position, from.transform.position);
			}

			Gizmos.color = c;

			_Gizmos_DrawColliders();
		}
	}
}

#endif
