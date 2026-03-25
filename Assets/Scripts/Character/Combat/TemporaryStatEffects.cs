using System.Collections.Generic;
using UnityEngine;

public class TemporaryStatEffects : MonoBehaviour
{

    // Limited-time effect
    private class TimedEffect
    {
        public StatEffectType effectType;
        public float multiplier;
        public float expireTime;
    }

    private CharacterStats stats;

    // Record the character's limited-time effects
    private List<TimedEffect> timedEffects = new List<TimedEffect>();

    // Unlimited-time effect
    private Dictionary<int, float> persistentMoveSpeedEffects = new Dictionary<int, float>();
    private Dictionary<int, float> persistentAttackSpeedEffects = new Dictionary<int, float>();

    void Awake()
    {
        stats = GetComponent<CharacterStats>();
    }

    void Update()
    {
        UpdateTimedEffects();
    }

    // Continuously monitor each time-limited effect possessed by a character to determine if their duration has expired.
    private void UpdateTimedEffects()
    {
        int i;

        for (i = timedEffects.Count - 1; i >= 0; i--)
        {
            if (Time.time >= timedEffects[i].expireTime)
            {
                RemoveAppliedMultiplier(timedEffects[i].effectType, timedEffects[i].multiplier);
                timedEffects.RemoveAt(i);
            }
        }
    }

    public void AddTimedMultiplier(StatEffectType effectType, float multiplier, float duration)
    {
        TimedEffect effect;

        if (stats == null)
        {
            return;
        }

        if (multiplier <= 0f)
        {
            return;
        }

        effect = new TimedEffect();
        effect.effectType = effectType;
        effect.multiplier = multiplier;
        effect.expireTime = Time.time + duration;

        ApplyMultiplier(effect.effectType, effect.multiplier);
        timedEffects.Add(effect);
    }

    public void AddPersistentMoveSpeedMultiplier(Component source, float multiplier)
    {
        int sourceId;

        if (stats == null || source == null)
        {
            return;
        }

        if (multiplier <= 0f)
        {
            return;
        }

        // Obtain the object's unique identification number to ensure that buffs from the same source are not granted repeatedly.
        sourceId = source.GetInstanceID();

        // Avoid getting the same effect repeatedly
        if (persistentMoveSpeedEffects.ContainsKey(sourceId))
        {
            return;
        }

        persistentMoveSpeedEffects[sourceId] = multiplier;
        ApplyMultiplier(StatEffectType.MoveSpeedMultiplier, multiplier);
    }

    public void RemovePersistentMoveSpeedMultiplier(Component source)
    {
        int sourceId;
        float multiplier;

        if (stats == null || source == null)
        {
            return;
        }

        sourceId = source.GetInstanceID();

        if (!persistentMoveSpeedEffects.TryGetValue(sourceId, out multiplier))
        {
            return;
        }

        RemoveAppliedMultiplier(StatEffectType.MoveSpeedMultiplier, multiplier);
        persistentMoveSpeedEffects.Remove(sourceId);
    }

    public void AddPersistentAttackSpeedMultiplier(Component source, float multiplier)
    {
        int sourceId;

        if (stats == null || source == null)
        {
            return;
        }

        if (multiplier <= 0f)
        {
            return;
        }

        sourceId = source.GetInstanceID();

        if (persistentAttackSpeedEffects.ContainsKey(sourceId))
        {
            return;
        }

        persistentAttackSpeedEffects[sourceId] = multiplier;
        ApplyMultiplier(StatEffectType.AttackSpeedMultiplier, multiplier);
    }

    public void RemovePersistentAttackSpeedMultiplier(Component source)
    {
        int sourceId;
        float multiplier;

        if (stats == null || source == null)
        {
            return;
        }

        sourceId = source.GetInstanceID();

        if (!persistentAttackSpeedEffects.TryGetValue(sourceId, out multiplier))
        {
            return;
        }

        RemoveAppliedMultiplier(StatEffectType.AttackSpeedMultiplier, multiplier);
        persistentAttackSpeedEffects.Remove(sourceId);
    }

    // This function recalculates the effect of buffs after the base value of an attribute changes.
    public void ReapplyActiveEffectsOnCurrentStats()
    {
        int i;
        List<float> movePersistentValues;
        List<float> attackPersistentValues;

        if (stats == null)
        {
            return;
        }

        movePersistentValues = new List<float>(persistentMoveSpeedEffects.Values);
        attackPersistentValues = new List<float>(persistentAttackSpeedEffects.Values);

        for (i = 0; i < movePersistentValues.Count; i++)
        {
            ApplyMultiplier(StatEffectType.MoveSpeedMultiplier, movePersistentValues[i]);
        }

        for (i = 0; i < attackPersistentValues.Count; i++)
        {
            ApplyMultiplier(StatEffectType.AttackSpeedMultiplier, attackPersistentValues[i]);
        }

        for (i = 0; i < timedEffects.Count; i++)
        {
            ApplyMultiplier(timedEffects[i].effectType, timedEffects[i].multiplier);
        }
    }

    private void ApplyMultiplier(StatEffectType effectType, float multiplier)
    {
        if (stats == null)
        {
            return;
        }

        if (effectType == StatEffectType.MoveSpeedMultiplier)
        {
            stats.moveSpeed = stats.moveSpeed * multiplier;
        }
        else if (effectType == StatEffectType.AttackSpeedMultiplier)
        {
            stats.attackSpeedMultiplier = stats.attackSpeedMultiplier * multiplier;
        }
    }

    private void RemoveAppliedMultiplier(StatEffectType effectType, float multiplier)
    {
        if (stats == null)
        {
            return;
        }

        if (multiplier == 0f)
        {
            return;
        }

        if (effectType == StatEffectType.MoveSpeedMultiplier)
        {
            stats.moveSpeed = stats.moveSpeed / multiplier;
        }
        else if (effectType == StatEffectType.AttackSpeedMultiplier)
        {
            stats.attackSpeedMultiplier = stats.attackSpeedMultiplier / multiplier;
        }
    }
}
