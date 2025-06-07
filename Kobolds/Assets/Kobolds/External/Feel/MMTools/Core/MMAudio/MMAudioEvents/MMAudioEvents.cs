using UnityEngine;
using UnityEngine.Audio;

namespace MoreMountains.Tools
{
	public class MMAudioEvents
	{
	}

	/// <summary>
	///     A struct used to trigger sounds
	/// </summary>
	public struct MMSfxEvent
	{
		private static event Delegate OnEvent;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void RuntimeInitialization()
		{
			OnEvent = null;
		}

		public static void Register(Delegate callback)
		{
			OnEvent += callback;
		}

		public static void Unregister(Delegate callback)
		{
			OnEvent -= callback;
		}

		public delegate void Delegate(
			AudioClip clipToPlay, AudioMixerGroup audioGroup = null, float volume = 1f, float pitch = 1f,
			int priority = 128);

		public static void Trigger(
			AudioClip clipToPlay, AudioMixerGroup audioGroup = null, float volume = 1f, float pitch = 1f,
			int priority = 128)
		{
			OnEvent?.Invoke(clipToPlay, audioGroup, volume, pitch, priority);
		}
	}
}
