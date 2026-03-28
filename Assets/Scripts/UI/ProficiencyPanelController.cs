using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ProficiencyPanelController : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private ArenaProgressionManager progressionManager;

    [Header("Profile Select")]
    [SerializeField] private TMP_Dropdown profileDropdown;

    [Header("Cell Parents")]
    [SerializeField] private Transform vigorRow;
    [SerializeField] private Transform controlRow;
    [SerializeField] private Transform enduranceRow;
    [SerializeField] private Transform intelligenceRow;
    [SerializeField] private GameObject proficiencyCellPrefab;

    [Header("Right Detail")]
    [SerializeField] private TMP_Text selectedProfileText;
    [SerializeField] private TMP_Text selectedTypeText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text statBonusText;
    [SerializeField] private TMP_Text expInfoText;
    [SerializeField] private TMP_Text perkPreviewText;

    private List<GladiatorProfileData> currentProfiles = new List<GladiatorProfileData>();
    private GladiatorProficiencyType selectedType = GladiatorProficiencyType.OneHanded;
    private bool cellsBuilt = false;

    void Start()
    {
        if (profileDropdown != null)
        {
            profileDropdown.onValueChanged.AddListener(HandleProfileChanged);
        }

        BuildCellsIfNeeded();
        RefreshRightDetail();
    }

    public void SetProfiles(List<GladiatorProfileData> profiles)
    {
        int i;

        currentProfiles.Clear();

        if (profiles != null)
        {
            for (i = 0; i < profiles.Count; i++)
            {
                if (profiles[i] == null)
                {
                    continue;
                }

                if (currentProfiles.Contains(profiles[i]))
                {
                    continue;
                }

                currentProfiles.Add(profiles[i]);
            }
        }

        if (profileDropdown != null)
        {
            profileDropdown.ClearOptions();

            for (i = 0; i < currentProfiles.Count; i++)
            {
                profileDropdown.options.Add(new TMP_Dropdown.OptionData(currentProfiles[i].GetGladiatorName()));
            }

            if (currentProfiles.Count > 0)
            {
                profileDropdown.value = 0;
                profileDropdown.RefreshShownValue();
            }
        }

        RefreshRightDetail();
    }

    public void SelectProficiencyType(GladiatorProficiencyType type)
    {
        selectedType = type;
        RefreshRightDetail();
    }

    private void HandleProfileChanged(int index)
    {
        RefreshRightDetail();
    }

    private void BuildCellsIfNeeded()
    {
        if (cellsBuilt)
        {
            return;
        }

        BuildRow(vigorRow, new GladiatorProficiencyType[] {
            GladiatorProficiencyType.OneHanded,
            GladiatorProficiencyType.TwoHanded,
            GladiatorProficiencyType.Polearm
        }, new string[] {
            "One-handed Weapon",
            "Two-handed Weapon",
            "Polearm Weapon"
        });

        BuildRow(controlRow, new GladiatorProficiencyType[] {
            GladiatorProficiencyType.Bow,
            GladiatorProficiencyType.Crossbow,
            GladiatorProficiencyType.Throwing
        }, new string[] {
            "Bow",
            "Crossbow",
            "Throwing"
        });

        BuildRow(enduranceRow, new GladiatorProficiencyType[] {
            GladiatorProficiencyType.Running,
            GladiatorProficiencyType.CombatArts,
            GladiatorProficiencyType.Shield
        }, new string[] {
            "Running",
            "Combat Arts",
            "Shield"
        });

        BuildRow(intelligenceRow, new GladiatorProficiencyType[] {
            GladiatorProficiencyType.Tactics,
            GladiatorProficiencyType.Medicine,
            GladiatorProficiencyType.Magic
        }, new string[] {
            "Tactics",
            "Medicine",
            "Magic"
        });

        cellsBuilt = true;
    }

    private void BuildRow(
        Transform rowParent,
        GladiatorProficiencyType[] types,
        string[] labels
    )
    {
        int i;

        if (rowParent == null || proficiencyCellPrefab == null)
        {
            return;
        }

        for (i = 0; i < types.Length; i++)
        {
            GameObject cellObject;
            ProficiencyTypeCellUI cellUI;

            cellObject = Instantiate(proficiencyCellPrefab, rowParent);
            cellUI = cellObject.GetComponent<ProficiencyTypeCellUI>();

            if (cellUI == null)
            {
                continue;
            }

            cellUI.Setup(this, types[i], labels[i]);
        }
    }

    private void RefreshRightDetail()
    {
        GladiatorProfileData currentProfile;
        int level;
        float currentExp;
        float requiredExp;
        float multiplier;

        currentProfile = GetSelectedProfile();

        if (currentProfile == null || progressionManager == null)
        {
            if (selectedProfileText != null)
            {
                selectedProfileText.text = "No character selected";
            }

            return;
        }

        progressionManager.EnsureProfileInitialized(currentProfile);

        level = progressionManager.GetLevel(currentProfile, selectedType);
        currentExp = progressionManager.GetCurrentExp(currentProfile, selectedType);
        requiredExp = progressionManager.GetExpRequiredForNextLevel(currentProfile, selectedType);
        multiplier = progressionManager.GetCurrentExperienceMultiplier(currentProfile, selectedType);

        if (selectedProfileText != null)
        {
            selectedProfileText.text = "Character: " + currentProfile.GetGladiatorName();
        }

        if (selectedTypeText != null)
        {
            selectedTypeText.text = "Proficiency Type: " + GetTypeLabel(selectedType);
        }

        if (levelText != null)
        {
            levelText.text = "Level: " + level + " / 300";
        }

        if (statBonusText != null)
        {
            statBonusText.text = progressionManager.GetStatBonusDescription(currentProfile, selectedType);
        }

        if (expInfoText != null)
        {
            expInfoText.text =
                "Current experience: " + currentExp.ToString("0") +
                "\nUpgrade required: " + requiredExp.ToString("0") +
                "\nExp multiplier: " + multiplier.ToString("0.00") + "x";
        }

        if (perkPreviewText != null)
        {
            perkPreviewText.text = progressionManager.GetPerkPreviewText(currentProfile, selectedType);
        }
    }

    private GladiatorProfileData GetSelectedProfile()
    {
        int index;

        if (currentProfiles.Count == 0)
        {
            return null;
        }

        if (profileDropdown == null)
        {
            return currentProfiles[0];
        }

        index = Mathf.Clamp(profileDropdown.value, 0, currentProfiles.Count - 1);
        return currentProfiles[index];
    }

    private string GetTypeLabel(GladiatorProficiencyType type)
    {
        if (type == GladiatorProficiencyType.OneHanded)
        {
            return "One-handed Weapon";
        }

        if (type == GladiatorProficiencyType.TwoHanded)
        {
            return "Two-handed Weapon";
        }

        if (type == GladiatorProficiencyType.Polearm)
        {
            return "Polearm Weapon";
        }

        if (type == GladiatorProficiencyType.Bow)
        {
            return "Bow";
        }

        if (type == GladiatorProficiencyType.Crossbow)
        {
            return "Crossbow";
        }

        if (type == GladiatorProficiencyType.Throwing)
        {
            return "Throwing";
        }

        if (type == GladiatorProficiencyType.Running)
        {
            return "Running";
        }

        if (type == GladiatorProficiencyType.CombatArts)
        {
            return "Combat Arts";
        }

        if (type == GladiatorProficiencyType.Shield)
        {
            return "Shield";
        }

        if (type == GladiatorProficiencyType.Tactics)
        {
            return "Tactics";
        }

        if (type == GladiatorProficiencyType.Medicine)
        {
            return "Medicine";
        }

        if (type == GladiatorProficiencyType.Magic)
        {
            return "Magic";
        }

        return type.ToString();
    }
}
