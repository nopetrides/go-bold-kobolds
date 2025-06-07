using FIMSpace.FTools;
using UnityEngine;

namespace FIMSpace.BonesStimulation
{
	public partial class BonesStimulator
	{
		private static readonly Vector2 _UNLIMITED = new(-180, 180);
		private bool useAxisBlending;
		private bool useAxisLimits;

		/// <summary>
		///     Need to be executed when changing BlendAxis variables and LimitAxis variables
		/// </summary>
		public void USER_CheckExtraSettingsAfterManualChange()
		{
			if (BlendXAxis != 1f || BlendYAxis != 1f || BlendZAxis != 1f)
				useAxisBlending = true;
			else useAxisBlending = false;

			if (LimitXAxisRotation != _UNLIMITED || LimitYAxisRotation != _UNLIMITED ||
				LimitZAxisRotation != _UNLIMITED)
				useAxisLimits = true;
			else useAxisLimits = false;

			if (UseCollisions) RefreshCollidersRadius();
		}

		private void PostProcessing()
		{
			Bone b;

			if (useAxisBlending)
			{
				b = Bones[0];

				while (b != null)
				{
					var localEulers = b.transform.localEulerAngles;
					var srcEulers = b.srcAnimatorLocRotation.eulerAngles;

					localEulers.x = BlendAxisAngle(BlendXAxis, srcEulers.x, localEulers.x);
					localEulers.y = BlendAxisAngle(BlendYAxis, srcEulers.y, localEulers.y);
					localEulers.z = BlendAxisAngle(BlendZAxis, srcEulers.z, localEulers.z);

					//if (BlendXAxis <= 0f) localEulers.x = b.srcAnimatorLocRotation.eulerAngles.x;
					//else if (BlendXAxis < 1f) localEulers.x = Mathf.LerpAngle(b.srcAnimatorLocRotation.eulerAngles.x, localEulers.x, BlendXAxis);

					//if (BlendYAxis <= 0f) localEulers.y = b.srcAnimatorLocRotation.eulerAngles.y;
					//else if (BlendYAxis < 1f) localEulers.y = Mathf.LerpAngle(b.srcAnimatorLocRotation.eulerAngles.y, localEulers.y, BlendYAxis);

					//if (BlendZAxis <= 0f) localEulers.z = b.srcAnimatorLocRotation.eulerAngles.z;
					//else if (BlendZAxis < 1f) localEulers.z = Mathf.LerpAngle(b.srcAnimatorLocRotation.eulerAngles.z, localEulers.z, BlendZAxis);

					//if ( FreezeYAxis) localEulers.y = b.srcAnimatorLocRotation.eulerAngles.y;
					//if ( FreezeZAxis) localEulers.z = b.srcAnimatorLocRotation.eulerAngles.z;
					//Quaternion nRot = Quaternion.AngleAxis(localEulers.x, b.transform.parent.right);
					//nRot *= Quaternion.AngleAxis(localEulers.y, b.transform.parent.up);
					//nRot *= Quaternion.AngleAxis(localEulers.z, b.transform.parent.forward);
					//b.transform.parent.rotation * 
					//b.transform.rotation = nRot;

					var preRot = b.transform.localRotation;
					var nRot = Quaternion.Euler(localEulers);
					nRot = FMuscle_Quaternion.EnsureQuaternionContinuity(preRot, nRot);
					b.transform.localRotation = nRot;
					b = b.Child;
				}
			}

			if (useAxisLimits)
			{
				b = Bones[0];
				while (b != null)
				{
					var localEulers = b.transform.localEulerAngles;
					var srcEulers = b.srcAnimatorLocRotation.eulerAngles;

					var deltaDiff = new Vector3();
					deltaDiff.x = Mathf.DeltaAngle(localEulers.x, srcEulers.x);
					deltaDiff.y = Mathf.DeltaAngle(localEulers.y, srcEulers.y);
					deltaDiff.z = Mathf.DeltaAngle(localEulers.z, srcEulers.z);

					var limits = new Vector3();
					limits.x = LimitAxisCalc(deltaDiff.x, LimitXAxisRotation.x, LimitXAxisRotation.y);
					limits.y = LimitAxisCalc(deltaDiff.y, LimitYAxisRotation.x, LimitYAxisRotation.y);
					limits.z = LimitAxisCalc(deltaDiff.z, LimitZAxisRotation.x, LimitZAxisRotation.y);

					var limited = false;
					if (limits.x != deltaDiff.x)
					{
						limited = true;
						localEulers.x = LimitOutput(srcEulers.x, limits.x, deltaDiff.x, b);
					}

					if (limits.y != deltaDiff.y)
					{
						limited = true;
						localEulers.y = LimitOutput(srcEulers.y, limits.y, deltaDiff.y, b);
					}

					if (limits.z != deltaDiff.z)
					{
						limited = true;
						localEulers.z = LimitOutput(srcEulers.z, limits.z, deltaDiff.z, b);
					}

					if (limited)
					{
						var preRot = b.transform.localRotation;
						var nRot = Quaternion.Euler(localEulers);
						nRot = FMuscle_Quaternion.EnsureQuaternionContinuity(preRot, nRot);
						b.transform.localRotation = nRot;
					}

					b = b.Child;
				}
			}


			elapsedDelta = 0;

			b = Bones[0];
			while (b != null)
			{
				b.RefreshFinalPose();
				b = b.Child;
			}
		}


		private float BlendAxisAngle(float blend, float src, float target)
		{
			if (blend <= 0f) return src;
			if (blend < 1f) return Mathf.LerpAngle(src, target, blend);
			return target;
		}

		private float LimitAxisCalc(float diff, float limitLow, float limitUp)
		{
			if (diff < limitLow)
			{
				if (limitLow > -180)
					if (diff < limitLow)
						diff = limitLow;
			}
			else if (diff > limitUp)
			{
				if (limitUp < 180)
					if (diff > limitUp)
						diff = limitUp;
			}

			return diff;
		}

		private float LimitOutput(float src, float limited, float diff, Bone b)
		{
			if (DampenOnLimits > 0f)
			{
				var factor = Mathf.Min(1f, Mathf.Abs(diff - limited) / 90f) * DampenOnLimits;
				if (b.Child != null) b.Child.MomentDampen += factor;
				//if (b.Parent != null) b.Parent.MomentDampen += factor;
				//b.MomentDampen += factor;
				if (b.MomentDampen > 1f) b.MomentDampen = 1f;
			}

			if (ElasticLimits <= 0f)
				return src - limited;
			var lim = src - limited;
			var limV = Mathf.Min(1f, Mathf.Abs(diff - limited) / 90f);
			limV = ElasticLimitCurve.Evaluate(limV);

			return Mathf.Lerp(lim, src - diff, limV * ElasticLimits);
		}
	}
}
