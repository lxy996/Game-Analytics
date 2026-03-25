using UnityEngine;

public class SkillController : MonoBehaviour
{
    [SerializeField] private float dashDistance = 5f;
    [SerializeField] private float dashCooldown = 3f;

    private CharacterMotor motor;
    private Animator animator;
    private float lastDashTime = -999f;

    void Awake()
    {
        motor = GetComponent<CharacterMotor>();
        animator = GetComponent<Animator>();
    }

    public bool CanUseDash()
    {
        if (Time.time >= lastDashTime + dashCooldown)
        {
            return true;
        }

        return false;
    }

    public void UseDash()
    {
        Vector2 dashDir;
        Vector3 moveAmount;

        if (!CanUseDash())
        {
            return;
        }

        lastDashTime = Time.time;

        if (animator != null)
        {
            animator.SetTrigger("Dash");
        }

        dashDir = motor.GetFacingDirection().normalized;
        moveAmount = new Vector3(dashDir.x, dashDir.y, 0f) * dashDistance;
        transform.position = transform.position + moveAmount;
    }
}
