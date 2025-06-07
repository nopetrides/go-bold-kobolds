using System;
using System.Collections;
using System.Collections.Generic;

namespace LeTai.TrueShadow.Demo
{
	public enum AdsrStage
	{
		Off,
		Attack,
		Decay,
		Sustain,
		Release
	}

	public class AdsrEnvelop : IEnumerator<double>
	{
		private int currentSample;
		public int numAttackSamples;
		public int numDecaySamples;
		public int numReleaseSamples;

		private int releaseSample;
		public double sustainScale;

		public AdsrStage CurrentStage { get; private set; } = AdsrStage.Off;

		public bool MoveNext()
		{
			currentSample++;
			MaybeAdvancesStage();

			switch (CurrentStage)
			{
				case AdsrStage.Off:
					Current = 0;
					break;
				case AdsrStage.Attack:
					Current = Map(currentSample, 0, numAttackSamples, 0, 1);
					break;
				case AdsrStage.Decay:
					Current = Map(currentSample - numAttackSamples, 0, numDecaySamples, 1, sustainScale);
					break;
				case AdsrStage.Sustain:
					Current = sustainScale;
					break;
				case AdsrStage.Release:
					Current = Map(currentSample - releaseSample, 0, numReleaseSamples, sustainScale, 0);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			return true;
		}

		public void Reset()
		{
			currentSample = 0;
			CurrentStage = AdsrStage.Attack;
		}

		public double Current { get; private set; }

		object IEnumerator.Current => Current;

		public void Dispose()
		{
		}

		public void Release()
		{
			CurrentStage = AdsrStage.Release;
			releaseSample = currentSample;
		}

		private void MaybeAdvancesStage()
		{
			if (CurrentStage == AdsrStage.Attack)
				if (currentSample > numAttackSamples)
					CurrentStage = AdsrStage.Decay;

			if (CurrentStage == AdsrStage.Decay)
				if (currentSample > numAttackSamples + numDecaySamples)
					CurrentStage = AdsrStage.Sustain;

			if (CurrentStage == AdsrStage.Release)
				if (currentSample - releaseSample > numReleaseSamples)
					CurrentStage = AdsrStage.Off;
		}


		private static double Map(double value, double fromLow, double fromHigh, double toLow, double toHigh)
		{
			return (value - fromLow) * (toHigh - toLow) / (fromHigh - fromLow) + toLow;
		}
	}
}
