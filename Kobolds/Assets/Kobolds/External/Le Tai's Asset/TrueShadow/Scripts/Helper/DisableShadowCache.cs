using LeTai.TrueShadow.PluginInterfaces;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LeTai.TrueShadow
{
	[ExecuteAlways]
	[RequireComponent(typeof(TrueShadow))]
	public class DisableShadowCache : MonoBehaviour, ITrueShadowCustomHashProvider
	{
		public bool everyFrame;
		private TrueShadow shadow;

		private void Update()
		{
			if (everyFrame)
				Dirty();
		}

		private void OnEnable()
		{
			shadow = GetComponent<TrueShadow>();
			Dirty();
		}

		private void OnDisable()
		{
			shadow.CustomHash = 0;
			shadow.SetTextureDirty();
		}

		private void Dirty()
		{
			shadow.CustomHash = Random.Range(int.MinValue, int.MaxValue);
			shadow.SetTextureDirty();
		}
	}
}
