using UnityEngine;

public class TimedStatPickup : ArenaPickup
{


    [SerializeField] private StatEffectType statType = StatEffectType.MoveSpeedMultiplier;
    [SerializeField] private float multiplier = 1.5f;
    [SerializeField] private float duration = 5f;

    protected override bool TryApplyPickup(Collider2D other)
    {
        TemporaryStatEffects effects;

        effects = other.GetComponent<TemporaryStatEffects>();

        if (effects == null)
        {
            return false;
        }

        effects.AddTimedMultiplier(statType, multiplier, duration);
        return true;
    }
}
