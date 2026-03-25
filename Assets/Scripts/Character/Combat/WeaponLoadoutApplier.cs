using UnityEngine;

public class WeaponLoadoutApplier : MonoBehaviour
{
    [SerializeField] private WeaponLoadoutData currentLoadout;

    private CharacterStats stats;
    private CombatController combat;
    private EnemyController enemyController;
    private AllyController allyController;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private TemporaryStatEffects temporaryStatEffects;

    void Awake()
    {
        stats = GetComponent<CharacterStats>();
        combat = GetComponent<CombatController>();
        enemyController = GetComponent<EnemyController>();
        allyController = GetComponent<AllyController>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        temporaryStatEffects = GetComponent<TemporaryStatEffects>();

        ApplyCurrentLoadout();
    }

    public void ApplyCurrentLoadout()
    {
        if (currentLoadout == null)
        {
            return;
        }

        if (stats != null)
        {
            stats.ApplyWeaponLoadout(currentLoadout);
        }

        if (combat != null)
        {
            combat.ApplyWeaponLoadout(currentLoadout);
        }

        if (enemyController != null)
        {
            enemyController.ApplyWeaponLoadout(currentLoadout);
        }

        if (allyController != null)
        {
            allyController.ApplyWeaponLoadout(currentLoadout);
        }

        if (animator != null && currentLoadout.animatorController != null)
        {
            animator.runtimeAnimatorController = currentLoadout.animatorController;
        }

        if (spriteRenderer != null && currentLoadout.idleSprite != null)
        {
            spriteRenderer.sprite = currentLoadout.idleSprite;
        }

        if (temporaryStatEffects != null)
        {
            temporaryStatEffects.ReapplyActiveEffectsOnCurrentStats();
        }
    }

    public void SetLoadout(WeaponLoadoutData newLoadout)
    {
        currentLoadout = newLoadout;
        ApplyCurrentLoadout();
    }

    public WeaponLoadoutData GetCurrentLoadout()
    {
        return currentLoadout;
    }
}
