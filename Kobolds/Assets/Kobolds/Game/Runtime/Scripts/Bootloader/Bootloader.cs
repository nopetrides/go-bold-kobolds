using Kobolds.Runtime.Managers;
using UnityEngine;

namespace Kobolds.Runtime
{
    public class Bootloader : MonoBehaviour
    {
        [SerializeField] private Animator AnimatorComponent;

		[SerializeField] private GlobalsMgr _gameLoader;

		// Called by the animator after finishing the splash intro
        public void LoadIntro()
		{
			Instantiate(_gameLoader);
		}
    }
}
