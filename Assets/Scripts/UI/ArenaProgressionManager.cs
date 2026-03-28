using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.GPUSort;

public class ArenaProgressionManager : MonoBehaviour
{
    private class ProficiencyState
    {
        public int level = 1;
        public float currentExp = 0f;
    }

    private Dictionary<GladiatorProfileData, Dictionary<GladiatorProficiencyType, ProficiencyState>> runtimeData =
        new Dictionary<GladiatorProfileData, Dictionary<GladiatorProficiencyType, ProficiencyState>>();

    [Header("Level Rules")]
    [SerializeField] private int maxLevel = 300;

    public void EnsureProfileInitialized(GladiatorProfileData profile)
    {
        Array types;
        int i;
        Dictionary<GladiatorProficiencyType, ProficiencyState> typeMap;

        if (profile == null)
        {
            return;
        }

        if (runtimeData.ContainsKey(profile))
        {
            return;
        }

        typeMap = new Dictionary<GladiatorProficiencyType, ProficiencyState>();
        types = Enum.GetValues(typeof(GladiatorProficiencyType));

        for (i = 0; i < types.Length; i++)
        {
            GladiatorProficiencyType type;
            ProficiencyState state;
            int baseLevel;

            type = (GladiatorProficiencyType)types.GetValue(i);
            state = new ProficiencyState();

            baseLevel = profile.GetProficiencyLevel(type);

            if (baseLevel > 0)
            {
                state.level = Mathf.Clamp(baseLevel, 1, maxLevel);
            }

            typeMap[type] = state;
        }

        runtimeData[profile] = typeMap;
    }

    public int GetLevel(GladiatorProfileData profile, GladiatorProficiencyType type)
    {
        ProficiencyState state;

        state = GetState(profile, type);

        if (state == null)
        {
            return 1;
        }

        return state.level;
    }

    public float GetCurrentExp(GladiatorProfileData profile, GladiatorProficiencyType type)
    {
        ProficiencyState state;

        state = GetState(profile, type);

        if (state == null)
        {
            return 0f;
        }

        return state.currentExp;
    }

    public float GetExpRequiredForNextLevel(GladiatorProfileData profile, GladiatorProficiencyType type)
    {
        int level;

        level = GetLevel(profile, type);
        return CalculateRequiredExp(level);
    }

    public float GetCurrentExperienceMultiplier(GladiatorProfileData profile, GladiatorProficiencyType type)
    {
        int level;
        float t;

        level = GetLevel(profile, type);
        t = Mathf.InverseLerp(1f, maxLevel, level);

        // The experience multiplier is higher in lower levels, gradually decreasing to 1.0 in higher levels.
        return Mathf.Lerp(3.0f, 1.0f, t);
    }

    public void AddExperience(GladiatorProfileData profile, GladiatorProficiencyType type, float baseExp)
    {
        ProficiencyState state;
        float gainedExp;
        float requiredExp;

        state = GetState(profile, type);

        if (state == null)
        {
            return;
        }

        if (state.level >= maxLevel)
        {
            state.level = maxLevel;
            state.currentExp = 0f;
            return;
        }

        gainedExp = baseExp * GetCurrentExperienceMultiplier(profile, type);
        state.currentExp = state.currentExp + gainedExp;

        while (state.level < maxLevel)
        {
            requiredExp = CalculateRequiredExp(state.level);

            if (state.currentExp < requiredExp)
            {
                break;
            }

            state.currentExp = state.currentExp - requiredExp;
            state.level = state.level + 1;
        }

        if (state.level >= maxLevel)
        {
            state.level = maxLevel;
            state.currentExp = 0f;
        }
    }

    public void AwardPostMatchExperience(
        List<ArenaMatchFighterEntry> playerRoster,
        bool playerWon,
        int playerTeamKills
    )
    {
        int i;
        float baseWeaponExp;
        float runningExp;
        float tacticsExp;
        float shieldExp;

        if (playerRoster == null)
        {
            return;
        }

        for (i = 0; i < playerRoster.Count; i++)
        {
            GladiatorProficiencyType weaponTypeLine;
            GladiatorProfileData profile;
            WeaponLoadoutData loadout;

            if (playerRoster[i] == null)
            {
                continue;
            }

            profile = playerRoster[i].gladiatorProfile;
            loadout = playerRoster[i].selectedLoadout;

            if (profile == null || loadout == null)
            {
                continue;
            }

            EnsureProfileInitialized(profile);

            baseWeaponExp = 400f + playerTeamKills * 120f;
            runningExp = 180f + playerTeamKills * 40f;
            tacticsExp = 120f;
            shieldExp = 0f;

            if (playerWon)
            {
                baseWeaponExp = baseWeaponExp + 500f;
                runningExp = runningExp + 150f;
                tacticsExp = tacticsExp + 250f;
            }

            weaponTypeLine = MapLoadoutToProficiency(loadout);
            AddExperience(profile, weaponTypeLine, baseWeaponExp);
            AddExperience(profile, GladiatorProficiencyType.Running, runningExp);
            AddExperience(profile, GladiatorProficiencyType.Tactics, tacticsExp);

            if (loadout.hasShield)
            {
                shieldExp = 250f + playerTeamKills * 60f;

                if (playerWon)
                {
                    shieldExp = shieldExp + 180f;
                }

                AddExperience(profile, GladiatorProficiencyType.Shield, shieldExp);
            }
        }
    }

