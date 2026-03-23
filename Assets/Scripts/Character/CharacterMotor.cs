using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CharacterMotor : MonoBehaviour
{
    private Rigidbody2D rb;
    private CharacterStats stats;
    private Vector2 moveInput;
    private Vector2 facingDirection = Vector2.right;
    private bool movementLocked = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<CharacterStats>();
    }

    public void SetMoveInput(Vector2 input)
    {
        if (movementLocked)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = input.normalized;

        if (moveInput.sqrMagnitude > 0.01f)
        {
            facingDirection = moveInput;
        }
    }

    public Vector2 GetMoveInput()
    {
        return moveInput;
    }

    public Vector2 GetFacingDirection()
    {
        return facingDirection;
    }

    public void SetFacingDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        facingDirection = direction.normalized;
    }

    public void SetMovementLocked(bool value)
    {
        movementLocked = value;

        if (movementLocked)
        {
            moveInput = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
        }
    }
    public bool GetMovementLocked()
    {
        return movementLocked;
    }

    void FixedUpdate()
    {
        if (movementLocked)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        rb.linearVelocity = moveInput * stats.moveSpeed;
    }
}
