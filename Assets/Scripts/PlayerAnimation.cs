
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;

    void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    
    public void SetRunning(bool running)
    {
        if (animator == null)
        {
            Debug.LogError("Animator reference is missing!");
            return;
        }

        animator.SetBool("IsRunning", running);

        Debug.Log(
            "Animator: " + animator.gameObject.name +
            " | Controller: " + animator.runtimeAnimatorController.name +
            " | IsRunning: " + animator.GetBool("IsRunning")
        );
    }

}
