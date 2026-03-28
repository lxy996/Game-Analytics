using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ArenaLoadoutRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text proficiencyText;
    [SerializeField] private TMP_Dropdown loadoutDropdown;

    private ArenaMatchFighterEntry sourceEntry;
    private List<WeaponLoadoutData> availableLoadouts = new List<WeaponLoadoutData>();

    public void Setup(ArenaMatchFighterEntry entry, string proficiencySummary)
    {
        WeaponLoadoutData initialLoadout;
        int i;
        int selectedIndex;

        sourceEntry = entry;
        availableLoadouts.Clear();

        if (sourceEntry == null || sourceEntry.gladiatorProfile == null)
        {
            return;
        }

        if (nameText != null)
        {
            nameText.text = sourceEntry.gladiatorProfile.GetGladiatorName();
        }

        if (proficiencyText != null)
        {
            proficiencyText.text = proficiencySummary;
        }

        BuildLoadoutList(sourceEntry.gladiatorProfile);

        if (loadoutDropdown == null)
        {
            return;
        }

        loadoutDropdown.ClearOptions();

        for (i = 0; i < availableLoadouts.Count; i++)
        {
            loadoutDropdown.options.Add(new TMP_Dropdown.OptionData(availableLoadouts[i].name));
        }

        initialLoadout = ResolveInitialLoadout();
        selectedIndex = 0;

        for (i = 0; i < availableLoadouts.Count; i++)
        {
            if (availableLoadouts[i] == initialLoadout)
            {
                selectedIndex = i;
                break;
            }
        }

        loadoutDropdown.value = selectedIndex;
        loadoutDropdown.RefreshShownValue();
    }

    private void BuildLoadoutList(GladiatorProfileData profile)
    {
        int i;

        if (profile == null)
        {
            return;
        }

        if (profile.defaultLoadout != null)
        {
            availableLoadouts.Add(profile.defaultLoadout);
        }

        if (profile.availableLoadouts != null)
        {
            for (i = 0; i < profile.availableLoadouts.Count; i++)
            {
                if (profile.availableLoadouts[i] == null)
                {
                    continue;
                }

                if (availableLoadouts.Contains(profile.availableLoadouts[i]))
                {
                    continue;
                }

                availableLoadouts.Add(profile.availableLoadouts[i]);
            }
        }
    }

    private WeaponLoadoutData ResolveInitialLoadout()
    {
        if (sourceEntry != null && sourceEntry.selectedLoadout != null)
        {
            return sourceEntry.selectedLoadout;
        }

        if (sourceEntry != null && sourceEntry.gladiatorProfile != null)
        {
            return sourceEntry.gladiatorProfile.GetDefaultLoadout();
        }

        return null;
    }

    public ArenaMatchFighterEntry BuildRuntimeEntry()
    {
        ArenaMatchFighterEntry runtimeEntry;

        runtimeEntry = new ArenaMatchFighterEntry();

        if (sourceEntry == null)
        {
            return runtimeEntry;
        }

        runtimeEntry.includeInMatch = sourceEntry.includeInMatch;
        runtimeEntry.gladiatorProfile = sourceEntry.gladiatorProfile;
        runtimeEntry.spawnAsPlayerControlled = sourceEntry.spawnAsPlayerControlled;
        runtimeEntry.selectedLoadout = GetSelectedLoadout();

        return runtimeEntry;
    }

    public WeaponLoadoutData GetSelectedLoadout()
    {
        int index;

        if (availableLoadouts.Count == 0)
        {
            return null;
        }

        if (loadoutDropdown == null)
        {
            return availableLoadouts[0];
        }

        index = Mathf.Clamp(loadoutDropdown.value, 0, availableLoadouts.Count - 1);
        return availableLoadouts[index];
    }
}
