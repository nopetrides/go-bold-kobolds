using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace P3T.Scripts.Managers
{
    /// <summary>
    ///     The UI manager for showing various menus and state
    /// </summary>
    public class UiMgr : Singleton<UiMgr>
	{
		[Header("Timing and sorting")] [SerializeField]
		private float FadeInDuration = 0.5f;

		[SerializeField] private float FadeOutDuration = 0.5f;
		[SerializeField] private int SortGap = 10;

		private readonly Stack<MenuBase> _activeMenus = new();
		private readonly Dictionary<Type, MenuBase> _disabledMenus = new();

		private readonly Dictionary<Type, MenuBase> _menuInstances = new();

		public bool TryGetMenu<T>(out T menu) where T : MenuBase
		{
			Type menuType = typeof(T);
			if (_menuInstances.TryGetValue(menuType, out MenuBase baseMenu))
			{
				menu = baseMenu as T;
				return true;
			}
			menu = null;
			return false;
		}

		public void LoadAndRegisterMenus()
		{
			AsyncOperationHandle<IList<MenuBase>> loadHandle = Addressables.LoadAssetsAsync<MenuBase>(
				"menu",
				addressable =>
				{
					if (addressable != null)
					{
						Instantiate(addressable, transform);
					}
				}, 
					Addressables.MergeMode.Union, // How to combine multiple labels
				false); // Whether to fail if any asset fails to load
			loadHandle.Completed += LoadHandle_Completed;
			
			
		}

		private void LoadMenusFromResources()
		{
			var menus = Resources.LoadAll<MenuBase>("/Menu");
			foreach (var menu in menus)
			{
				var menuType = menu?.GetType();
				if(_menuInstances.TryAdd(menuType, menu))
				{
					Debug.Log($"[UIMgr] Registered menu {menuType?.Name} in object {menu?.name}");
				}
				else
				{
					Debug.LogError($"[UIMgr] Failed to register menu {menuType?.Name} in object {menu?.name}");
				}
			}
		}
		
		private void LoadHandle_Completed(AsyncOperationHandle<IList<MenuBase>> operation)
		{
			if (operation.Status != AsyncOperationStatus.Succeeded)
				Debug.LogWarning("Some assets did not load.");
		}


        /// <summary>
        ///     Clear the stack and close all menus
        /// </summary>
        public void CloseAllMenus()
		{
			while (_activeMenus.Count > 0)
			{
				var menu = _activeMenus.Pop();
				menu.PerformFullFadeOut(FadeOutDuration);
				_disabledMenus.Add(menu.GetType(), menu);
			}
		}

        /// <summary>
        ///     Show a menu by adding it to the stack
        /// </summary>
        /// <param name="menuToOpen"></param>
        /// <param name="onMenuOpenComplete"></param>
        /// <param name="fadeIn"></param>
        /// <returns></returns>
        public MenuBase ShowMenu(Type menuToOpen, System.Action onMenuOpenComplete = null, bool fadeIn = true)
		{
			var menu = PushMenu(menuToOpen);
			if (menu == null) return null;
			if (fadeIn)
				menu.PerformFullFadeIn(FadeInDuration, onMenuOpenComplete);
			else
				onMenuOpenComplete?.Invoke();
			return menu;
		}

        /// <summary>
        ///     Half fade the screen when long processing happens
        ///     Usually only needed if contacting the internet
        /// </summary>
        /// <param name="onComplete"></param>
        /// <returns></returns>
        public MenuBase ShowHalfFader(System.Action onComplete)
		{
			var menu = ShowMenu(typeof(ScreenFadeOverlay), fadeIn: false);
			if (menu is ScreenFadeOverlay screenFadeOverlay)
				screenFadeOverlay.PerformHalfFadeIn(FadeInDuration, onComplete);

			return menu;
		}

        /// <summary>
        ///     Internal function.
        ///     Pushes the given menu to the stack
        /// </summary>
        /// <param name="menu"></param>
        /// <returns></returns>
        private MenuBase PushMenu(Type menu)
		{
			// Check if object already exists
			if (!_menuInstances.ContainsKey(menu.GetType()))
			{
				// instantiate the game object
				MenuBase prefab = null; //GetMenuPrefabFromType(menu);
				if (prefab == null)
					return null;
				
				var createdMenu = Instantiate(prefab, transform);
				createdMenu.OnInstantiate();
				_menuInstances.Add(menu, createdMenu);
			}

			var uiObj = _menuInstances[menu];

			if (_activeMenus.Contains(uiObj))
			{
				UnityEngine.Debug.LogError($"Already opened menu {menu}");
				return uiObj;
			}

			if (_disabledMenus.ContainsKey(menu)) _disabledMenus.Remove(menu);

			int sortOverride;

			if (_activeMenus.TryPeek(out var currentTop))
				sortOverride = currentTop.SortOrder + SortGap;
			else
				sortOverride = 0;

			uiObj.SortOrder = sortOverride;

			uiObj.PerformFullFadeIn(FadeInDuration);
			_activeMenus.Push(uiObj);

			return uiObj;
		}

        /// <summary>
        ///     Hide a given menu
        /// </summary>
        /// <param name="menuToClose"></param>
        /// <param name="onMenuFullyHidden"></param>
        /// <param name="fadeOut"></param>
        public void HideMenu(Type menuToClose, System.Action onMenuFullyHidden = null, bool fadeOut = true)
		{
			var menu = PopMenu(menuToClose);
			if (menu == null)
				return;

			if (fadeOut)
				menu.PerformFullFadeOut(FadeOutDuration, onMenuFullyHidden);
			else
				onMenuFullyHidden?.Invoke();
		}

        /// <summary>
        ///     Internal function.
        ///     Removes a menu from the stack
        /// </summary>
        /// <param name="menu"></param>
        /// <returns></returns>
        private MenuBase PopMenu(Type menu)
		{
			if (!_menuInstances.TryGetValue(menu, out var uiObj))
			{
				UnityEngine.Debug.LogError($"Menu {menu} was never created");
				return null;
			}

			if (_activeMenus.TryPeek(out var peekedUI))
				if (peekedUI != uiObj)
				{
					UnityEngine.Debug.LogError(
						$"The top of the stack {peekedUI.name} wasn't the object we wanted to hide {uiObj.name}");
					return null;
				}

			if (_activeMenus.TryPop(out var poppedUI))
				if (!_disabledMenus.TryAdd(menu, poppedUI))
					UnityEngine.Debug.LogError(
						$"Failed to add {menu} to the disabled menus list. Was it already marked as disabled?");

			return poppedUI;
		}

		/*
        /// <summary>
        ///     Gets a prefab by the passed <see cref="GameMenus" /> type
        ///     TODO there are more efficient ways to do this
        /// </summary>
        /// <param name="menuType"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        private MenuBase GetMenuPrefabFromType(GameMenus menuType)
		{
			if (menuType == GameMenus.None)
				return null;
			MenuBase menu;
			switch (menuType)
			{
				case GameMenus.Fader:
					menu = ScreenFaderPrefab;
					break;
				case GameMenus.Splash:
					menu = SplashMenuPrefab;
					break;
				case GameMenus.MainMenu:
					menu = MainMenuPrefab;
					break;
				case GameMenus.SettingsMenu:
					menu = SettingsMenuPrefab;
					break;
				case GameMenus.InGameUI:
					menu = InGameUIPrefab;
					break;
				case GameMenus.GameOverMenu:
					menu = GameOverMenuPrefab;
					break;
				default:
					throw new System.ArgumentOutOfRangeException(nameof(menuType), menuType, null);
			}

			if (menu == null) UnityEngine.Debug.LogError($"Failed to find prefab for {menuType}");

			return menu;
		}
		*/
	}
}