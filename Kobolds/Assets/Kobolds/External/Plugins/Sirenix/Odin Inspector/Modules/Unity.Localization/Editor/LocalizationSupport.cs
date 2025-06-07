//-----------------------------------------------------------------------
// <copyright file="LocalizationSupport.cs" company="Sirenix ApS">
// Copyright (c) Sirenix ApS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine.Localization;

#if UNITY_EDITOR

namespace Sirenix.OdinInspector.Modules.Localization.Editor
{
	public class LocalizedReferenceResolver : OdinPropertyResolver<LocalizedReference>
	{
		public override int ChildNameToIndex(string name)
		{
			throw new NotSupportedException();
		}

		public override int ChildNameToIndex(ref StringSlice name)
		{
			throw new NotSupportedException();
		}

		public override InspectorPropertyInfo GetChildInfo(int childIndex)
		{
			throw new NotSupportedException();
		}

		protected override int GetChildCount(LocalizedReference value)
		{
			return 0;
		}
	}
}
#endif
