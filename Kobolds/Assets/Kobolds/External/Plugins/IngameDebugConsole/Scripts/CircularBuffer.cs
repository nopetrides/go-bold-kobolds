using System;
using UnityEngine;

namespace IngameDebugConsole
{
	public class CircularBuffer<T>
	{
		private readonly T[] array;
		private int startIndex;

		public CircularBuffer(int capacity)
		{
			array = new T[capacity];
		}

		public int Count { get; private set; }
		public T this[int index] => array[(startIndex + index) % array.Length];

		// Old elements are overwritten when capacity is reached
		public void Add(T value)
		{
			if (Count < array.Length)
			{
				array[Count++] = value;
			}
			else
			{
				array[startIndex] = value;
				if (++startIndex >= array.Length)
					startIndex = 0;
			}
		}
	}

	public class DynamicCircularBuffer<T>
	{
		private T[] array;
		private int startIndex;

		public DynamicCircularBuffer(int initialCapacity = 2)
		{
			array = new T[initialCapacity];
		}

		public int Count { get; private set; }
		public int Capacity => array.Length;

		public T this[int index]
		{
			get => array[(startIndex + index) % array.Length];
			set => array[(startIndex + index) % array.Length] = value;
		}

		private void SetCapacity(int capacity)
		{
			var newArray = new T[capacity];
			if (Count > 0)
			{
				var elementsBeforeWrap = Mathf.Min(Count, array.Length - startIndex);
				Array.Copy(array, startIndex, newArray, 0, elementsBeforeWrap);
				if (elementsBeforeWrap < Count)
					Array.Copy(array, 0, newArray, elementsBeforeWrap, Count - elementsBeforeWrap);
			}

			array = newArray;
			startIndex = 0;
		}

		/// <summary>Inserts the value to the beginning of the collection.</summary>
		public void AddFirst(T value)
		{
			if (array.Length == Count)
				SetCapacity(Mathf.Max(array.Length * 2, 4));

			startIndex = startIndex > 0 ? startIndex - 1 : array.Length - 1;
			array[startIndex] = value;
			Count++;
		}

		/// <summary>Adds the value to the end of the collection.</summary>
		public void Add(T value)
		{
			if (array.Length == Count)
				SetCapacity(Mathf.Max(array.Length * 2, 4));

			this[Count++] = value;
		}

		public void AddRange(DynamicCircularBuffer<T> other)
		{
			if (other.Count == 0)
				return;

			if (array.Length < Count + other.Count)
				SetCapacity(Mathf.Max(array.Length * 2, Count + other.Count));

			var insertStartIndex = (startIndex + Count) % array.Length;
			var elementsBeforeWrap = Mathf.Min(other.Count, array.Length - insertStartIndex);
			var otherElementsBeforeWrap = Mathf.Min(other.Count, other.array.Length - other.startIndex);

			Array.Copy(
				other.array, other.startIndex, array, insertStartIndex,
				Mathf.Min(elementsBeforeWrap, otherElementsBeforeWrap));
			if (elementsBeforeWrap < otherElementsBeforeWrap) // This array wrapped before the other array
				Array.Copy(
					other.array, other.startIndex + elementsBeforeWrap, array, 0,
					otherElementsBeforeWrap - elementsBeforeWrap);
			else if (elementsBeforeWrap > otherElementsBeforeWrap) // The other array wrapped before this array
				Array.Copy(
					other.array, 0, array, insertStartIndex + otherElementsBeforeWrap,
					elementsBeforeWrap - otherElementsBeforeWrap);

			var copiedElements = Mathf.Max(elementsBeforeWrap, otherElementsBeforeWrap);
			if (copiedElements < other.Count) // Both arrays wrapped and there's still some elements left to copy
				Array.Copy(
					other.array, copiedElements - otherElementsBeforeWrap, array, copiedElements - elementsBeforeWrap,
					other.Count - copiedElements);

			Count += other.Count;
		}

		public T RemoveFirst()
		{
			var element = array[startIndex];
			array[startIndex] = default;

			if (++startIndex == array.Length)
				startIndex = 0;

			Count--;
			return element;
		}

		public T RemoveLast()
		{
			var index = (startIndex + Count - 1) % array.Length;
			var element = array[index];
			array[index] = default;

			Count--;
			return element;
		}

		public int RemoveAll(Predicate<T> shouldRemoveElement)
		{
			return RemoveAll<T>(shouldRemoveElement, null, null);
		}

