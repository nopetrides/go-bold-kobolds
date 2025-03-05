using UnityEngine;

namespace P3T.Scripts.AnimatedIntro
{
    public class SetAnimationOnAwake : MonoBehaviour
    {
        [SerializeField] private Animator Ac;
        [SerializeField] private string AnimationName;

        private void Awake()
        {
            if (Ac != null)
                Ac.Play(AnimationName, -1, normalizedTime:Random.value);
        }
    }
}
