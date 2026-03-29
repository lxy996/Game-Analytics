using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerBattleHudUI : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private TMP_Text healthText;

    [Header("Dash")]
    [SerializeField] private Image dashReadyIcon;
    [SerializeField] private Image dashCooldownMask;
    [SerializeField] private TMP_Text dashText;

    [Header("Guard")]
    [SerializeField] private Image guardReadyIcon;
    [SerializeField] private Image guardCooldownMask;
    [SerializeField] private TMP_Text guardText;

    void LateUpdate()
    {
        ResolvePlayerReference();
        RefreshHealth();
        RefreshDash();
        RefreshGuard();
    }

    private void ResolvePlayerReference()
    {
        if (playerController == null)
        {
            playerController = Object.FindFirstObjectByType<PlayerController>();
        }
    }

    private void RefreshHealth()
    {
        Health health;
        CharacterStats stats;
        float current;
        float max;

        if (playerController == null)
        {
            return;
        }

        health = playerController.GetComponent<Health>();
        stats = playerController.GetComponent<CharacterStats>();

        if (health == null || stats == null)
        {
            return;
        }

        current = health.GetCurrentHealth();
        max = stats.GetEffectiveMaxHealth();

        if (healthFillImage != null && max > 0f)
        {
            healthFillImage.fillAmount = current / max;
        }

        if (healthText != null)
        {
            healthText.text = Mathf.CeilToInt(current) + " / " + Mathf.CeilToInt(max);
        }
    }

    private void RefreshDash()
    {
        SkillController skills;
        float normalized;
        bool ready;

        if (playerController == null)
        {
            return;
        }

        skills = playerController.GetComponent<SkillController>();

        if (skills == null)
        {
            return;
        }

        normalized = skills.GetDashCooldownNormalized();
        ready = normalized <= 0f;

        if (dashCooldownMask != null)
        {
            dashCooldownMask.fillAmount = normalized;
        }

        if (dashReadyIcon != null)
        {
            dashReadyIcon.color = ready ? Color.white : new Color(1f, 1f, 1f, 0.45f);
        }

        if (dashText != null)
        {
            dashText.text = ready ? "Dash Ready" : skills.GetDashCooldownRemaining().ToString("0.0");
        }
    }

    private void RefreshGuard()
    {
        CombatController combat;
        float normalized;
        bool ready;

        if (playerController == null)
        {
            return;
        }

        combat = playerController.GetComponent<CombatController>();

        if (combat == null)
        {
            return;
        }

        normalized = combat.GetGuardCooldownNormalized();
        ready = normalized <= 0f;

        if (guardCooldownMask != null)
        {
            guardCooldownMask.fillAmount = normalized;
        }

        if (guardReadyIcon != null)
        {
            guardReadyIcon.color = ready ? Color.white : new Color(1f, 1f, 1f, 0.45f);
        }

        if (guardText != null)
        {
            guardText.text = ready ? "Guard Ready" : combat.GetGuardCooldownRemaining().ToString("0.0");
        }
    }
}
