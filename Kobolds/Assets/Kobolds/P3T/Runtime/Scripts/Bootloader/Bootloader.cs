using UnityEngine;
using UnityEngine.SceneManagement;

namespace P3T.Scripts.Bootloader
{
    public class Bootloader : MonoBehaviour
    {
        [SerializeField] private Animator AnimatorComponent;

        public void LoadIntro()
        {
            SceneManager.LoadScene("P3T/Scenes/AnimatedScene");
        }

    }
}