		public int RemoveAll<Y>(
			Predicate<T> shouldRemoveElement, Action<T, int> onElementIndexChanged,
			DynamicCircularBuffer<Y> synchronizedBuffer)
		{
			var synchronizedArray = synchronizedBuffer != null ? synchronizedBuffer.array : null;
			var elementsBeforeWrap = Mathf.Min(Count, array.Length - startIndex);
			var removedElements = 0;
			int i = startIndex, newIndex = startIndex, endIndex = startIndex + elementsBeforeWrap;
			for (; i < endIndex; i++)
				if (shouldRemoveElement(array[i]))
				{
					removedElements++;
				}
				else
				{
					if (removedElements > 0)
					{
						var element = array[i];
						array[newIndex] = element;

						if (synchronizedArray != null)
							synchronizedArray[newIndex] = synchronizedArray[i];

						if (onElementIndexChanged != null)
							onElementIndexChanged(element, newIndex - startIndex);
					}

					newIndex++;
				}

			i = 0;
			endIndex = Count - elementsBeforeWrap;

			if (newIndex < array.Length)
				for (; i < endIndex; i++)
					if (shouldRemoveElement(array[i]))
					{
						removedElements++;
					}
					else
					{
						var element = array[i];
						array[newIndex] = element;

						if (synchronizedArray != null)
							synchronizedArray[newIndex] = synchronizedArray[i];

						if (onElementIndexChanged != null)
							onElementIndexChanged(element, newIndex - startIndex);

						if (++newIndex == array.Length)
						{
							i++;
							break;
						}
					}

			if (newIndex == array.Length)
			{
				newIndex = 0;
				for (; i < endIndex; i++)
					if (shouldRemoveElement(array[i]))
					{
						removedElements++;
					}
					else
					{
						if (removedElements > 0)
						{
							var element = array[i];
							array[newIndex] = element;

							if (synchronizedArray != null)
								synchronizedArray[newIndex] = synchronizedArray[i];

							if (onElementIndexChanged != null)
								onElementIndexChanged(element, newIndex + elementsBeforeWrap);
						}

						newIndex++;
					}
			}

			TrimEnd(removedElements);
			if (synchronizedBuffer != null)
				synchronizedBuffer.TrimEnd(removedElements);

			return removedElements;
		}

		public void TrimStart(int trimCount, Action<T> perElementCallback = null)
		{
			TrimInternal(trimCount, startIndex, perElementCallback);
			startIndex = (startIndex + trimCount) % array.Length;
		}

		public void TrimEnd(int trimCount, Action<T> perElementCallback = null)
		{
			TrimInternal(trimCount, (startIndex + Count - trimCount) % array.Length, perElementCallback);
		}

		private void TrimInternal(int trimCount, int startIndex, Action<T> perElementCallback)
		{
			var elementsBeforeWrap = Mathf.Min(trimCount, array.Length - startIndex);
			if (perElementCallback == null)
			{
				Array.Clear(array, startIndex, elementsBeforeWrap);
				if (elementsBeforeWrap < trimCount)
					Array.Clear(array, 0, trimCount - elementsBeforeWrap);
			}
			else
			{
				for (int i = startIndex, endIndex = startIndex + elementsBeforeWrap; i < endIndex; i++)
				{
					perElementCallback(array[i]);
					array[i] = default;
				}

				for (int i = 0, endIndex = trimCount - elementsBeforeWrap; i < endIndex; i++)
				{
					perElementCallback(array[i]);
					array[i] = default;
				}
			}

			Count -= trimCount;
		}

		public void Clear()
		{
			var elementsBeforeWrap = Mathf.Min(Count, array.Length - startIndex);
			Array.Clear(array, startIndex, elementsBeforeWrap);
			if (elementsBeforeWrap < Count)
				Array.Clear(array, 0, Count - elementsBeforeWrap);

			startIndex = 0;
			Count = 0;
		}

		public int IndexOf(T value)
		{
			var elementsBeforeWrap = Mathf.Min(Count, array.Length - startIndex);
			var index = Array.IndexOf(array, value, startIndex, elementsBeforeWrap);
			if (index >= 0)
				return index - startIndex;

			if (elementsBeforeWrap < Count)
			{
				index = Array.IndexOf(array, value, 0, Count - elementsBeforeWrap);
				if (index >= 0)
					return index + elementsBeforeWrap;
			}

			return -1;
		}

		public void ForEach(Action<T> action)
		{
			var elementsBeforeWrap = Mathf.Min(Count, array.Length - startIndex);
			for (int i = startIndex, endIndex = startIndex + elementsBeforeWrap; i < endIndex; i++)
				action(array[i]);
			for (int i = 0, endIndex = Count - elementsBeforeWrap; i < endIndex; i++)
				action(array[i]);
		}
	}
}
