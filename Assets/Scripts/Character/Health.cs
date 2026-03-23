using System;
using System.Collections;
using UnityEngine;

public class Health : MonoBehaviour
{
    public event Action<float, float> OnHealthChanged;
    public event Action OnDied;

    [SerializeField] private float hurtFlashDuration = 0.08f;
    [SerializeField] private float deathFadeDuration = 0.8f;
    [SerializeField] private Color hurtColor = new Color(1f, 0.4f, 0.4f, 1f);
    [SerializeField] private Color deathFlashColor = new Color(1f, 0.4f, 0.4f, 1f);

    private CharacterStats stats;
    [SerializeField] private float currentHealth;
    [SerializeField] private bool isDead;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine hurtFlashCoroutine;

    void Awake()
    {
        stats = GetComponent<CharacterStats>();
        currentHealth = stats.maxHealth;
        isDead = false;

        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    // Used to test hurt and dead effect
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            TakeDamage(10f);
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            TakeDamage(999f);
        }
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    public bool GetIsDead()
    {
        return isDead;
    }


    public void TakeDamage(float amount)
    {
        CombatController combat;
        bool blocked;

        if (isDead)
        {
            return;
        }

        combat = GetComponent<CombatController>();

        if (combat != null)
        {
            blocked = combat.TryBlockHit();

            if (blocked)
            {
                return;
            }
        }
        currentHealth = currentHealth - amount;

        if (currentHealth < 0)
        {
            currentHealth = 0;
        }

        if (OnHealthChanged != null)
        {
            OnHealthChanged(currentHealth, stats.maxHealth);
        }

        PlayHurtFlash();

        if (currentHealth == 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (isDead)
        {
            return;
        }

        currentHealth = currentHealth + amount;

        if (currentHealth > stats.maxHealth)
        {
            currentHealth = stats.maxHealth;
        }

        if (OnHealthChanged != null)
        {
            OnHealthChanged(currentHealth, stats.maxHealth);
        }
    }

    private void PlayHurtFlash()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (hurtFlashCoroutine != null)
        {
            StopCoroutine(hurtFlashCoroutine);
        }

        hurtFlashCoroutine = StartCoroutine(HurtFlashRoutine());
    }
    private IEnumerator HurtFlashRoutine()
    {
        spriteRenderer.color = hurtColor;
        yield return new WaitForSeconds(hurtFlashDuration);

        if (!isDead)
        {
            spriteRenderer.color = originalColor;
        }
    }
    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        if (OnDied != null)
        {
            OnDied();
        }

        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        float timer;
        float t;
        Color startColor;
        Color fadeColor;

        if (spriteRenderer == null)
        {
            Destroy(gameObject);
            yield break;
        }

        spriteRenderer.color = deathFlashColor;

        yield return new WaitForSeconds(hurtFlashDuration);

        startColor = spriteRenderer.color;
        timer = 0f;

        while (timer < deathFadeDuration)
        {
            timer = timer + Time.deltaTime;
            t = timer / deathFadeDuration;

            fadeColor = startColor;
            fadeColor.a = Mathf.Lerp(1f, 0f, t);

            spriteRenderer.color = fadeColor;

            yield return null;
        }

        Destroy(gameObject);

    }
}

