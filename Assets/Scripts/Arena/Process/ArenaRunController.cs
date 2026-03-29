using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ArenaRunController : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private ArenaMatchSetup matchSetup;
    [SerializeField] private ArenaMatchManager matchManager;
    [SerializeField] private ArenaMatchEventSpawner eventSpawner;
    [SerializeField] private ArenaProgressionManager progressionManager;
    [SerializeField] private ProficiencyPanelController proficiencyPanel;
    [SerializeField] private Transform combatantRoot;

    [Header("Normal Battles")]
    [SerializeField] private List<ArenaMatchPresetData> normalBattlePresets = new List<ArenaMatchPresetData>();

    [Header("Family Challenges")]
    [SerializeField] private List<FamilyChallengeDefinitionData> familyChallenges = new List<FamilyChallengeDefinitionData>();

    [Header("Panels")]
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject normalListPanel;
    [SerializeField] private GameObject challengeListPanel;
    [SerializeField] private GameObject specialListPanel;
    [SerializeField] private GameObject preMatchPanel;
    [SerializeField] private GameObject battleHudRoot;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private GameObject proficiencyPanelRoot;

    [Header("Mode Buttons")]
    [SerializeField] private Button normalModeButton;
    [SerializeField] private Button challengeModeButton;
    [SerializeField] private Button specialModeButton;

    [Header("List Containers")]
    [SerializeField] private Transform normalListContainer;
    [SerializeField] private Transform challengeListContainer;
    [SerializeField] private GameObject battleListButtonPrefab;

    [Header("Pre-Match UI")]
    [SerializeField] private TMP_Text matchTitleText;
    [SerializeField] private TMP_Text ruleInfoText;
    [SerializeField] private Button startMatchButton;
    [SerializeField] private Button backToLobbyButton;
    [SerializeField] private Button openProficiencyButton;
    [SerializeField] private Transform loadoutRowContainer;
    [SerializeField] private GameObject loadoutRowPrefab;
    [SerializeField] private Transform enemyPreviewContainer;
    [SerializeField] private GameObject enemyPreviewCardPrefab;

    [Header("Result UI")]
    [SerializeField] private TMP_Text resultSummaryText;
    [SerializeField] private Button retryMatchButton;
    [SerializeField] private Button returnToLobbyButton;
    [SerializeField] private Button nextNormalBattleButton;

    private readonly List<ArenaLoadoutRowUI> activeRows = new List<ArenaLoadoutRowUI>();
    private readonly HashSet<string> clearedFamilyIds = new HashSet<string>();

    private List<ArenaMatchFighterEntry> cachedEnemyPreviewRoster = new List<ArenaMatchFighterEntry>();
    private ArenaTeamThemeData cachedEnemyPreviewTheme;
    private string lastProgressGainSummary = "";

    private ArenaMatchPresetData selectedPreset;
    private FamilyChallengeDefinitionData selectedChallenge;
    private int selectedNormalIndex = -1;
    private int unlockedNormalBattleCount = 1;
    private bool lastMatchGrantedFirstClearReward = false;

    void Start()
    {
        BindStaticButtons();
        RebuildBattleLists();
        ShowNormalBattleList();
    }

    private void BindStaticButtons()
    {
        if (normalModeButton != null)
        {
            normalModeButton.onClick.RemoveAllListeners();
            normalModeButton.onClick.AddListener(ShowNormalBattleList);
        }

        if (challengeModeButton != null)
        {
            challengeModeButton.onClick.RemoveAllListeners();
            challengeModeButton.onClick.AddListener(ShowChallengeBattleList);
        }

        if (specialModeButton != null)
        {
            specialModeButton.onClick.RemoveAllListeners();
            specialModeButton.onClick.AddListener(ShowSpecialBattleList);
        }

        if (startMatchButton != null)
        {
            startMatchButton.onClick.RemoveAllListeners();
            startMatchButton.onClick.AddListener(StartSelectedMatch);
        }

        if (backToLobbyButton != null)
        {
            backToLobbyButton.onClick.RemoveAllListeners();
            backToLobbyButton.onClick.AddListener(ShowLobbyRoot);
        }

        if (retryMatchButton != null)
        {
            retryMatchButton.onClick.RemoveAllListeners();
            retryMatchButton.onClick.AddListener(RetryCurrentBattle);
        }

        if (returnToLobbyButton != null)
        {
            returnToLobbyButton.onClick.RemoveAllListeners();
            returnToLobbyButton.onClick.AddListener(ShowLobbyRoot);
        }

        if (nextNormalBattleButton != null)
        {
            nextNormalBattleButton.onClick.RemoveAllListeners();
            nextNormalBattleButton.onClick.AddListener(OpenNextNormalBattle);
        }

        if (openProficiencyButton != null)
        {
            openProficiencyButton.onClick.RemoveAllListeners();
            openProficiencyButton.onClick.AddListener(ToggleProficiencyPanel);
        }
    }

    private void RebuildBattleLists()
    {
        RebuildNormalBattleButtons();
        RebuildChallengeButtons();
        ForceRebuildAllMainLayouts();
    }

    private void RebuildNormalBattleButtons()
    {
        int i;

        ClearChildren(normalListContainer);

        for (i = 0; i < normalBattlePresets.Count; i++)
        {
            GameObject buttonObject;
            ArenaBattleListButtonUI buttonUI;
            string status;
            bool unlocked;

            if (battleListButtonPrefab == null || normalListContainer == null || normalBattlePresets[i] == null)
            {
                continue;
            }

            unlocked = i < unlockedNormalBattleCount;
            status = unlocked ? "Available" : "Locked";

            buttonObject = Instantiate(battleListButtonPrefab, normalListContainer);
            buttonUI = buttonObject.GetComponent<ArenaBattleListButtonUI>();

            if (buttonUI == null)
            {
                continue;
            }

            buttonUI.SetupNormal(
                this,
                i,
                "Normal Battle " + (i + 1),
                normalBattlePresets[i].GetMatchName(),
                status,
                unlocked
            );
        }
    }

    private void RebuildChallengeButtons()
    {
        int i;

        ClearChildren(challengeListContainer);

        for (i = 0; i < familyChallenges.Count; i++)
        {
            GameObject buttonObject;
            ArenaBattleListButtonUI buttonUI;
            string status;

            if (battleListButtonPrefab == null || challengeListContainer == null || familyChallenges[i] == null)
            {
                continue;
            }

            if (clearedFamilyIds.Contains(familyChallenges[i].familyId))
            {
                status = "First win achieved";
            }
            else
            {
                status = "Rewards for first win";
            }

            buttonObject = Instantiate(battleListButtonPrefab, challengeListContainer);
            buttonUI = buttonObject.GetComponent<ArenaBattleListButtonUI>();

            if (buttonUI == null)
            {
                continue;
            }

            buttonUI.SetupChallenge(
                this,
                i,
                familyChallenges[i].GetDisplayName(),
                familyChallenges[i].description,
                status,
                true
            );
        }
    }

    public void SelectNormalBattle(int index)
    {
        if (index < 0 || index >= normalBattlePresets.Count)
        {
            return;
        }

        if (index >= unlockedNormalBattleCount)
        {
            return;
        }

        selectedNormalIndex = index;
        selectedChallenge = null;
        selectedPreset = normalBattlePresets[index];

        OpenPreMatchForSelectedBattle();
    }

    public void SelectFamilyChallenge(int index)
    {
        if (index < 0 || index >= familyChallenges.Count)
        {
            return;
        }

        if (familyChallenges[index] == null)
        {
            return;
        }

        selectedNormalIndex = -1;
        selectedChallenge = familyChallenges[index];
        selectedPreset = familyChallenges[index].matchPreset;

        OpenPreMatchForSelectedBattle();
    }

    private void OpenPreMatchForSelectedBattle()
    {
        if (selectedPreset == null)
        {
            return;
        }

        if (matchTitleText != null)
        {
            if (selectedChallenge != null)
            {
                matchTitleText.text = "Challenge Battle - " + selectedChallenge.GetDisplayName();
            }
            else
            {
                matchTitleText.text = "Normal Battle - " + selectedPreset.GetMatchName();
            }
        }

        if (ruleInfoText != null)
        {
            ruleInfoText.text =
                selectedPreset.playerFighterCount + " VS " + selectedPreset.enemyFighterCount;
        }

        RebuildPlayerLoadoutRows(selectedPreset);
        BuildEnemyPreviewCache(selectedPreset);
        RebuildEnemyPreviewRows();

        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(false);
        }

        if (preMatchPanel != null)
        {
            preMatchPanel.SetActive(true);
        }

        if (battleHudRoot != null)
        {
            battleHudRoot.SetActive(false);
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        if (proficiencyPanelRoot != null)
        {
            proficiencyPanelRoot.SetActive(false);
        }

        if (matchSetup != null)
        {
            matchSetup.ClearCurrentCombatants();
        }
    }

    private void RebuildPlayerLoadoutRows(ArenaMatchPresetData preset)
    {
        ArenaTeamRosterPresetData playerPreset;
        List<ArenaMatchFighterEntry> entries;
        List<GladiatorProfileData> profiles;
        int i;

        ClearLoadoutRows();

        if (preset == null)
        {
            return;
        }

        playerPreset = preset.playerRosterPreset;

        if (playerPreset == null)
        {
            return;
        }

        entries = BuildPlayerEntriesFromPreset(playerPreset, 999);
        profiles = new List<GladiatorProfileData>();

        for (i = 0; i < entries.Count; i++)
        {
            GameObject rowObject;
            ArenaLoadoutRowUI rowUI;
            string summary;
            bool shouldSelect;
            bool locked;

            if (loadoutRowContainer == null || loadoutRowPrefab == null)
            {
                return;
            }

            rowObject = Instantiate(loadoutRowPrefab, loadoutRowContainer);
            rowUI = rowObject.GetComponent<ArenaLoadoutRowUI>();

            if (rowUI == null)
            {
                continue;
            }

            summary = BuildRowSummary(entries[i].gladiatorProfile);
            rowUI.Setup(entries[i], summary, true, TeamVisualColor.Blue);
            activeRows.Add(rowUI);

            locked = entries[i].spawnAsPlayerControlled;
            shouldSelect = locked || i < preset.playerFighterCount;

            rowUI.SetSelectionState(shouldSelect, locked);

            if (entries[i].gladiatorProfile != null && !profiles.Contains(entries[i].gladiatorProfile))
            {
                profiles.Add(entries[i].gladiatorProfile);
            }
        }

        if (proficiencyPanel != null)
        {
            proficiencyPanel.SetProfiles(profiles);
        }

        ForceRebuildLayout(loadoutRowContainer);
    }

    private string BuildRowSummary(GladiatorProfileData profile)
    {
        if (profile == null || progressionManager == null)
        {
            return string.Empty;
        }

        progressionManager.EnsureProfileInitialized(profile);

        return
            "One-handed " + progressionManager.GetLevel(profile, GladiatorProficiencyType.OneHanded) +
            "   Polearm " + progressionManager.GetLevel(profile, GladiatorProficiencyType.Polearm) +
            "   Bow " + progressionManager.GetLevel(profile, GladiatorProficiencyType.Bow) +
            "   Running " + progressionManager.GetLevel(profile, GladiatorProficiencyType.Running) +
            "   Shield " + progressionManager.GetLevel(profile, GladiatorProficiencyType.Shield);
    }

    private List<ArenaMatchFighterEntry> BuildPlayerEntriesFromPreset(
        ArenaTeamRosterPresetData preset,
        int desiredCount
    )
    {
        List<ArenaMatchFighterEntry> result;
        int i;

        result = new List<ArenaMatchFighterEntry>();

        if (preset == null || preset.fighters == null)
        {
            return result;
        }

        for (i = 0; i < preset.fighters.Count; i++)
        {
            ArenaMatchFighterEntry entryCopy;

            if (preset.fighters[i] == null)
            {
                continue;
            }

            if (!preset.fighters[i].includeInMatch)
            {
                continue;
            }

            entryCopy = new ArenaMatchFighterEntry();
            entryCopy.includeInMatch = true;
            entryCopy.gladiatorProfile = preset.fighters[i].gladiatorProfile;
            entryCopy.selectedLoadout = preset.fighters[i].selectedLoadout;
            entryCopy.spawnAsPlayerControlled = preset.fighters[i].spawnAsPlayerControlled;

            result.Add(entryCopy);

            if (result.Count >= desiredCount)
            {
                break;
            }
        }

        return result;
    }

    private void BuildEnemyPreviewCache(ArenaMatchPresetData preset)
    {
        cachedEnemyPreviewRoster.Clear();
        cachedEnemyPreviewTheme = null;

        if (preset == null || matchSetup == null)
        {
            return;
        }

        matchSetup.BuildPreviewEnemyData(
            preset,
            out cachedEnemyPreviewRoster,
            out cachedEnemyPreviewTheme
        );
    }

    private void RebuildEnemyPreviewRows()
    {
        int i;

        ClearChildren(enemyPreviewContainer);

        if (enemyPreviewContainer == null || enemyPreviewCardPrefab == null)
        {
            return;
        }

        for (i = 0; i < cachedEnemyPreviewRoster.Count; i++)
        {
            GameObject rowObject;
            ArenaLoadoutRowUI rowUI;
            string summary;
            TeamVisualColor previewColor;

            if (cachedEnemyPreviewRoster[i] == null)
            {
                continue;
            }

            rowObject = Instantiate(enemyPreviewCardPrefab, enemyPreviewContainer);
            rowUI = rowObject.GetComponent<ArenaLoadoutRowUI>();

            if (rowUI == null)
            {
                continue;
            }

            summary = BuildRowSummary(cachedEnemyPreviewRoster[i].gladiatorProfile);
            previewColor = TeamVisualColor.Blue;

            if (cachedEnemyPreviewTheme != null)
            {
                previewColor = cachedEnemyPreviewTheme.GetTeamColor();
            }

            rowUI.Setup(
                cachedEnemyPreviewRoster[i],
                summary,
                false,
                previewColor
            );
        }

        ForceRebuildLayout(enemyPreviewContainer);
    }

    public void StartSelectedMatch()
    {
        List<ArenaMatchFighterEntry> runtimePlayerRoster;

        if (selectedPreset == null || matchSetup == null)
        {
            return;
        }

        runtimePlayerRoster = BuildRuntimePlayerRosterFromRows();
        lastMatchGrantedFirstClearReward = false;

        if (runtimePlayerRoster.Count != selectedPreset.playerFighterCount)
        {
            if (ruleInfoText != null)
            {
                ruleInfoText.text =
                    "Please select exactly " + selectedPreset.playerFighterCount +
                    " fighters.\nPlayer leader must be included.";
            }

            return;
        }

        matchSetup.BuildConfiguredMatch(
            selectedPreset,
            runtimePlayerRoster,
            cachedEnemyPreviewRoster,
            cachedEnemyPreviewTheme
            );

        if (progressionManager != null)
        {
            progressionManager.ApplyProficiencyBonusesToSpawnedPlayerSide(combatantRoot);
        }

        if (eventSpawner != null)
        {
            eventSpawner.BeginMatchCycle();
        }

        if (preMatchPanel != null)
        {
            preMatchPanel.SetActive(false);
        }

        if (battleHudRoot != null)
        {
            battleHudRoot.SetActive(true);
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        if (proficiencyPanelRoot != null)
        {
            proficiencyPanelRoot.SetActive(false);
        }

        StartCoroutine(WatchMatchEnd(runtimePlayerRoster));
    }

    private List<ArenaMatchFighterEntry> BuildRuntimePlayerRosterFromRows()
    {
        List<ArenaMatchFighterEntry> result;
        int i;

        result = new List<ArenaMatchFighterEntry>();

        for (i = 0; i < activeRows.Count; i++)
        {
            ArenaMatchFighterEntry entry;

            if (activeRows[i] == null)
            {
                continue;
            }

            if (!activeRows[i].GetIsSelectedForMatch())
            {
                continue;
            }

            entry = activeRows[i].BuildRuntimeEntry();

            if (entry != null && entry.includeInMatch)
            {
                result.Add(entry);
            }
        }

        return result;
    }

    private IEnumerator WatchMatchEnd(List<ArenaMatchFighterEntry> runtimePlayerRoster)
    {
        bool playerWon;
        string rewardText;

        while (matchManager != null && !matchManager.GetMatchEnded())
        {
            yield return null;
        }

        if (eventSpawner != null)
        {
            eventSpawner.EndMatchCycle();
        }

        playerWon = false;

        if (matchManager != null)
        {
            playerWon = matchManager.GetLastPlayerWon();
        }

        lastProgressGainSummary = "";

        if (progressionManager != null && matchManager != null)
        {
            lastProgressGainSummary = progressionManager.AwardPostMatchExperienceAndBuildSummary(
                runtimePlayerRoster,
                playerWon,
                matchManager.GetPlayerSideKills()
            );
        }

        rewardText = string.Empty;

        if (playerWon)
        {
            if (selectedNormalIndex >= 0)
            {
                unlockedNormalBattleCount = Mathf.Max(unlockedNormalBattleCount, selectedNormalIndex + 2);
            }

            if (selectedChallenge != null)
            {
                if (!clearedFamilyIds.Contains(selectedChallenge.familyId))
                {
                    clearedFamilyIds.Add(selectedChallenge.familyId);
                    lastMatchGrantedFirstClearReward = true;
                    rewardText = selectedChallenge.firstClearRewardText;
                }
            }
        }

        RebuildBattleLists();
        ShowResultPanel(playerWon, rewardText);
    }

    private void ShowResultPanel(bool playerWon, string rewardText)
    {
        if (battleHudRoot != null)
        {
            battleHudRoot.SetActive(false);
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        if (resultSummaryText != null && matchManager != null)
        {
            resultSummaryText.text =
                (playerWon ? "VICTORY" : "DEFEAT") +
                "\n\nSCORE: " + matchManager.GetPerformanceScore() +
                "\nKILLS: " + matchManager.GetPlayerSideKills() +
                " - " + matchManager.GetEnemySideKills();

            if (!string.IsNullOrEmpty(rewardText))
            {
                resultSummaryText.text = resultSummaryText.text + "\nFirst win reward: " + rewardText;
            }
            else if (playerWon && selectedChallenge != null && clearedFamilyIds.Contains(selectedChallenge.familyId) && !lastMatchGrantedFirstClearReward)
            {
                resultSummaryText.text = resultSummaryText.text + "\nThe family's first win reward has been won.";
            }
        }

        if (!string.IsNullOrEmpty(lastProgressGainSummary))
        {
            resultSummaryText.text = resultSummaryText.text + "\n\nProficiency Upgrade:\n" + lastProgressGainSummary;
        }

        if (nextNormalBattleButton != null)
        {
            nextNormalBattleButton.gameObject.SetActive(playerWon && selectedNormalIndex >= 0 && selectedNormalIndex + 1 < unlockedNormalBattleCount && selectedNormalIndex + 1 < normalBattlePresets.Count);
        }
    }

    public void ToggleProficiencyPanel()
    {
        if (proficiencyPanelRoot == null)
        {
            return;
        }

        proficiencyPanelRoot.SetActive(!proficiencyPanelRoot.activeSelf);
    }

    public void CloseProficiencyPanel()
    {
        if (proficiencyPanelRoot == null)
        {
            return;
        }

        proficiencyPanelRoot.SetActive(false);
    }

    private void RetryCurrentBattle()
    {
        OpenPreMatchForSelectedBattle();
    }

    private void OpenNextNormalBattle()
    {
        int nextIndex;

        nextIndex = selectedNormalIndex + 1;

        if (nextIndex < 0 || nextIndex >= normalBattlePresets.Count)
        {
            ShowNormalBattleList();
            return;
        }

        if (nextIndex >= unlockedNormalBattleCount)
        {
            ShowNormalBattleList();
            return;
        }

        SelectNormalBattle(nextIndex);
    }

    private void ShowNormalBattleList()
    {
        if (matchSetup != null)
        {
            matchSetup.ClearCurrentCombatants();
        }

        if (eventSpawner != null)
        {
            eventSpawner.EndMatchCycle();
        }

        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(true);
        }

        if (normalListPanel != null)
        {
            normalListPanel.SetActive(true);
        }

        if (challengeListPanel != null)
        {
            challengeListPanel.SetActive(false);
        }

        if (specialListPanel != null)
        {
            specialListPanel.SetActive(false);
        }

        if (preMatchPanel != null)
        {
            preMatchPanel.SetActive(false);
        }

        if (battleHudRoot != null)
        {
            battleHudRoot.SetActive(false);
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        if (proficiencyPanelRoot != null)
        {
            proficiencyPanelRoot.SetActive(false);
        }
    }

    private void ShowChallengeBattleList()
    {
        if (matchSetup != null)
        {
            matchSetup.ClearCurrentCombatants();
        }

        if (eventSpawner != null)
        {
            eventSpawner.EndMatchCycle();
        }

        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(true);
        }

        if (normalListPanel != null)
        {
            normalListPanel.SetActive(false);
        }

        if (challengeListPanel != null)
        {
            challengeListPanel.SetActive(true);
        }

        if (specialListPanel != null)
        {
            specialListPanel.SetActive(false);
        }

        if (preMatchPanel != null)
        {
            preMatchPanel.SetActive(false);
        }

        if (battleHudRoot != null)
        {
            battleHudRoot.SetActive(false);
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        if (proficiencyPanelRoot != null)
        {
            proficiencyPanelRoot.SetActive(false);
        }
    }

    private void ShowSpecialBattleList()
    {
        if (matchSetup != null)
        {
            matchSetup.ClearCurrentCombatants();
        }

        if (eventSpawner != null)
        {
            eventSpawner.EndMatchCycle();
        }

        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(true);
        }

        if (normalListPanel != null)
        {
            normalListPanel.SetActive(false);
        }

        if (challengeListPanel != null)
        {
            challengeListPanel.SetActive(false);
        }

        if (specialListPanel != null)
        {
            specialListPanel.SetActive(true);
        }

        if (preMatchPanel != null)
        {
            preMatchPanel.SetActive(false);
        }

        if (battleHudRoot != null)
        {
            battleHudRoot.SetActive(false);
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        if (proficiencyPanelRoot != null)
        {
            proficiencyPanelRoot.SetActive(false);
        }
    }

    private void ShowLobbyRoot()
    {
        if (matchSetup != null)
        {
            matchSetup.ClearCurrentCombatants();
        }

        if (eventSpawner != null)
        {
            eventSpawner.EndMatchCycle();
        }

        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(true);
        }

        if (normalListPanel != null)
        {
            normalListPanel.SetActive(false);
        }

        if (challengeListPanel != null)
        {
            challengeListPanel.SetActive(false);
        }

        if (specialListPanel != null)
        {
            specialListPanel.SetActive(false);
        }

        if (preMatchPanel != null)
        {
            preMatchPanel.SetActive(false);
        }

        if (battleHudRoot != null)
        {
            battleHudRoot.SetActive(false);
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        if (proficiencyPanelRoot != null)
        {
            proficiencyPanelRoot.SetActive(false);
        }
    }

    private void ForceRebuildLayout(Transform root)
    {
        RectTransform rect;

        if (root == null)
        {
            return;
        }

        rect = root as RectTransform;

        if (rect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

    private void ForceRebuildAllMainLayouts()
    {
        ForceRebuildLayout(normalListContainer);
        ForceRebuildLayout(challengeListContainer);
        ForceRebuildLayout(loadoutRowContainer);
        ForceRebuildLayout(enemyPreviewContainer);
    }

    private void ClearChildren(Transform parent)
    {
        int i;

        if (parent == null)
        {
            return;
        }

        for (i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    private void ClearLoadoutRows()
    {
        int i;

        for (i = activeRows.Count - 1; i >= 0; i--)
        {
            if (activeRows[i] != null)
            {
                Destroy(activeRows[i].gameObject);
            }
        }

        activeRows.Clear();
    }
}