    public void ApplyProficiencyBonusesToSpawnedPlayerSide(Transform combatantRoot)
    {
        GladiatorInstanceIdentity[] identities;
        int i;

        if (combatantRoot == null)
        {
            return;
        }

        identities = combatantRoot.GetComponentsInChildren<GladiatorInstanceIdentity>(true);

        for (i = 0; i < identities.Length; i++)
        {
            CharacterStats stats;
            GladiatorProfileData profile;
            WeaponLoadoutData loadout;

            if (identities[i] == null)
            {
                continue;
            }

            if (!identities[i].GetBelongsToPlayerSide())
            {
                continue;
            }

            profile = identities[i].GetGladiatorProfile();
            loadout = identities[i].GetSelectedLoadout();

            if (profile == null || loadout == null)
            {
                continue;
            }

            EnsureProfileInitialized(profile);

            stats = identities[i].GetComponent<CharacterStats>();

            if (stats == null)
            {
                continue;
            }

            ApplyStatBonuses(profile, loadout, stats);
        }
    }

    private void ApplyStatBonuses(
        GladiatorProfileData profile,
        WeaponLoadoutData loadout,
        CharacterStats stats
    )
    {
        int runningLevel;
        int shieldLevel;

        if (profile == null || loadout == null || stats == null)
        {
            return;
        }

        runningLevel = GetLevel(profile, GladiatorProficiencyType.Running);
        shieldLevel = GetLevel(profile, GladiatorProficiencyType.Shield);

        stats.moveSpeed = stats.moveSpeed * (1f + runningLevel * 0.0008f);

        if (loadout.weaponType == WeaponType.Melee)
        {
            int oneHandedLevel;

            oneHandedLevel = GetLevel(profile, GladiatorProficiencyType.OneHanded);
            stats.attackDamage = stats.attackDamage * (1f + oneHandedLevel * 0.0014f);
            stats.attackSpeedMultiplier = stats.attackSpeedMultiplier * (1f + oneHandedLevel * 0.00175f);
        }
        else if (loadout.weaponType == WeaponType.Polearm)
        {
            int polearmLevel;

            polearmLevel = GetLevel(profile, GladiatorProficiencyType.Polearm);
            stats.attackDamage = stats.attackDamage * (1f + polearmLevel * 0.0014f);
            stats.attackRange = stats.attackRange + polearmLevel * 0.0015f;
        }
        else if (loadout.weaponType == WeaponType.Ranged)
        {
            int bowLevel;

            bowLevel = GetLevel(profile, GladiatorProficiencyType.Bow);
            stats.attackDamage = stats.attackDamage * (1f + bowLevel * 0.0012f);
            stats.projectileSpeed = stats.projectileSpeed * (1f + bowLevel * 0.0015f);
        }

        if (loadout.hasShield)
        {
            float reduction;

            reduction = Mathf.Min(0.45f, shieldLevel * 0.0015f);
            stats.guardCooldown = stats.guardCooldown * (1f - reduction);
        }
    }

