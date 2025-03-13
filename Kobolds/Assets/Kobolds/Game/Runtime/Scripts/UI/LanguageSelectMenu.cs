using Kobolds.Runtime;
using P3T.Scripts.Managers;
using UnityEngine;

namespace Kobolds.UI
{
	public class LanguageSelectMenu : MonoBehaviour
	{
		public void ButtonContinue()
		{
			SceneMgr.Instance.LoadScene(GameScenes.AnimatedIntro.ToString(), null);
		}
	}
}