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
    private ArenaVisualIdentity visualIdentity;

    void Awake()
    {
        stats = GetComponent<CharacterStats>();
        combat = GetComponent<CombatController>();
        enemyController = GetComponent<EnemyController>();
        allyController = GetComponent<AllyController>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        temporaryStatEffects = GetComponent<TemporaryStatEffects>();
        visualIdentity = GetComponent<ArenaVisualIdentity>();

        ApplyCurrentLoadout();
    }

    public void ApplyCurrentLoadout()
    {
        RuntimeAnimatorController controllerToUse;
        Sprite spriteToUse;
        TeamVisualColor teamColor;

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

        teamColor = GetCurrentTeamColor();
        controllerToUse = currentLoadout.GetAnimatorControllerForTeam(teamColor);
        spriteToUse = currentLoadout.GetIdleSpriteForTeam(teamColor);

        if (animator != null && controllerToUse != null)
        {
            animator.runtimeAnimatorController = controllerToUse;
        }

        if (spriteRenderer != null && spriteToUse != null)
        {
            spriteRenderer.sprite = spriteToUse;
        }

        if (temporaryStatEffects != null)
        {
            temporaryStatEffects.ReapplyActiveEffectsOnCurrentStats();
        }
    }
    private TeamVisualColor GetCurrentTeamColor()
    {
        if (visualIdentity != null)
        {
            return visualIdentity.GetTeamColor();
        }

        return TeamVisualColor.Blue;
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
