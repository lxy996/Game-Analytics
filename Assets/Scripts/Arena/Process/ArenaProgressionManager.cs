using System;
using System.Collections.Generic;
using UnityEngine;

public enum ProficiencyPerkEffectType
{
    None,
    AttackSpeed,
    MoveSpeed,
    LifeSteal,
    Regeneration,
    MaxHealth,
    ExtraGuardBlock,
    Damage
}

[System.Serializable]
public class SelectedPerkChoice
{
    public GladiatorProficiencyType proficiencyType;
    public int tier;
    public bool choseTop;
}

public class ArenaProgressionManager : MonoBehaviour
{
    private class ProficiencyState
    {
        public int level = 1;
        public float currentExp = 0f;
    }

    private Dictionary<GladiatorProfileData, Dictionary<GladiatorProficiencyType, ProficiencyState>> runtimeData =
        new Dictionary<GladiatorProfileData, Dictionary<GladiatorProficiencyType, ProficiencyState>>();

    private Dictionary<GladiatorProfileData, List<SelectedPerkChoice>> selectedPerks =
    new Dictionary<GladiatorProfileData, List<SelectedPerkChoice>>();

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

    public string AwardPostMatchExperienceAndBuildSummary(
    List<ArenaMatchFighterEntry> playerRoster,
    bool playerWon,
    int playerTeamKills
)
    {
        List<string> lines;
        int i;

        lines = new List<string>();

        if (playerRoster == null)
        {
            return "";
        }

        for (i = 0; i < playerRoster.Count; i++)
        {
            GladiatorProfileData profile;
            WeaponLoadoutData loadout;
            GladiatorProficiencyType weaponLine;
            int beforeWeapon;
            int beforeRunning;
            int beforeTactics;
            int beforeShield;
            int afterWeapon;
            int afterRunning;
            int afterTactics;
            int afterShield;
            string line;

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

            weaponLine = MapLoadoutToProficiency(loadout);

            beforeWeapon = GetLevel(profile, weaponLine);
            beforeRunning = GetLevel(profile, GladiatorProficiencyType.Running);
            beforeTactics = GetLevel(profile, GladiatorProficiencyType.Tactics);
            beforeShield = GetLevel(profile, GladiatorProficiencyType.Shield);

            AwardPostMatchExperience(
                new List<ArenaMatchFighterEntry> { playerRoster[i] },
                playerWon,
                playerTeamKills
            );

            afterWeapon = GetLevel(profile, weaponLine);
            afterRunning = GetLevel(profile, GladiatorProficiencyType.Running);
            afterTactics = GetLevel(profile, GladiatorProficiencyType.Tactics);
            afterShield = GetLevel(profile, GladiatorProficiencyType.Shield);

            line = profile.GetGladiatorName() + ":";

            if (afterWeapon > beforeWeapon)
            {
                line = line + " " + weaponLine + " +" + (afterWeapon - beforeWeapon);
            }

            if (afterRunning > beforeRunning)
            {
                line = line + " Running +" + (afterRunning - beforeRunning);
            }

            if (afterTactics > beforeTactics)
            {
                line = line + " Tactics +" + (afterTactics - beforeTactics);
            }

            if (afterShield > beforeShield)
            {
                line = line + " Shield +" + (afterShield - beforeShield);
            }

            if (line != profile.GetGladiatorName() + ":")
            {
                lines.Add(line);
            }
        }

        if (lines.Count == 0)
        {
            return "There was no proficiency upgrade in this battle.";
        }

        return string.Join("\n", lines);
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

            // Allows enemies to also receive proficiency bonuses.
            /*if (!identities[i].GetBelongsToPlayerSide())
            {
                continue;
            }*/

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
            ApplySelectedPerkBonuses(profile, loadout, stats);

            Health h = identities[i].GetComponent<Health>();
            if (h != null)
            {
                h.ResetHealthToMax();
            }
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
        stats.bonusMaxHealth = stats.bonusMaxHealth + runningLevel * 0.06f;

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
            stats.attackSpeedMultiplier = stats.attackSpeedMultiplier * (1f + polearmLevel * 0.0012f);
        }
        else if (loadout.weaponType == WeaponType.Ranged)
        {
            int bowLevel;

            bowLevel = GetLevel(profile, GladiatorProficiencyType.Bow);
            stats.attackDamage = stats.attackDamage * (1f + bowLevel * 0.0012f);
            stats.projectileSpeed = stats.projectileSpeed * (1f + bowLevel * 0.0015f);
            stats.attackSpeedMultiplier = stats.attackSpeedMultiplier * (1f + bowLevel * 0.00125f);
        }

