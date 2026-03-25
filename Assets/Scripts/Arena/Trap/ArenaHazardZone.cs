using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ArenaHazardZone : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private bool affectPlayer = true;
    [SerializeField] private bool affectAllies = true;
    [SerializeField] private bool affectEnemies = true;

    [Header("Move Speed Debuff")]
    [SerializeField] private bool applyMoveSpeedDebuff = true;
    [SerializeField] private float moveSpeedMultiplier = 0.6f;

    [Header("Damage Over Time")]
    [SerializeField] private bool dealPeriodicDamage = false;
    [SerializeField] private float damagePerTick = 5f;
    [SerializeField] private float damageInterval = 1f;

    private Dictionary<Health, float> nextDamageTimeLookup = new Dictionary<Health, float>();
    private List<TemporaryStatEffects> affectedEffects = new List<TemporaryStatEffects>();

    protected virtual void Reset()
    {
        Collider2D col;

        col = GetComponent<Collider2D>();

        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    // Character enters the area of ​​effect
    void OnTriggerEnter2D(Collider2D other)
    {
        TemporaryStatEffects effects;
        Health health;

        if (!CanAffect(other))
        {
            return;
        }

        effects = other.GetComponent<TemporaryStatEffects>();
        health = other.GetComponent<Health>();

        if (applyMoveSpeedDebuff && effects != null)
        {
            effects.AddPersistentMoveSpeedMultiplier(this, moveSpeedMultiplier);

            if (!affectedEffects.Contains(effects))
            {
                affectedEffects.Add(effects);
            }
        }

        if (dealPeriodicDamage && health != null)
        {
            nextDamageTimeLookup[health] = Time.time;
        }
    }

    // Character stays within the area of ​​effect
    void OnTriggerStay2D(Collider2D other)
    {
        Health health;

        if (!dealPeriodicDamage)
        {
            return;
        }

        if (!CanAffect(other))
        {
            return;
        }

        health = other.GetComponent<Health>();

        if (health == null)
        {
            return;
        }

        if (health.GetIsDead())
        {
            return;
        }

        HandleDamageTick(health);
    }

    // Character leaves the area of ​​effect
    void OnTriggerExit2D(Collider2D other)
    {
        TemporaryStatEffects effects;
        Health health;

        effects = other.GetComponent<TemporaryStatEffects>();
        health = other.GetComponent<Health>();

        if (effects != null)
        {
            effects.RemovePersistentMoveSpeedMultiplier(this);
            affectedEffects.Remove(effects);
        }

        if (health != null && nextDamageTimeLookup.ContainsKey(health))
        {
            nextDamageTimeLookup.Remove(health);
        }
    }

    void OnDisable()
    {
        int i;

        for (i = 0; i < affectedEffects.Count; i++)
        {
            if (affectedEffects[i] != null)
            {
                affectedEffects[i].RemovePersistentMoveSpeedMultiplier(this);
            }
        }

        affectedEffects.Clear();
        nextDamageTimeLookup.Clear();
    }

    private bool CanAffect(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() != null && affectPlayer)
        {
            return true;
        }

        if (other.GetComponent<AllyController>() != null && affectAllies)
        {
            return true;
        }

        if (other.GetComponent<EnemyController>() != null && affectEnemies)
        {
            return true;
        }

        return false;
    }

    private void HandleDamageTick(Health health)
    {
        float nextTime;

        if (!nextDamageTimeLookup.TryGetValue(health, out nextTime))
        {
            nextDamageTimeLookup[health] = Time.time;
            nextTime = Time.time;
        }

        if (Time.time < nextTime)
        {
            return;
        }

        health.TakeDamage(damagePerTick);
        nextDamageTimeLookup[health] = Time.time + damageInterval;
    }
}
