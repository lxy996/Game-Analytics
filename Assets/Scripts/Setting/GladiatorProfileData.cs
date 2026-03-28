using System.Collections.Generic;
using UnityEngine;

public enum GladiatorProficiencyType
{
    OneHanded,
    TwoHanded,
    Polearm,

    Bow,
    Crossbow,
    Throwing,

    Running,
    CombatArts,
    Shield,

    Tactics,
    Medicine,
    Magic
}

[System.Serializable]
public class GladiatorProficiencyEntry
{
    public GladiatorProficiencyType proficiencyType;
    public int level = 1;
}

[System.Serializable]
public class GladiatorSkillPreviewEntry
{
    public string skillName;
    [TextArea(2, 4)] public string description;
    public Sprite icon;
    public bool unlocked = true;
}

[CreateAssetMenu(fileName = "GladiatorProfile", menuName = "Game/Gladiator Profile")]
public class GladiatorProfileData : ScriptableObject
{
    [Header("Identity")]
    public string gladiatorName;
    [TextArea(2, 4)] public string shortDescription;
    public Sprite portrait;

    [Header("Weapon Options")]
    public WeaponLoadoutData defaultLoadout;
    public List<WeaponLoadoutData> availableLoadouts = new List<WeaponLoadoutData>();

    [Header("Proficiencies")]
    public List<GladiatorProficiencyEntry> proficiencies = new List<GladiatorProficiencyEntry>();

    [Header("Skills Preview")]
    public List<GladiatorSkillPreviewEntry> skills = new List<GladiatorSkillPreviewEntry>();

    [Header("Economy Placeholder")]
    public int recruitCost = 100;
    public int weeklyWage = 10;

    public string GetGladiatorName()
    {
        if (string.IsNullOrEmpty(gladiatorName))
        {
            return name;
        }

        return gladiatorName;
    }

    public WeaponLoadoutData GetDefaultLoadout()
    {
        if (defaultLoadout != null)
        {
            return defaultLoadout;
        }

        if (availableLoadouts != null && availableLoadouts.Count > 0)
        {
            return availableLoadouts[0];
        }

        return null;
    }

    public int GetProficiencyLevel(GladiatorProficiencyType type)
    {
        int i;

        if (proficiencies == null)
        {
            return 0;
        }

        for (i = 0; i < proficiencies.Count; i++)
        {
            if (proficiencies[i] == null)
            {
                continue;
            }

            if (proficiencies[i].proficiencyType == type)
            {
                return proficiencies[i].level;
            }
        }

        return 0;
    }
    public bool HasAnyAvailableLoadout()
    {
        if (defaultLoadout != null)
        {
            return true;
        }

        if (availableLoadouts != null && availableLoadouts.Count > 0)
        {
            return true;
        }

        return false;
    }

    public WeaponLoadoutData GetRandomAvailableLoadout()
    {
        List<WeaponLoadoutData> candidates;
        int i;
        int index;

        candidates = new List<WeaponLoadoutData>();

        if (defaultLoadout != null)
        {
            candidates.Add(defaultLoadout);
        }

        if (availableLoadouts != null)
        {
            for (i = 0; i < availableLoadouts.Count; i++)
            {
                if (availableLoadouts[i] == null)
                {
                    continue;
                }

                if (candidates.Contains(availableLoadouts[i]))
                {
                    continue;
                }

                candidates.Add(availableLoadouts[i]);
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        index = Random.Range(0, candidates.Count);
        return candidates[index];
    }
}


