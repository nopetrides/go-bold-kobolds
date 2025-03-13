using System.Threading.Tasks;
using Kobolds.UI;
using P3T.Scripts.Managers;
using P3T.Scripts.Utils;
using UnityEngine;

namespace Kobolds.Runtime.Managers
{
	/// <summary>
	///     The "don't destroy on load" parent that contains all other global managers
	/// </summary>
	public class GlobalsMgr : Singleton<GlobalsMgr>
	{
		[SerializeField] private string[] DefaultLoadAssetLabels = {"Preload"};

		public override void Awake()
		{
			base.Awake();
			if (Instance == this)
				DontDestroyOnLoad(gameObject);
			
			_ = LoadDefaultAssets();
		}

		private async Task LoadDefaultAssets()
		{
			_ = await P3TAssetLoader.LoadAndSpawnStoredAssetsByLabelAsync(DefaultLoadAssetLabels);

			if (UiMgr.Instance == null)
			{
				Debug.LogError($"Failed to load {nameof(UiMgr)}");
				return;
			}

			if (SceneMgr.Instance == null)
			{
				gameObject.AddComponent<SceneMgr>();
			}
			
			SceneMgr.Instance.LoadScene(GameScenes.LanguageSelect.ToString(), typeof(LanguageSelectMenu));
		}
	}
}