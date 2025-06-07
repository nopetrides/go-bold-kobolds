//-----------------------------------------------------------------------
// <copyright file="OdinLocalizationMetadataRegistry.cs" company="Sirenix ApS">
// Copyright (c) Sirenix ApS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine.Localization.Metadata;

namespace Sirenix.OdinInspector.Modules.Localization.Editor.Internal
{
	public class OdinLocalizationMetadataRegistry
	{
		public static readonly List<Type> AssetEntryMetadataTypes = new();

		public static readonly List<Type> StringEntryMetadataTypes = new();

		public static readonly Dictionary<Type, bool> MetadataAllowsMultiple = new();

		static OdinLocalizationMetadataRegistry()
		{
			var metadataTypes = TypeCache.GetTypesDerivedFrom(typeof(IMetadata));

			for (var i = 0; i < metadataTypes.Count; i++)
			{
				var currentType = metadataTypes[i];

				var attr = currentType.GetCustomAttribute<MetadataAttribute>();

				if (attr is null)
				{
					MetadataAllowsMultiple[currentType] = true;
					continue;
				}

				MetadataAllowsMultiple[currentType] = attr.AllowMultiple;

				var currentAllowedTypes = attr.AllowedTypes;

				if (currentAllowedTypes.HasFlag(MetadataType.StringTableEntry))
					StringEntryMetadataTypes.Add(currentType);

				if (currentAllowedTypes.HasFlag(MetadataType.AssetTableEntry)) AssetEntryMetadataTypes.Add(currentType);
			}
		}
	}
}