        if (loadout.hasShield)
        {
            float reduction;
            int extraBlocks;

            reduction = Mathf.Min(0.45f, shieldLevel * 0.0015f);
            stats.guardCooldown = stats.guardCooldown * (1f - reduction);

            extraBlocks = shieldLevel / 100;
            stats.guardBlockCountPerUse = stats.guardBlockCountPerUse + extraBlocks;
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
                "\nPolearm weapon attack range +" + (level * 0.15f).ToString("0.0") + "%" +
                "\nPolearm weapon attack speed +" + (level * 0.12f).ToString("0.0") + "%";

        }

        if (type == GladiatorProficiencyType.Bow)
        {
            percentA = level * 0.12f;

            return
                "Bow damage +" + percentA.ToString("0.0") + "%" +
                "\nArrow flight speed +" + (level * 0.15f).ToString("0.0") + "%" +
                "\nBow attack speed +" + (level * 0.125f).ToString("0.0") + "%";
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
                "\nMax health +" + (level * 0.06f).ToString("0.0");
        }

        if (type == GladiatorProficiencyType.CombatArts)
        {
            return "Combat arts placeholder data";
        }

        if (type == GladiatorProficiencyType.Shield)
        {
            return
                "Block cooldown reduction " + Mathf.Min(45f, level * 0.15f).ToString("0.0") + "%" +
                "\nExtra blocks per guard +" + (level / 100);
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

    /*public string GetPerkPreviewText(GladiatorProfileData profile, GladiatorProficiencyType type)
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
    }*/

    public void SetPerkChoice(
    GladiatorProfileData profile,
    GladiatorProficiencyType type,
    int tier,
    bool choseTop
)
    {
        List<SelectedPerkChoice> perkList;
        int i;

        if (profile == null)
        {
            return;
        }

        if (!selectedPerks.TryGetValue(profile, out perkList))
        {
            perkList = new List<SelectedPerkChoice>();
            selectedPerks[profile] = perkList;
        }

        for (i = 0; i < perkList.Count; i++)
        {
            if (perkList[i].proficiencyType == type && perkList[i].tier == tier)
            {
                perkList[i].choseTop = choseTop;
                return;
            }
        }

        perkList.Add(new SelectedPerkChoice
        {
            proficiencyType = type,
            tier = tier,
            choseTop = choseTop
        });
    }

    private void ApplySelectedPerkBonuses(
    GladiatorProfileData profile,
    WeaponLoadoutData loadout,
    CharacterStats stats
)
    {
        List<SelectedPerkChoice> perkList;
        int i;

        if (profile == null || stats == null)
        {
            return;
        }

        if (!selectedPerks.TryGetValue(profile, out perkList))
        {
            return;
        }

        for (i = 0; i < perkList.Count; i++)
        {
            ApplySinglePerk(profile, perkList[i], stats);
        }
    }

    private void ApplySinglePerk(
    GladiatorProfileData profile,
    SelectedPerkChoice perk,
    CharacterStats stats
)
    {
        if (perk == null || stats == null)
        {
            return;
        }

        if (perk.proficiencyType == GladiatorProficiencyType.OneHanded)
        {
            if (perk.tier == 50)
            {
                if (perk.choseTop)
                {
                    stats.attackSpeedMultiplier = stats.attackSpeedMultiplier * 1.08f;
                }
                else
                {
                    stats.attackDamage = stats.attackDamage * 1.08f;
                }
            }
            else if (perk.tier == 100)
            {
                if (perk.choseTop)
                {
                    stats.lifeStealPercent = stats.lifeStealPercent + 0.08f;
                }
                else
                {
                    stats.healthRegenPerSecond = stats.healthRegenPerSecond + 1f;
                }
            }
            else if (perk.tier == 150)
            {
                if (perk.choseTop)
                {
                    stats.bonusMaxHealth = stats.bonusMaxHealth + 15f;
                }
                else
                {
                    stats.moveSpeed = stats.moveSpeed * 1.08f;
                }
            }
        }
        else if (perk.proficiencyType == GladiatorProficiencyType.Polearm)
        {
            if (perk.tier == 50)
            {
                if (perk.choseTop)
                {
                    stats.attackSpeedMultiplier = stats.attackSpeedMultiplier * 1.08f;
                }
                else
                {
                    stats.attackDamage = stats.attackDamage * 1.08f;
                }
            }
            else if (perk.tier == 100)
            {
                if (perk.choseTop)
                {
                    stats.attackRange = stats.attackRange * 1.10f;
                }
                else
                {
                    stats.healthRegenPerSecond = stats.healthRegenPerSecond + 1f;
                }
            }
            else if (perk.tier == 150)
            {
                if (perk.choseTop)
                {
                    stats.lifeStealPercent = stats.lifeStealPercent + 0.08f;
                }
                else
                {
                    stats.bonusMaxHealth = stats.bonusMaxHealth + 15f;
                }
            }
        }
        else if (perk.proficiencyType == GladiatorProficiencyType.Bow)
        {
            if (perk.tier == 50)
            {
                if (perk.choseTop)
                {
                    stats.attackSpeedMultiplier = stats.attackSpeedMultiplier * 1.10f;
                }
                else
                {
                    stats.attackDamage = stats.attackDamage * 1.08f;
                }
            }
            else if (perk.tier == 100)
            {
                if (perk.choseTop)
                {
                    stats.projectileSpeed = stats.projectileSpeed * 1.12f;
                }
                else
                {
                    stats.moveSpeed = stats.moveSpeed * 1.08f;
                }
            }
            else if (perk.tier == 150)
            {
                if (perk.choseTop)
                {
                    stats.lifeStealPercent = stats.lifeStealPercent + 0.08f;
                }
                else
                {
                    stats.healthRegenPerSecond = stats.healthRegenPerSecond + 1f;
                }
            }
        }
        else if (perk.proficiencyType == GladiatorProficiencyType.Running)
        {
            if (perk.tier == 50)
            {
                if (perk.choseTop)
                {
                    stats.moveSpeed = stats.moveSpeed * 1.08f;
                }
                else
                {
                    stats.bonusMaxHealth = stats.bonusMaxHealth + 15f;
                }
            }
            else if (perk.tier == 100)
            {
                if (perk.choseTop)
                {
                    // Dash cooldown reserved for next step
                }
                else
                {
                    stats.healthRegenPerSecond = stats.healthRegenPerSecond + 1f;
                }
            }
            else if (perk.tier == 150)
            {
                if (perk.choseTop)
                {
                    stats.attackSpeedMultiplier = stats.attackSpeedMultiplier * 1.08f;
                }
                else
                {
                    stats.lifeStealPercent = stats.lifeStealPercent + 0.06f;
                }
            }
        }
        else if (perk.proficiencyType == GladiatorProficiencyType.Shield)
        {
            if (perk.tier == 50)
            {
                if (perk.choseTop)
                {
                    stats.guardBlockCountPerUse = stats.guardBlockCountPerUse + 1;
                }
                else
                {
                    stats.guardCooldown = stats.guardCooldown * 0.88f;
                }
            }
            else if (perk.tier == 100)
            {
                if (perk.choseTop)
                {
                    stats.bonusMaxHealth = stats.bonusMaxHealth + 20f;
                }
                else
                {
                    stats.healthRegenPerSecond = stats.healthRegenPerSecond + 1f;
                }
            }
            else if (perk.tier == 150)
            {
                if (perk.choseTop)
                {
                    stats.moveSpeed = stats.moveSpeed * 1.08f;
                }
                else
                {
                    stats.attackDamage = stats.attackDamage * 1.06f;
                }
            }
        }
    }

    public string GetPerkDisplayName(
    GladiatorProficiencyType type,
    int tier,
    bool isTop
)
    {
        if (type == GladiatorProficiencyType.OneHanded)
        {
            if (tier == 50)
            {
                return isTop ? "Attack Speed +" : "Damage +";
            }

            if (tier == 100)
            {
                return isTop ? "Life Steal" : "Regeneration";
            }

            if (tier == 150)
            {
                return isTop ? "Max Health +" : "Move Speed +";
            }
        }

        if (type == GladiatorProficiencyType.Polearm)
        {
            if (tier == 50)
            {
                return isTop ? "Attack Speed +" : "Damage +";
            }

            if (tier == 100)
            {
                return isTop ? "Range +" : "Regeneration";
            }

            if (tier == 150)
            {
                return isTop ? "Life Steal" : "Max Health +";
            }
        }

        if (type == GladiatorProficiencyType.Bow)
        {
            if (tier == 50)
            {
                return isTop ? "Attack Speed +" : "Damage +";
            }

            if (tier == 100)
            {
                return isTop ? "Projectile Speed +" : "Move Speed +";
            }

            if (tier == 150)
            {
                return isTop ? "Life Steal" : "Regeneration";
            }
        }

        if (type == GladiatorProficiencyType.Running)
        {
            if (tier == 50)
            {
                return isTop ? "Move Speed +" : "Max Health +";
            }

            if (tier == 100)
            {
                return isTop ? "Dash Cooldown -" : "Regeneration";
            }

            if (tier == 150)
            {
                return isTop ? "Attack Speed +" : "Life Steal";
            }
        }

        if (type == GladiatorProficiencyType.Shield)
        {
            if (tier == 50)
            {
                return isTop ? "Extra Guard Block" : "Guard Cooldown -";
            }

            if (tier == 100)
            {
                return isTop ? "Max Health +" : "Regeneration";
            }

            if (tier == 150)
            {
                return isTop ? "Move Speed +" : "Damage +";
            }
        }

        if (type == GladiatorProficiencyType.Tactics)
        {
            if (tier == 50)
            {
                return isTop ? "Command Power +" : "Command Duration +";
            }

            if (tier == 100)
            {
                return isTop ? "Focus Bonus +" : "Follow Bonus +";
            }
        }

        return isTop ? "Top Perk" : "Bottom Perk";
    }

    public string GetPerkShortDescription(
        GladiatorProficiencyType type,
        int tier,
        bool isTop
    )
    {
        string name;

        name = GetPerkDisplayName(type, tier, isTop);

        if (name == "Attack Speed +")
        {
            return "+8% Attack Speed";
        }

        if (name == "Damage +")
        {
            return "+8% Damage";
        }

        if (name == "Life Steal")
        {
            return "+8% Life Steal";
        }

        if (name == "Regeneration")
        {
            return "+1 HP/s";
        }

        if (name == "Max Health +")
        {
            return "+15 Max Health";
        }

        if (name == "Move Speed +")
        {
            return "+8% Move Speed";
        }

        if (name == "Projectile Speed +")
        {
            return "+12% Projectile Speed";
        }

        if (name == "Range +")
        {
            return "+10% Attack Range";
        }

        if (name == "Extra Guard Block")
        {
            return "+1 Guard Block";
        }

        if (name == "Guard Cooldown -")
        {
            return "-12% Guard Cooldown";
        }

        if (name == "Dash Cooldown -")
        {
            return "Reserved";
        }

        return "Reserved";
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

    public bool TryGetPerkChoice(
    GladiatorProfileData profile,
    GladiatorProficiencyType type,
    int tier,
    out bool choseTop
)
    {
        List<SelectedPerkChoice> perkList;
        int i;

        choseTop = false;

        if (profile == null)
        {
            return false;
        }

        if (!selectedPerks.TryGetValue(profile, out perkList))
        {
            return false;
        }

        for (i = 0; i < perkList.Count; i++)
        {
            if (perkList[i].proficiencyType == type && perkList[i].tier == tier)
            {
                choseTop = perkList[i].choseTop;
                return true;
            }
        }

        return false;
    }

}
