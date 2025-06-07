//-----------------------------------------------------------------------
// <copyright file="OdinLocalizationAssetCache.cs" company="Sirenix ApS">
// Copyright (c) Sirenix ApS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Localization.Tables;
using Object = UnityEngine.Object;

namespace Sirenix.OdinInspector.Modules.Localization.Editor.Internal
{
	public static class OdinLocalizationAssetCache
	{
		private static readonly Dictionary<AssetIdentifier, Object> Assets = new();

		public static Object Get(string guid, Type assetType)
		{
			if (string.IsNullOrEmpty(guid)) return null;

			var identifier = new AssetIdentifier(assetType, guid);

			if (Assets.TryGetValue(identifier, out var result)) return result;

			var path = AssetDatabase.GUIDToAssetPath(guid);

			result = AssetDatabase.LoadAssetAtPath(path, assetType);

			Assets.Add(identifier, result);

			return result;
		}

		public static Object Get(SharedTableData.SharedTableEntry sharedEntry, AssetTable assetTable, Type assetType)
		{
			var entry = assetTable.GetEntry(sharedEntry.Id);

			if (entry == null || entry.IsEmpty) return null;

			return Get(entry.Guid, assetType);
		}

		public static void Clear()
		{
			Assets.Clear();
		}

		private readonly struct AssetIdentifier
		{
			public readonly Type AssetType;
			public readonly string Guid;

			public AssetIdentifier(Type assetType, string guid)
			{
				AssetType = assetType;
				Guid = guid;
			}
		}
	}
}
