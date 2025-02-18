using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Scripts
{
    public class AnimPlayer : MonoBehaviour
    {
        // variable for name of animation to trigger
        [SerializeField]
        private string _animation;
        // animator reference
        [SerializeField]
        private Animator _animator;

        // Trigger methood
        public void TriggerAnimation()
        {
            _animator.SetTrigger(_animation);
        }
    }
}
