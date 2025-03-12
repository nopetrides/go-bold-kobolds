using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace P3T.Scripts.Managers
{
	/// <summary>
	///     The "don't destroy on load" parent that contains all other global managers
	/// </summary>
	public class GlobalsMgr : Singleton<GlobalsMgr>
	{
		public override void Awake()
		{
			base.Awake();
			if (Instance == this)
				DontDestroyOnLoad(gameObject);

			var loadAssetAsync = Addressables.LoadAssetAsync<GameObject>("MenuUIMgrCanvas");
			loadAssetAsync.Completed += LoadHandle_Completed;
		}
		private void LoadHandle_Completed(AsyncOperationHandle<GameObject> operation)
		{
			if (operation.Status == AsyncOperationStatus.Succeeded)
				Instantiate(operation.Result);
			else 
				Debug.LogWarning("Failed to load MenuUIMgrCanvas");
			
		}
	}
	
	
}