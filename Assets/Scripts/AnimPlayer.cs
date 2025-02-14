using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimPlayer : MonoBehaviour
{
    // variable for name of animation to trigger
    [SerializeField]
    private string _animation;
    // animator reference
    private Animator _animator;

    // Check for animator component
    private bool AnimatorCheck()
    {
        // return true if present
        if (GetComponent<Animator>() != null)
        {
            return true;
        }
        // return false if not
        else { return false; }
    }
    // Trigger methood
    public void TriggerAnimation()
    {
        // Checks for Animator
        if(AnimatorCheck())
        {
            _animator.SetTrigger(_animation);
        }
    }

}
