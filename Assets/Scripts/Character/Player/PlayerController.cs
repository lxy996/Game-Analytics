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

        HandleMovement();
        HandleCombat();
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
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J))
        {
            combat.BasicAttack();
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
