using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace P3T.Scripts.Managers
{
    /// <summary>
    ///     Manages the current scene and scene transitions
    ///     Ensures the UI clears and opens to the correct menu after loading is complete
    /// </summary>
    public class SceneMgr : Singleton<SceneMgr>
	{
		public void LoadScene(GameScenes sceneToLoad, Type menuToOpen)
		{
			StartCoroutine(PerformLoadSequence(sceneToLoad, menuToOpen));
		}

		private IEnumerator PerformLoadSequence(GameScenes sceneToLoad, Type menuToOpen)
		{
			var waiting = true;

			UiMgr.Instance.CloseAllMenus();

			UiMgr.Instance.ShowMenu(typeof(ScreenFadeOverlay), () => waiting = false);

			yield return new WaitWhile(() => waiting);

			var asyncOperation = SceneManager.LoadSceneAsync(sceneToLoad.ToString());

			while (asyncOperation is { isDone: false }) yield return null;

			UiMgr.Instance.HideMenu(typeof(ScreenFadeOverlay));

			UiMgr.Instance.ShowMenu(menuToOpen);
		}
	}
}