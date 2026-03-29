using UnityEngine;
using System.Collections;

public class SkillController : MonoBehaviour
{
    [SerializeField] private float dashDistance = 5f;
    [SerializeField] private float dashDuration = 0.09f;
    [SerializeField] private float dashCooldown = 3f;
    [SerializeField] private GameObject dashVfxPrefab;

    private CharacterMotor motor;
    private Animator animator;
    private Rigidbody2D rb;
    private float lastDashTime = -999f;
    private bool isDashing = false;

    void Awake()
    {
        motor = GetComponent<CharacterMotor>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    public bool CanUseDash()
    {
        if (isDashing)
        {
            return false;
        }

        if (Time.time >= lastDashTime + dashCooldown)
        {
            return true;
        }

        return false;
    }

    public void UseDash()
    {
        if (!CanUseDash())
        {
            return;
        }

        StartCoroutine(DashRoutine());
    }

    private IEnumerator DashRoutine()
    {
        Vector2 dashDir;
        Vector3 startPos;
        Vector3 endPos;
        float timer;
        float t;

        lastDashTime = Time.time;
        isDashing = true;

        if (animator != null)
        {
            animator.SetTrigger("Dash");
        }

        if (motor != null)
        {
            motor.SetMovementLocked(true);
        }

        dashDir = motor.GetFacingDirection().normalized;

        if (dashDir.sqrMagnitude < 0.01f)
        {
            dashDir = Vector2.right;
        }

        startPos = transform.position;
        endPos = startPos + new Vector3(dashDir.x, dashDir.y, 0f) * dashDistance;

        if (dashVfxPrefab != null)
        {
            Instantiate(dashVfxPrefab, startPos, Quaternion.identity);
        }

        timer = 0f;

        while (timer < dashDuration)
        {
            timer = timer + Time.deltaTime;
            t = Mathf.Clamp01(timer / dashDuration);
            Vector2 nextPos = Vector2.Lerp(startPos, endPos, t);

            rb.MovePosition(nextPos);
            yield return null;
        }


        if (dashVfxPrefab != null)
        {
            Instantiate(dashVfxPrefab, endPos, Quaternion.identity);
        }

        if (motor != null)
        {
            motor.SetMovementLocked(false);
        }

        isDashing = false;
    }

    public float GetDashCooldownRemaining()
    {
        float remain;

        remain = (lastDashTime + dashCooldown) - Time.time;

        if (remain < 0f)
        {
            remain = 0f;
        }

        return remain;
    }

    public float GetDashCooldownNormalized()
    {
        if (dashCooldown <= 0f)
        {
            return 0f;
        }

        return GetDashCooldownRemaining() / dashCooldown;
    }
}
