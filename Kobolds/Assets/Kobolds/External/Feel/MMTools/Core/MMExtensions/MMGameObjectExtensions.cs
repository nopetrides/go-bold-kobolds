using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoreMountains.Tools
{
	/// <summary>
	///     Game object extensions
	/// </summary>
	public static class GameObjectExtensions
	{
		private static readonly List<Component> m_ComponentCache = new();

		/// <summary>
		///     Grabs a component without allocating memory uselessly
		/// </summary>
		/// <param name="this"></param>
		/// <param name="componentType"></param>
		/// <returns></returns>
		public static Component MMGetComponentNoAlloc(this GameObject @this, Type componentType)
		{
			@this.GetComponents(componentType, m_ComponentCache);
			var component = m_ComponentCache.Count > 0 ? m_ComponentCache[0] : null;
			m_ComponentCache.Clear();
			return component;
		}

		/// <summary>
		///     Grabs a component without allocating memory uselessly
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="this"></param>
		/// <returns></returns>
		public static T MMGetComponentNoAlloc<T>(this GameObject @this) where T : Component
		{
			@this.GetComponents(typeof(T), m_ComponentCache);
			var component = m_ComponentCache.Count > 0 ? m_ComponentCache[0] : null;
			m_ComponentCache.Clear();
			return component as T;
		}

		/// <summary>
		///     Grabs a component on the object, or on its children objects, or on a parent, or adds it to the object if none were
		///     found
		/// </summary>
		/// <param name="this"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static T MMGetComponentAroundOrAdd<T>(this GameObject @this) where T : Component
		{
			var component = @this.GetComponentInChildren<T>(true);
			if (component == null) component = @this.GetComponentInParent<T>();
			if (component == null) component = @this.AddComponent<T>();
			return component;
		}

		/// <summary>
		///     Gets the specified component on the object, or adds it and returns it if there isn't already one
		/// </summary>
		/// <param name="gameObject"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static T MMGetOrAddComponent<T>(this GameObject @this) where T : Component
		{
			var component = @this.GetComponent<T>();
			if (component == null) component = @this.AddComponent<T>();
			return component;
		}

		/// <summary>
		///     Gets the specified component on the object, or adds it and returns it if there isn't already one
		/// </summary>
		/// <param name="gameObject"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static (T newComponent, bool createdNew) MMFindOrCreateObjectOfType<T>(
			this GameObject @this, string newObjectName, Transform parent, bool forceNewCreation = false)
			where T : Component
		{
			var searchedObject = (T) Object.FindAnyObjectByType(typeof(T));
			if (searchedObject == null || forceNewCreation)
			{
				var newGo = new GameObject(newObjectName);
				newGo.transform.SetParent(parent);
				return (newGo.AddComponent<T>(), true);
			}

			return (searchedObject, false);
		}
	}
}
