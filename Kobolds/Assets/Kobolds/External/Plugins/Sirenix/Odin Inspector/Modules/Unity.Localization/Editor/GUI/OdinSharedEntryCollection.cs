//-----------------------------------------------------------------------
// <copyright file="OdinSharedEntryCollection.cs" company="Sirenix ApS">
// Copyright (c) Sirenix ApS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector.Modules.Localization.Editor.Internal;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;

namespace Sirenix.OdinInspector.Modules.Localization.Editor
{
	public class OdinSharedEntryCollection
	{
		public enum SortOrderState
		{
			Unsorted,
			Ascending,
			Descending
		}

		public readonly LocalizationTableCollection Collection;
		public readonly HashSet<SharedTableData.SharedTableEntry> FilteredEntries;

		private string _searchTerm = string.Empty;

		public SortOrderState CurrentSortOrderState = SortOrderState.Unsorted;

		public bool IsSearching;

		public List<SharedTableData.SharedTableEntry> SortedEntries;

		private readonly StringComparer stringComparer = new();

		public OdinSharedEntryCollection(LocalizationTableCollection collection)
		{
			Collection = collection;

			FilteredEntries = new HashSet<SharedTableData.SharedTableEntry>();
		}

		public bool IsSorted => CurrentSortOrderState != SortOrderState.Unsorted;

		public int Length => Entries.Count;

		public string SearchTerm
		{
			get => _searchTerm;
			private set
			{
				_searchTerm = value;

				IsSearching = !string.IsNullOrEmpty(value);
			}
		}

		public List<SharedTableData.SharedTableEntry> Entries =>
			IsSorted ? SortedEntries : Collection.SharedData.Entries;

		public SharedTableData.SharedTableEntry this[int index] => Entries[index];

		public bool IsVisible(SharedTableData.SharedTableEntry sharedEntry)
		{
			return !IsSearching || (IsSearching && FilteredEntries.Contains(sharedEntry));
		}