    public string GetStatBonusDescription(GladiatorProfileData profile, GladiatorProficiencyType type)
    {
        int level;
        float percentA;
        float percentB;

        EnsureProfileInitialized(profile);
        level = GetLevel(profile, type);

        if (type == GladiatorProficiencyType.OneHanded)
        {
            percentA = level * 0.14f;
            percentB = level * 0.175f;

            return
                "One-handed weapon damage +" + percentA.ToString("0.0") + "%" +
                "\nOne-handed weapon attack speed +" + percentB.ToString("0.0") + "%";
        }

        if (type == GladiatorProficiencyType.TwoHanded)
        {
            return "Two-handed weapon placeholder data";
        }

        if (type == GladiatorProficiencyType.Polearm)
        {
            percentA = level * 0.14f;

            return
                "Polearm weapon damage +" + percentA.ToString("0.0") + "%" +
                "\nPolearm weapon attack speed +" + (level * 0.15f).ToString("0.0") + "%";
        }

        if (type == GladiatorProficiencyType.Bow)
        {
            percentA = level * 0.12f;

            return
                "Bow damage +" + percentA.ToString("0.0") + "%" +
                "\nArrow flight speed +" + (level * 0.15f).ToString("0.0") + "%";
        }

        if (type == GladiatorProficiencyType.Crossbow)
        {
            return "Crossbow placeholder data";
        }

        if (type == GladiatorProficiencyType.Throwing)
        {
            return "Throwing weapon placeholder data";
        }

        if (type == GladiatorProficiencyType.Running)
        {
            return
                "Movement speed +" + (level * 0.08f).ToString("0.0") + "%" +
                "\nDash-related effects: Reserved interface";
        }

        if (type == GladiatorProficiencyType.CombatArts)
        {
            return "Combat arts placeholder data";
        }

        if (type == GladiatorProficiencyType.Shield)
        {
            return
                "Block cooldown reduction " + Mathf.Min(45f, level * 0.15f).ToString("0.0") + "%" +
                "\nBlock benefit: Reserved interface";
        }

        if (type == GladiatorProficiencyType.Tactics)
        {
            return
                "Command enhancement: Reserved interface" +
                "\nCommand benefit: Reserved interface";
        }


        if (type == GladiatorProficiencyType.Medicine)
        {
            return "Medicine skill placeholder data";
        }

        if (type == GladiatorProficiencyType.Magic)
        {
            return "Magic skill placeholder data";
        }


        return string.Empty;
    }

    public string GetPerkPreviewText(GladiatorProfileData profile, GladiatorProficiencyType type)
    {
        int level;
        int tier;

        EnsureProfileInitialized(profile);
        level = GetLevel(profile, type);

        tier = 50;
        return
            BuildPerkTierLine(level, tier, "General Line A", "Placeholder Perk A") +
            "\n" +
            BuildPerkTierLine(level, tier, "General Line B", "Placeholder Perk B") +
            "\n" +
            BuildPerkTierLine(level, tier, "Treasure Line", "Placeholder Relic Perk") +
            "\n\n" +
            BuildPerkTierLine(level, 100, "General Line A", "Placeholder Perk A2") +
            "\n" +
            BuildPerkTierLine(level, 100, "General Line B", "Placeholder Perk B2") +
            "\n" +
            BuildPerkTierLine(level, 100, "Treasure Line", "Placeholder Relic Perk2") +
            "\n\n" +
            BuildPerkTierLine(level, 150, "General Line A", "Placeholder Perk A3") +
            "\n" +
            BuildPerkTierLine(level, 150, "General Line B", "Placeholder Perk B3") +
            "\n" +
            BuildPerkTierLine(level, 150, "Treasure Line", "Placeholder Relic Perk3");
    }

    public GladiatorProficiencyType MapLoadoutToProficiency(WeaponLoadoutData loadout)
    {
        if (loadout == null)
        {
            return GladiatorProficiencyType.OneHanded;
        }

        if (loadout.weaponType == WeaponType.Polearm)
        {
            return GladiatorProficiencyType.Polearm;
        }

        if (loadout.weaponType == WeaponType.Ranged)
        {
            return GladiatorProficiencyType.Bow;
        }

        return GladiatorProficiencyType.OneHanded;
    }

    private string BuildPerkTierLine(int level, int tier, string lineName, string perkName)
    {
        if (level >= tier)
        {
            return "[" + tier + "] " + lineName + " - Unlocked - " + perkName;
        }

        return "[" + tier + "] " + lineName + " - Locked";
    }

    private float CalculateRequiredExp(int level)
    {
        // The experience requirement is
        // low in the early stages,
        // increases in the mid-stages,
        // and becomes significantly higher in the later stages.
        return 120f + level * 20f + level * level * 1.35f;
    }

    private ProficiencyState GetState(GladiatorProfileData profile, GladiatorProficiencyType type)
    {
        Dictionary<GladiatorProficiencyType, ProficiencyState> typeMap;

        if (profile == null)
        {
            return null;
        }

        EnsureProfileInitialized(profile);

        if (!runtimeData.TryGetValue(profile, out typeMap))
        {
            return null;
        }

        if (!typeMap.ContainsKey(type))
        {
            return null;
        }

        return typeMap[type];
    }
}
