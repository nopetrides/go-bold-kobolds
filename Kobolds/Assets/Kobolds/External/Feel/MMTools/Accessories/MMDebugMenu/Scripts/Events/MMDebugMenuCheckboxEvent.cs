using UnityEngine;

namespace MoreMountains.Tools
{
	/// <summary>
	///     An event used to broadcast checkbox events from a MMDebugMenu
	/// </summary>
	public struct MMDebugMenuCheckboxEvent
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

		public enum EventModes
		{
			FromCheckbox,
			SetCheckbox
		}

		public delegate void Delegate(
			string checkboxEventName, bool value, EventModes eventMode = EventModes.FromCheckbox);

		public static void Trigger(string checkboxEventName, bool value, EventModes eventMode = EventModes.FromCheckbox)
		{
			OnEvent?.Invoke(checkboxEventName, value, eventMode);
		}
	}
}