		public bool UpdateSearchTerm<TTable>(
			string value,
			OdinGUITableCollection<TTable> tables,
			LocalizationTableCollection collection,
			bool forceUpdate = false) where TTable : LocalizationTable
		{
			if (SearchTerm == value && !forceUpdate) return false;

			SearchTerm = value;

			if (string.IsNullOrEmpty(SearchTerm)) return true;

			FilteredEntries.Clear();

			for (var i = 0; i < tables.Count; i++)
			{
				var table = tables[i];

				switch (table.Type)
				{
					case OdinGUITable<TTable>.GUITableType.Default:
						switch (table.Asset)
						{
							case AssetTable assetTable:
								var assetCollection = collection as AssetTableCollection;

								for (var j = 0; j < Length; j++)
								{
									var sharedEntry = this[j];

									var assetType = assetCollection.GetEntryAssetType(sharedEntry.Id);

									var asset = OdinLocalizationAssetCache.Get(sharedEntry, assetTable, assetType);

									if (asset == null) continue;

									if (FuzzySearch.Contains(SearchTerm, asset.name)) FilteredEntries.Add(sharedEntry);
								}

								break;

							case StringTable stringTable:
								for (var j = 0; j < Length; j++)
								{
									var sharedEntry = this[j];

									var entry = stringTable.GetEntry(sharedEntry.Id);

									if (entry is null || string.IsNullOrEmpty(entry.Value)) continue;

									if (FuzzySearch.Contains(SearchTerm, entry.Value)) FilteredEntries.Add(sharedEntry);
								}

								break;
						}

						break;

					case OdinGUITable<TTable>.GUITableType.Key:
						for (var j = 0; j < Entries.Count; j++)
							if (FuzzySearch.Contains(SearchTerm, Entries[j].Key))
								FilteredEntries.Add(Entries[j]);

						break;

					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			return true;
		}

		public void SortByKeys(bool preserveCurrentOrder)
		{
			switch (CurrentSortOrderState)
			{
				case SortOrderState.Ascending:
					stringComparer.IsAscending = true;

					if (preserveCurrentOrder)
					{
						var result = Collection.SharedData.Entries.OrderBy(entry => entry.Key, stringComparer)
							.ThenBy(GetOrderIndex)
							.ToList();

						SortedEntries = result;
					}
					else
					{
						SortedEntries = Collection.SharedData.Entries.OrderBy(entry => entry.Key, stringComparer)
							.ToList();
					}

					return;

				case SortOrderState.Descending:
					stringComparer.IsAscending = false;

					if (preserveCurrentOrder)
					{
						var result = Collection.SharedData.Entries.OrderByDescending(entry => entry.Key, stringComparer)
							.ThenBy(GetOrderIndex)
							.ToList();

						SortedEntries = result;
					}
					else
					{
						SortedEntries = Collection.SharedData.Entries
							.OrderByDescending(entry => entry.Key, stringComparer).ToList();
					}

					return;
			}
		}

		public void SortByAssetTable(AssetTableCollection collection, AssetTable table, bool preserveCurrentOrder)
		{
			switch (CurrentSortOrderState)
			{
				case SortOrderState.Ascending:
					stringComparer.IsAscending = true;

					if (preserveCurrentOrder)
					{
						var result = Collection.SharedData.Entries
							.OrderBy(entry => GetAssetNameFromEntry(entry, table, collection), stringComparer)
							.ThenBy(GetOrderIndex)
							.ToList();

						SortedEntries = result;
					}
					else
					{
						SortedEntries = Collection.SharedData.Entries.OrderBy(
								entry => GetAssetNameFromEntry(entry, table, collection), stringComparer)
							.ToList();
					}

					return;

				case SortOrderState.Descending:
					stringComparer.IsAscending = false;

					if (preserveCurrentOrder)
					{
						var result = Collection.SharedData.Entries
							.OrderByDescending(
								entry => GetAssetNameFromEntry(entry, table, collection),
								stringComparer)
							.ThenBy(GetOrderIndex)
							.ToList();

						SortedEntries = result;
					}
					else
					{
						SortedEntries = Entries.OrderByDescending(
							entry => GetAssetNameFromEntry(entry, table, collection), stringComparer).ToList();
					}

					return;
			}
		}

		public void SortByStringTable(StringTable table, bool preserveCurrentOrder)
		{
			switch (CurrentSortOrderState)
			{
				case SortOrderState.Ascending:
					stringComparer.IsAscending = true;

					if (preserveCurrentOrder)
					{
						var result = Collection.SharedData.Entries
							.OrderBy(entry => GetStringFromEntry(entry, table), stringComparer)
							.ThenBy(GetOrderIndex)
							.ToList();

						SortedEntries = result;
					}
					else
					{
						SortedEntries = Collection.SharedData.Entries.OrderBy(
							entry => GetStringFromEntry(entry, table), stringComparer).ToList();
					}

					return;

				case SortOrderState.Descending:
					stringComparer.IsAscending = false;

					if (preserveCurrentOrder)
					{
						var result = Collection.SharedData.Entries
							.OrderByDescending(entry => GetStringFromEntry(entry, table), stringComparer)
							.ThenBy(GetOrderIndex)
							.ToList();

						SortedEntries = result;
					}
					else
					{
						SortedEntries = Entries.OrderByDescending(
							entry => GetStringFromEntry(entry, table), stringComparer).ToList();
					}

					return;
			}
		}

		private static string GetStringFromEntry(SharedTableData.SharedTableEntry sharedEntry, StringTable table)
		{
			var entry = table.GetEntry(sharedEntry.Id);

			return entry?.Value;
		}

		private static string GetAssetNameFromEntry(
			SharedTableData.SharedTableEntry sharedEntry, AssetTable table, AssetTableCollection collection)
		{
			var entry = table.GetEntry(sharedEntry.Id);

			if (entry == null || entry.IsEmpty) return null;

			var type = collection.GetEntryAssetType(sharedEntry.Id);

			var asset = OdinLocalizationAssetCache.Get(entry.Guid, type);

			return asset == null ? null : asset.name;
		}

		public void MoveEntry(int from, int to)
		{
			if (from < 0 || from >= Entries.Count) return;

			if (to < 0 || to > Entries.Count) return;

			if (from == to) return;

			var fromEntry = Collection.SharedData.Entries[from];

			if (to > from) to -= 1;

			//to = afterTo ? to + 1 : to;

			Collection.SharedData.Entries.RemoveAt(from);

			Collection.SharedData.Entries.Insert(to, fromEntry);

			OdinLocalizationEvents.RaiseTableEntryModified(Collection.SharedData.Entries[from]);
			OdinLocalizationEvents.RaiseTableEntryModified(Collection.SharedData.Entries[to]);

			EditorUtility.SetDirty(Collection.SharedData);
		}

		public void GotoNextSortOrderState()
		{
			switch (CurrentSortOrderState)
			{
				case SortOrderState.Unsorted:
					CurrentSortOrderState = SortOrderState.Ascending;
					break;

				case SortOrderState.Ascending:
					CurrentSortOrderState = SortOrderState.Descending;
					break;

				case SortOrderState.Descending:
					CurrentSortOrderState = SortOrderState.Unsorted;
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public int GetIndex(SharedTableData.SharedTableEntry sharedEntry)
		{
			for (var i = 0; i < Length; i++)
				if (this[i].Id == sharedEntry.Id)
					return i;

			return -1;
		}

		public int GetOrderIndex(SharedTableData.SharedTableEntry sharedEntry)
		{
			if (IsSorted && SortedEntries.Count == Length)
			{
				for (var i = 0; i < SortedEntries.Count; i++)
					if (SortedEntries[i].Id == sharedEntry.Id)
						return i;

				return -1;
			}

			for (var i = 0; i < Length; i++)
				if (this[i].Id == sharedEntry.Id)
					return i;

			return -1;
		}

		private class StringComparer : IComparer<string>
		{
			public bool IsAscending = true;

			public int Compare(string self, string other)
			{
				if (string.IsNullOrEmpty(self) && string.IsNullOrEmpty(other)) return 0;

				if (string.IsNullOrEmpty(self)) return IsAscending ? 1 : -1;

				if (string.IsNullOrEmpty(other)) return IsAscending ? -1 : 1;

				return string.Compare(self, other, StringComparison.InvariantCulture);
			}
		}
	}
}
