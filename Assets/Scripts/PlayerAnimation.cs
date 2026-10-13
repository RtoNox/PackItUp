
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    public Animator animator;
    public float movementThreshold = 0.1f;

    void Update()
    {
        // Get the player's movement speed
        Vector3 velocity = Vector3.zero;

        CharacterController controller =
            GetComponentInParent<CharacterController>();

        if (controller != null)
        {
            velocity = controller.velocity;
        }

        // Check if the player is moving horizontally
        Vector3 horizontalVelocity =
            new Vector3(velocity.x, 0, velocity.z);

        bool isRunning =
            horizontalVelocity.magnitude > movementThreshold;

        animator.SetBool("IsRunning", isRunning);
    }
}
