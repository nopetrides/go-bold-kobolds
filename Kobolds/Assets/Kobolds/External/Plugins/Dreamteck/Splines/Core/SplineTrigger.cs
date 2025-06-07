using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Dreamteck.Splines
{
	[Serializable]
	public class TriggerGroup
	{
#if UNITY_EDITOR
		public bool open;
#endif

		public bool enabled = true;
		public string name = "";
		public Color color = Color.white;
		public SplineTrigger[] triggers = new SplineTrigger[0];

		public void Check(double start, double end, SplineUser user = null)
		{
			for (var i = 0; i < triggers.Length; i++)
			{
				if (triggers[i] == null) continue;

				if (triggers[i].Check(start, end)) triggers[i].Invoke(user);
			}
		}

		public void Reset()
		{
			for (var i = 0; i < triggers.Length; i++) triggers[i].Reset();
		}

		/// <summary>
		///     Returns all triggers within the specified range
		/// </summary>
		public List<SplineTrigger> GetTriggers(double from, double to)
		{
			var triggerList = new List<SplineTrigger>();
			for (var i = 0; i < triggers.Length; i++)
			{
				if (triggers[i] == null) continue;
				if (triggers[i].position >= from && triggers[i].position <= to) triggerList.Add(triggers[i]);
			}

			return triggerList;
		}

		/// <summary>
		///     Creates a new trigger inside the group
		/// </summary>
		public SplineTrigger AddTrigger(double position, SplineTrigger.Type type)
		{
			return AddTrigger(position, type, "Trigger " + (triggers.Length + 1), Color.white);
		}

		/// <summary>
		///     Creates a new trigger inside the group
		/// </summary>
		public SplineTrigger AddTrigger(double position, SplineTrigger.Type type, string name, Color color)
		{
			var newTrigger = new SplineTrigger(type);
			newTrigger.position = position;
			newTrigger.color = color;
			newTrigger.name = name;
			ArrayUtility.Add(ref triggers, newTrigger);
			return newTrigger;
		}

		/// <summary>
		///     Removes the trigger at the given index from the group
		/// </summary>
		public void RemoveTrigger(int index)
		{
			ArrayUtility.RemoveAt(ref triggers, index);
		}
	}

	[Serializable]
	public class SplineTrigger
	{
		public enum Type
		{
			Double,
			Forward,
			Backward
		}

		public string name = "Trigger";

		[SerializeField]
		public Type type = Type.Double;

		public bool workOnce;

		[Range(0f, 1f)]
		public double position = 0.5;

		[SerializeField]
		public bool enabled = true;

		[SerializeField]
		public Color color = Color.white;

		[SerializeField]
		[HideInInspector]
		public TriggerEvent onCross = new();

		private bool worked;

		public SplineTrigger(Type t)
		{
			type = t;
			enabled = true;
			onCross = new TriggerEvent();
		}

		/// <summary>
		///     Add a new UnityAction to the trigger
		/// </summary>
		/// <param name="action"></param>
		public void AddListener(UnityAction<SplineUser> action)
		{
			onCross.AddListener(action);
		}

		public void AddListener(UnityAction action)
		{
			var addAction = new UnityAction<SplineUser>(user => { action.Invoke(); });
			onCross.AddListener(addAction);
		}

		public void RemoveListener(UnityAction<SplineUser> action)
		{
			onCross.RemoveListener(action);
		}

		public void RemoveAllListeners()
		{
			onCross.RemoveAllListeners();
		}

		public void Reset()
		{
			worked = false;
		}

		public bool Check(double previousPercent, double currentPercent)
		{
			if (!enabled) return false;
			if (workOnce && worked) return false;
			var passed = false;
			switch (type)
			{
				case Type.Double:
					passed = (previousPercent <= position && currentPercent >= position) ||
							(currentPercent <= position && previousPercent >= position); break;
				case Type.Forward: passed = previousPercent <= position && currentPercent >= position; break;
				case Type.Backward: passed = currentPercent <= position && previousPercent >= position; break;
			}

			if (passed) worked = true;
			return passed;
		}

		public void Invoke(SplineUser user = null)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying) return;
#endif
			onCross.Invoke(user);
		}

		[Serializable]
		public class TriggerEvent : UnityEvent<SplineUser>
		{
		}
	}
}
