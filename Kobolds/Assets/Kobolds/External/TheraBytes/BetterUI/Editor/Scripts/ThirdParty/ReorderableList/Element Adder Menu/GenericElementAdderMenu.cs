// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using UnityEditor;
using UnityEngine;

namespace TheraBytes.BetterUi.Editor.ThirdParty
{
	internal sealed class GenericElementAdderMenu : IElementAdderMenu
	{
		private readonly GenericMenu _innerMenu = new();

		public bool IsEmpty => _innerMenu.GetItemCount() == 0;

		public void DropDown(Rect position)
		{
			_innerMenu.DropDown(position);
		}

		public void AddItem(GUIContent content, GenericMenu.MenuFunction handler)
		{
			_innerMenu.AddItem(content, false, handler);
		}

		public void AddDisabledItem(GUIContent content)
		{
			_innerMenu.AddDisabledItem(content);
		}

		public void AddSeparator(string path = "")
		{
			_innerMenu.AddSeparator(path);
		}
	}
}
