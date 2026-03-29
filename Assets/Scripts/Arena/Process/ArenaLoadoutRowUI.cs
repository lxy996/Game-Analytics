using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ArenaLoadoutRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text proficiencyText;
    [SerializeField] private TMP_Text weaponNameText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Dropdown loadoutDropdown;
    [SerializeField] private Toggle includeToggle;
    [SerializeField] private TMP_Text selectionStateText;
    [SerializeField] private Image dimMask;


    private ArenaMatchFighterEntry sourceEntry;
    private List<WeaponLoadoutData> availableLoadouts = new List<WeaponLoadoutData>();
    private bool allowLoadoutEdit = true;
    private TeamVisualColor previewColor = TeamVisualColor.Blue;
    private bool forceIncluded = false;

    public void Setup(
        ArenaMatchFighterEntry entry,
        string proficiencySummary,
        bool editable,
        TeamVisualColor teamColor
    )
    {
        WeaponLoadoutData initialLoadout;
        int i;
        int selectedIndex;

        sourceEntry = entry;
        allowLoadoutEdit = editable;
        previewColor = teamColor;
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

        initialLoadout = ResolveInitialLoadout();

        if (loadoutDropdown != null)
        {
            loadoutDropdown.onValueChanged.RemoveAllListeners();
            loadoutDropdown.gameObject.SetActive(allowLoadoutEdit);

            if (allowLoadoutEdit)
            {
                loadoutDropdown.ClearOptions();

                for (i = 0; i < availableLoadouts.Count; i++)
                {
                    loadoutDropdown.options.Add(new TMP_Dropdown.OptionData(availableLoadouts[i].name));
                }

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
                loadoutDropdown.onValueChanged.AddListener(HandleLoadoutChanged);
            }
        }

        RefreshVisuals();

        if (includeToggle != null)
        {        
            includeToggle.onValueChanged.RemoveAllListeners();

            includeToggle.onValueChanged.AddListener((bool isOn) => {
                
                SetSelectionState(isOn, forceIncluded);
            });
        }
    }

    private void HandleLoadoutChanged(int index)
    {
        RefreshVisuals();
    }

    private void RefreshVisuals()
    {
        WeaponLoadoutData currentLoadout;
        Sprite previewSprite;

        currentLoadout = GetSelectedLoadout();

        if (weaponNameText != null)
        {
            if (currentLoadout != null)
            {
                weaponNameText.text = currentLoadout.name;
            }
            else
            {
                weaponNameText.text = "No Weapon";
            }
        }

        previewSprite = ResolvePreviewSprite(currentLoadout);

        if (portraitImage != null)
        {
            portraitImage.sprite = previewSprite;
            portraitImage.enabled = previewSprite != null;
        }
    }

    private Sprite ResolvePreviewSprite(WeaponLoadoutData loadout)
    {
        Sprite sprite;

        if (loadout != null)
        {
            sprite = loadout.GetIdleSpriteForTeam(previewColor);

            if (sprite != null)
            {
                return sprite;
            }

            if (loadout.idleSprite != null)
            {
                return loadout.idleSprite;
            }

            if (loadout.pickupIcon != null)
            {
                return loadout.pickupIcon;
            }
        }

        if (sourceEntry != null && sourceEntry.gladiatorProfile != null)
        {
            return sourceEntry.gladiatorProfile.portrait;
        }

        return null;
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

        if (includeToggle != null)
        {
            runtimeEntry.includeInMatch = includeToggle.isOn || forceIncluded;
        }

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

        if (!allowLoadoutEdit || loadoutDropdown == null)
        {
            return ResolveInitialLoadout();
        }

        index = Mathf.Clamp(loadoutDropdown.value, 0, availableLoadouts.Count - 1);
        return availableLoadouts[index];
    }

    public bool GetIsSelectedForMatch()
    {
        if (includeToggle == null)
        {
            return true;
        }

        return includeToggle.isOn || forceIncluded;
    }

    public void SetSelectionState(bool selected, bool locked)
    {
        forceIncluded = locked;

        if (includeToggle != null)
        {
            includeToggle.interactable = !locked;
            includeToggle.isOn = selected || locked;
        }

        if (selectionStateText != null)
        {
            if (locked)
            {
                selectionStateText.text = "Required";
            }
            else
            {
                selectionStateText.text = (includeToggle != null && includeToggle.isOn) ? "Selected" : "Bench";
            }
        }

        if (dimMask != null)
        {
            bool isSelected;

            isSelected = true;

            if (includeToggle != null)
            {
                isSelected = includeToggle.isOn;
            }

            dimMask.enabled = !isSelected;
        }
    }

}
