using UnityEngine;

[RequireComponent(typeof(CharacterMotor))]
[RequireComponent(typeof(CombatController))]
[RequireComponent(typeof(SkillController))]
[RequireComponent(typeof(Health))]
public class PlayerController : MonoBehaviour
{
    private CharacterMotor motor;
    private CombatController combat;
    private SkillController skills;
    private Health health;

    void Awake()
    {
        motor = GetComponent<CharacterMotor>();
        combat = GetComponent<CombatController>();
        skills = GetComponent<SkillController>();
        health = GetComponent<Health>();
    }

    void Update()
    {
        if (health.GetIsDead())
        {
            motor.SetMoveInput(Vector2.zero);
            return;
        }

        HandleCombat();
        HandleMovement();
        HandleSkills();
    }

    private void HandleMovement()
    {
        float x;
        float y;
        Vector2 move;

        x = Input.GetAxisRaw("Horizontal");
        y = Input.GetAxisRaw("Vertical");
        move = new Vector2(x, y);

        motor.SetMoveInput(move);
    }

    private void HandleCombat()
    {
        Vector3 mouseWorld;
        Vector2 attackDirection;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J))
        {
            if (combat != null)
            {
                // Use mouse to control the ranged weapon's attack direction
                if (GetComponent<CharacterStats>().weaponType == WeaponType.Ranged)
                {
                    mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    attackDirection = new Vector2(
                        mouseWorld.x - transform.position.x,
                        mouseWorld.y - transform.position.y
                    );

                    combat.SetOverrideAttackDirection(attackDirection);
                }
                else
                {
                    combat.ClearOverrideAttackDirection();
                }
            }

            combat.BasicAttack();
        }

        if (Input.GetMouseButtonDown(1))
        {
            combat.StartGuard();
        }

        if (Input.GetMouseButtonUp(1))
        {
            combat.EndGuard();
        }
    }

    private void HandleSkills()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            skills.UseDash();
        }
    }
}
