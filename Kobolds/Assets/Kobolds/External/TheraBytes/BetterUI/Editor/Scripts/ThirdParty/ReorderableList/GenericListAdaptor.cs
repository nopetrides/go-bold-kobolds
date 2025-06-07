// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace TheraBytes.BetterUi.Editor.ThirdParty
{
	/// <summary>
	///     Reorderable list adaptor for generic list.
	/// </summary>
	/// <remarks>
	///     <para>
	///         This adaptor can be subclassed to add special logic to item height calculation.
	///         You may want to implement a custom adaptor class where specialised functionality
	///         is needed.
	///     </para>
	///     <para>
	///         List elements which implement the <see cref="System.ICloneable" /> interface are
	///         cloned using that interface upon duplication; otherwise the item value or reference is
	///         simply copied.
	///     </para>
	/// </remarks>
	/// <typeparam name="T">Type of list element.</typeparam>
	public class GenericListAdaptor<T> : IReorderableListAdaptor
	{
		private readonly ReorderableListControl.ItemDrawer<T> _itemDrawer;

		/// <summary>
		///     Fixed height of each list item.
		/// </summary>
		public float FixedItemHeight;

#region Construction

		/// <summary>
		///     Initializes a new instance of <see cref="GenericListAdaptor{T}" />.
		/// </summary>
		/// <param name="list">The list which can be reordered.</param>
		/// <param name="itemDrawer">Callback to draw list item.</param>
		/// <param name="itemHeight">Height of list item in pixels.</param>
		public GenericListAdaptor(IList<T> list, ReorderableListControl.ItemDrawer<T> itemDrawer, float itemHeight)
		{
			List = list;
			_itemDrawer = itemDrawer ?? ReorderableListGUI.DefaultItemDrawer;
			FixedItemHeight = itemHeight;
		}

#endregion

		/// <summary>
		///     Gets the underlying list data structure.
		/// </summary>
		public IList<T> List { get; }

		/// <summary>
		///     Gets element from list.
		/// </summary>
		/// <param name="index">Zero-based index of element.</param>
		/// <returns>
		///     The element.
		/// </returns>
		public T this[int index] => List[index];

#region IReorderableListAdaptor - Implementation

		/// <inheritdoc />
		public int Count => List.Count;

		/// <inheritdoc />
		public virtual bool CanDrag(int index)
		{
			return true;
		}

		/// <inheritdoc />
		public virtual bool CanRemove(int index)
		{
			return true;
		}

		/// <inheritdoc />
		public virtual void Add()
		{
			List.Add(default);
		}

		/// <inheritdoc />
		public virtual void Insert(int index)
		{
			List.Insert(index, default);
		}

		/// <inheritdoc />
		public virtual void Duplicate(int index)
		{
			var newItem = List[index];

			var existingItem = newItem as ICloneable;
			if (existingItem != null)
				newItem = (T) existingItem.Clone();

			List.Insert(index + 1, newItem);
		}

		/// <inheritdoc />
		public virtual void Remove(int index)
		{
			List.RemoveAt(index);
		}

		/// <inheritdoc />
		public virtual void Move(int sourceIndex, int destIndex)
		{
			if (destIndex > sourceIndex)
				--destIndex;

			var item = List[sourceIndex];
			List.RemoveAt(sourceIndex);
			List.Insert(destIndex, item);
		}

		/// <inheritdoc />
		public virtual void Clear()
		{
			List.Clear();
		}

		/// <inheritdoc />
		public virtual void BeginGUI()
		{
		}

		/// <inheritdoc />
		public virtual void EndGUI()
		{
		}

		/// <inheritdoc />
		public virtual void DrawItemBackground(Rect position, int index)
		{
		}

		/// <inheritdoc />
		public virtual void DrawItem(Rect position, int index)
		{
			List[index] = _itemDrawer(position, List[index]);
		}

		/// <inheritdoc />
		public virtual float GetItemHeight(int index)
		{
			return FixedItemHeight;
		}

#endregion
	}
}
