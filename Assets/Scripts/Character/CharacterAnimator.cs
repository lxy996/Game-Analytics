using UnityEngine;

public class CharacterAnimator : MonoBehaviour
{
    private Animator animator;
    private CharacterMotor motor;
    //private Health health;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        animator = GetComponent<Animator>();
        motor = GetComponent<CharacterMotor>();
        //health = GetComponent<Health>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        Vector2 moveInput;
        Vector2 facing;
        bool isMoving;

        if (animator == null)
        {
            return;
        }

        moveInput = motor.GetMoveInput();
        facing = motor.GetFacingDirection();

        if (moveInput.sqrMagnitude > 0.01f)
        {
            isMoving = true;
        }
        else
        {
            isMoving = false;
        }

        animator.SetBool("IsMoving", isMoving);
        //animator.SetBool("Dead", health.GetIsDead());

        animator.SetFloat("MoveX", moveInput.x);
        animator.SetFloat("MoveY", moveInput.y);

        animator.SetFloat("FaceX", facing.x);
        animator.SetFloat("FaceY", facing.y);

        UpdateSpriteFacing(facing);
    }

    private void UpdateSpriteFacing(Vector2 facing)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (facing.x > 0.05f)
        {
            spriteRenderer.flipX = false;
        }
        else if (facing.x < -0.05f)
        {
            spriteRenderer.flipX = true;
        }
    }
}
