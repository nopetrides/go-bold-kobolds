using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Basics
{
	/// <summary>
	///     FM: Class to choose random elements from list but with possibility to set range to choose random elements without
	///     too fast repetition
	///     For example you have elements like "1,2,3,4,5" and with back range = 2 you will NOT choose elements like 3,'1,1' or
	///     4,3,'5,5'
	/// </summary>
	public class FBasic_NotRepetiveSelector<T>
	{
		private int added;
		private readonly int backRange;
		private readonly List<T> elements;
		private readonly List<int> previousElements = new();

		public FBasic_NotRepetiveSelector(List<T> elements, int backRange)
		{
			this.elements = elements;
			this.backRange = backRange;

			if (backRange > 0 && elements.Count > 1)
			{
				if (backRange > elements.Count - 1) backRange = Mathf.Max(1, elements.Count / 2);

				for (var i = 0; i < backRange; i++) previousElements.Add(-1);
			}
			else
			{
				backRange = 0;
			}

			added = 0;
		}

		/// <summary>
		///     Converting list to array to use by this class - FBasic_NotRepetiveSelector<type>.ArrayToList(array);
		/// </summary>
		/// <param name="elements"></param>
		/// <returns></returns>
		public static List<T> ArrayToList(T[] elements)
		{
			var elems = new List<T>();

			for (var i = 0; i < elements.Length; i++) elems.Add(elements[i]);

			return elems;
		}

		/// <summary>
		///     Returning element from list choosing one which wasn choosed before in defined range in contructor
		/// </summary>
		public T GetElementNotRepetive()
		{
			if (backRange < 1) return elements[Random.Range(0, elements.Count)];

			T e;
			var i = ChooseElementDontRepeat(elements, previousElements, backRange);
			e = elements[i];

			previousElements[added] = i;
			added++;
			if (added > previousElements.Count - 1) added = 0;

			return e;
		}

		/// <summary>
		///     Private calculations method for choosing right element from list
		/// </summary>
		private int ChooseElementDontRepeat(List<T> elements, List<int> previousClips, int backCount)
		{
			int i;
			i = Random.Range(0, elements.Count);

			if (backCount > elements.Count - 1)
			{
				Debug.Log("Back Count too big for given array!");
				return i;
			}

			var was = false;
			for (var j = 0; j < backCount; j++)
				if (previousClips[j] == i)
				{
					was = true;
					break;
				}

			if (was) return ChooseElementDontRepeat(elements, previousClips, backCount);

			return i;
		}
	}
}
