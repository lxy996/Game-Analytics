using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ArenaMatchFighterEntry
{
    public bool includeInMatch = true;
    public GladiatorProfileData gladiatorProfile;
    public WeaponLoadoutData selectedLoadout;
    public bool spawnAsPlayerControlled = false;
}

[System.Serializable]
public class ArenaFormationSettings
{
    [Header("Column Distance From Center")]
    public float frontLineDistance = 3f;
    public float middleLineDistance = 5f;
    public float backLineDistance = 7f;

    [Header("Spacing")]
    public float verticalSpacing = 5f;

    [Header("Offsets")]
    public float depthOffset = 0f;
    public float centerYOffset = 0f;
}

public class ArenaMatchSetup : MonoBehaviour
{
    private enum FormationLane
    {
        Frontline,
        Midline,
        Backline
    }

    private class ResolvedFighterEntry
    {
        public ArenaMatchFighterEntry sourceEntry;
        public WeaponLoadoutData resolvedLoadout;
        public bool spawnAsPlayerControlled;
    }

    [Header("Flow")]
    [SerializeField] private bool buildMatchOnStart = true;

    [Header("Preset Mode")]
    [SerializeField] private bool useMatchPreset = true;
    [SerializeField] private ArenaMatchPresetData matchPreset;

    [Header("References")]
    [SerializeField] private ArenaMatchManager matchManager;
    [SerializeField] private Transform combatantRoot;

    [Header("Base Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject allyPrefab;
    [SerializeField] private GameObject enemyPrefab;

    [Header("Team Themes")]
    [SerializeField] private ArenaTeamThemeData playerTeamTheme;
    [SerializeField] private ArenaTeamThemeData enemyTeamTheme;

    [Header("Tower Anchors")]
    [SerializeField] private Transform leftTowerAnchor;
    [SerializeField] private Transform rightTowerAnchor;

    [Header("Player Side Formation")]
    [SerializeField] private ArenaFormationSettings playerFormation = new ArenaFormationSettings();

    [Header("Enemy Side Formation")]
    [SerializeField] private ArenaFormationSettings enemyFormation = new ArenaFormationSettings();

    [Header("Player Side Roster")]
    [SerializeField] private List<ArenaMatchFighterEntry> playerSideRoster = new List<ArenaMatchFighterEntry>();

    [Header("Enemy Side Roster")]
    [SerializeField] private List<ArenaMatchFighterEntry> enemySideRoster = new List<ArenaMatchFighterEntry>();

    private readonly List<GameObject> spawnedCombatants = new List<GameObject>();
    private GameObject currentLeftTower;
    private GameObject currentRightTower;
    private EnemyAIProfileData runtimeEnemyAIProfile;

    void Start()
    {
        if (!buildMatchOnStart)
        {
            return;
        }

        BuildMatch();
    }

    [ContextMenu("Build Match")]
    public void BuildMatch()
    {
        List<ArenaMatchFighterEntry> resolvedPlayerRoster;
        List<ArenaMatchFighterEntry> resolvedEnemyRoster;
        ArenaTeamThemeData resolvedPlayerTheme;
        ArenaTeamThemeData resolvedEnemyTheme;
        ArenaFormationSettings resolvedPlayerFormation;
        ArenaFormationSettings resolvedEnemyFormation;
        Health playerHealthReference;

        // Resolve the current match configuration.
        // If a match preset is enabled, use preset data first.
        // Otherwise, fall back to the manually configured roster/theme/formation.
        ResolveCurrentMatchConfiguration(
            out resolvedPlayerRoster,
            out resolvedEnemyRoster,
            out resolvedPlayerTheme,
            out resolvedEnemyTheme,
            out resolvedPlayerFormation,
            out resolvedEnemyFormation
        );

        ClearSpawnedCombatants();
        ReplaceSideTower(leftTowerAnchor, resolvedPlayerTheme, ref currentLeftTower);
        ReplaceSideTower(rightTowerAnchor, resolvedEnemyTheme, ref currentRightTower);

        playerHealthReference = SpawnSide(
            ArenaTeam.PlayerSide,
            resolvedPlayerRoster,
            resolvedPlayerFormation,
            resolvedPlayerTheme
        );

        SpawnSide(
            ArenaTeam.EnemySide,
            resolvedEnemyRoster,
            resolvedEnemyFormation,
            resolvedEnemyTheme
        );

        if (matchManager != null)
        {
            matchManager.SetCombatantSearchRoot(combatantRoot);
            matchManager.BeginMatch(playerHealthReference);
        }
    }

    public void BuildConfiguredMatch(ArenaMatchPresetData runtimePreset, List<ArenaMatchFighterEntry> runtimePlayerRoster)
    {
        List<ArenaMatchFighterEntry> resolvedPlayerRoster;
        List<ArenaMatchFighterEntry> resolvedEnemyRoster;
        ArenaTeamThemeData resolvedPlayerTheme;
        ArenaTeamThemeData resolvedEnemyTheme;
        ArenaFormationSettings resolvedPlayerFormation;
        ArenaFormationSettings resolvedEnemyFormation;
        Health playerHealthReference;

        resolvedPlayerRoster = runtimePlayerRoster;
        resolvedEnemyRoster = CloneRosterEntries(enemySideRoster);
        resolvedPlayerTheme = playerTeamTheme;
        resolvedEnemyTheme = enemyTeamTheme;
        resolvedPlayerFormation = CloneFormation(playerFormation);
        resolvedEnemyFormation = CloneFormation(enemyFormation);

        if (runtimePreset != null)
        {
            if (runtimePreset.playerRosterPreset != null && runtimePreset.playerRosterPreset.defaultTheme != null)
            {
                resolvedPlayerTheme = runtimePreset.playerRosterPreset.defaultTheme;
            }

            ResolveEnemyConfigurationFromPreset(
                runtimePreset,
                ref resolvedEnemyRoster,
                ref resolvedEnemyTheme,
                ref resolvedEnemyFormation,
                out _
            );
        }

        ClearSpawnedCombatants();
        ReplaceSideTower(leftTowerAnchor, resolvedPlayerTheme, ref currentLeftTower);
        ReplaceSideTower(rightTowerAnchor, resolvedEnemyTheme, ref currentRightTower);

        playerHealthReference = SpawnSide(
            ArenaTeam.PlayerSide,
            resolvedPlayerRoster,
            resolvedPlayerFormation,
            resolvedPlayerTheme
        );

        SpawnSide(
            ArenaTeam.EnemySide,
            resolvedEnemyRoster,
            resolvedEnemyFormation,
            resolvedEnemyTheme
        );

        if (matchManager != null)
        {
            matchManager.SetCombatantSearchRoot(combatantRoot);
            matchManager.BeginMatch(playerHealthReference);
        }
    }

    // Resolve all data needed for this match.
    // Manual inspector values are still kept as a fallback.
    private void ResolveCurrentMatchConfiguration(
        out List<ArenaMatchFighterEntry> resolvedPlayerRoster,
        out List<ArenaMatchFighterEntry> resolvedEnemyRoster,
        out ArenaTeamThemeData resolvedPlayerTheme,
        out ArenaTeamThemeData resolvedEnemyTheme,
        out ArenaFormationSettings resolvedPlayerFormation,
        out ArenaFormationSettings resolvedEnemyFormation
    )
    {
        ArenaTeamRosterPresetData chosenEnemyPreset;

        resolvedPlayerRoster = CloneRosterEntries(playerSideRoster);
        resolvedEnemyRoster = CloneRosterEntries(enemySideRoster);
        resolvedPlayerTheme = playerTeamTheme;
        resolvedEnemyTheme = enemyTeamTheme;
        resolvedPlayerFormation = CloneFormation(playerFormation);
        resolvedEnemyFormation = CloneFormation(enemyFormation);
        chosenEnemyPreset = null;
        runtimeEnemyAIProfile = null;

        if (!useMatchPreset || matchPreset == null)
        {
            return;
        }

        ResolvePlayerConfigurationFromPreset(
            matchPreset,
            ref resolvedPlayerRoster,
            ref resolvedPlayerTheme,
            ref resolvedPlayerFormation
        );

        ResolveEnemyConfigurationFromPreset(
            matchPreset,
            ref resolvedEnemyRoster,
            ref resolvedEnemyTheme,
            ref resolvedEnemyFormation,
            out chosenEnemyPreset
        );
    }

    // Use player preset data if provided by the match preset.
    // If there is no player roster preset, keep the manual player roster and trim it to the required fighter count.
    private void ResolvePlayerConfigurationFromPreset(
        ArenaMatchPresetData preset,
        ref List<ArenaMatchFighterEntry> resolvedPlayerRoster,
        ref ArenaTeamThemeData resolvedPlayerTheme,
        ref ArenaFormationSettings resolvedPlayerFormation
    )
    {
        if (preset == null)
        {
            return;
        }

        if (preset.playerRosterPreset != null)
        {
            resolvedPlayerRoster = BuildRosterFromTeamPreset(
                preset.playerRosterPreset,
                preset.playerFighterCount,
                false
            );

            if (preset.playerRosterPreset.defaultTheme != null)
            {
                resolvedPlayerTheme = preset.playerRosterPreset.defaultTheme;
            }

            if (preset.playerRosterPreset.overrideFormation)
            {
                resolvedPlayerFormation = CloneFormation(preset.playerRosterPreset.formationOverride);
            }
        }
        else
        {
            TrimRosterToCount(resolvedPlayerRoster, preset.playerFighterCount, false);
        }
    }

    // Build enemy data based on the selected random generation mode.
    // Supports:
    // 1. Random team preset
    // 2. Random gladiators from profile pool
    // 3. Hybrid of team preset and gladiator pool
    private void ResolveEnemyConfigurationFromPreset(
        ArenaMatchPresetData preset,
        ref List<ArenaMatchFighterEntry> resolvedEnemyRoster,
        ref ArenaTeamThemeData resolvedEnemyTheme,
        ref ArenaFormationSettings resolvedEnemyFormation,
        out ArenaTeamRosterPresetData chosenEnemyPreset
    )
    {
        chosenEnemyPreset = null;
        runtimeEnemyAIProfile = null;

        if (preset == null)
        {
            return;
        }

        chosenEnemyPreset = ChooseRandomTeamPreset(preset.enemyTeamPresetPool);
        if (chosenEnemyPreset != null)
        {
            runtimeEnemyAIProfile = chosenEnemyPreset.enemyAIProfile;
        }

        if (preset.enemyRosterMode == EnemyRosterGenerationMode.RandomTeamPreset)
        {
            if (chosenEnemyPreset != null)
            {
                resolvedEnemyRoster = BuildRosterFromTeamPreset(
                    chosenEnemyPreset,
                    preset.enemyFighterCount,
                    preset.randomizeEnemyLoadouts
                );

                if (chosenEnemyPreset.overrideFormation)
                {
                    resolvedEnemyFormation = CloneFormation(chosenEnemyPreset.formationOverride);
                }
            }
            else
            {
                resolvedEnemyRoster = BuildRandomRosterFromGladiatorPool(
                    preset.enemyGladiatorPool,
                    preset.enemyFighterCount,
                    preset.allowDuplicateEnemyProfiles,
                    preset.randomizeEnemyLoadouts
                );
            }
        }
        else if (preset.enemyRosterMode == EnemyRosterGenerationMode.RandomGladiators)
        {
            resolvedEnemyRoster = BuildRandomRosterFromGladiatorPool(
                preset.enemyGladiatorPool,
                preset.enemyFighterCount,
                preset.allowDuplicateEnemyProfiles,
                preset.randomizeEnemyLoadouts
            );
        }
        else if (preset.enemyRosterMode == EnemyRosterGenerationMode.HybridPresetAndRandomGladiators)
        {
            resolvedEnemyRoster = BuildHybridEnemyRoster(
                preset,
                chosenEnemyPreset
            );

            if (chosenEnemyPreset != null && chosenEnemyPreset.overrideFormation)
            {
                resolvedEnemyFormation = CloneFormation(chosenEnemyPreset.formationOverride);
            }
        }

        resolvedEnemyTheme = ResolveEnemyTheme(preset, chosenEnemyPreset, resolvedEnemyTheme);
    }

    // Resolve enemy theme.
    // Random theme has the highest priority if enabled.
    // Otherwise use the chosen enemy preset theme.
    // If neither is available, keep the manual fallback theme.
    private ArenaTeamThemeData ResolveEnemyTheme(
        ArenaMatchPresetData preset,
        ArenaTeamRosterPresetData chosenEnemyPreset,
        ArenaTeamThemeData fallbackTheme
    )
    {
        ArenaTeamThemeData randomTheme;

        if (preset != null && preset.randomizeEnemyTheme)
        {
            randomTheme = ChooseRandomTheme(preset.enemyThemePool);

            if (randomTheme != null)
            {
                return randomTheme;
            }
        }

        if (chosenEnemyPreset != null && chosenEnemyPreset.defaultTheme != null)
        {
            return chosenEnemyPreset.defaultTheme;
        }

        return fallbackTheme;
    }

    // Build a roster directly from a team preset.
    // Optionally randomize loadouts for each fighter if the fighter entry did not manually override loadout.
    private List<ArenaMatchFighterEntry> BuildRosterFromTeamPreset(
        ArenaTeamRosterPresetData preset,
        int desiredCount,
        bool randomizeLoadoutsWhenPossible
    )
    {
        List<ArenaMatchFighterEntry> roster;
        int i;

        roster = new List<ArenaMatchFighterEntry>();

        if (preset == null || preset.fighters == null)
        {
            return roster;
        }

        for (i = 0; i < preset.fighters.Count; i++)
        {
            ArenaMatchFighterEntry copiedEntry;

            if (preset.fighters[i] == null)
            {
                continue;
            }

            if (!preset.fighters[i].includeInMatch)
            {
                continue;
            }

            copiedEntry = CloneEntry(preset.fighters[i]);

            if (randomizeLoadoutsWhenPossible)
            {
                if (copiedEntry.selectedLoadout == null && copiedEntry.gladiatorProfile != null)
                {
                    copiedEntry.selectedLoadout = copiedEntry.gladiatorProfile.GetRandomAvailableLoadout();
                }
            }

            roster.Add(copiedEntry);
        }

        TrimRosterToCount(roster, desiredCount, false);
        return roster;
    }

    // Build an enemy roster directly from the gladiator profile pool.
    private List<ArenaMatchFighterEntry> BuildRandomRosterFromGladiatorPool(
        List<GladiatorProfileData> gladiatorPool,
        int desiredCount,
        bool allowDuplicates,
        bool randomizeLoadouts
    )
    {
        List<ArenaMatchFighterEntry> roster;
        List<GladiatorProfileData> validProfiles;
        int attempts;
        int maxAttempts;

        roster = new List<ArenaMatchFighterEntry>();
        validProfiles = GetValidProfiles(gladiatorPool);

        if (desiredCount <= 0 || validProfiles.Count == 0)
        {
            return roster;
        }

        attempts = 0;
        maxAttempts = Mathf.Max(20, desiredCount * 20);

        while (roster.Count < desiredCount && attempts < maxAttempts)
        {
            GladiatorProfileData pickedProfile;
            ArenaMatchFighterEntry newEntry;

            attempts++;

            pickedProfile = validProfiles[Random.Range(0, validProfiles.Count)];

            if (!allowDuplicates && RosterContainsProfile(roster, pickedProfile))
            {
                continue;
            }

            newEntry = CreateEntryFromProfile(pickedProfile, randomizeLoadouts);
            roster.Add(newEntry);

            if (!allowDuplicates && roster.Count >= validProfiles.Count)
            {
                break;
            }
        }

        return roster;
    }

    // Build an enemy roster by mixing:
    // 1. A random team preset
    // 2. The random gladiator profile pool
    // This creates more variation while still preserving some preset team flavor.
    private List<ArenaMatchFighterEntry> BuildHybridEnemyRoster(
        ArenaMatchPresetData preset,
        ArenaTeamRosterPresetData chosenEnemyPreset
    )
    {
        List<ArenaMatchFighterEntry> candidateEntries;
        List<ArenaMatchFighterEntry> finalRoster;
        List<GladiatorProfileData> validProfiles;
        int i;

        candidateEntries = new List<ArenaMatchFighterEntry>();
        finalRoster = new List<ArenaMatchFighterEntry>();

        if (chosenEnemyPreset != null && chosenEnemyPreset.fighters != null)
        {
            for (i = 0; i < chosenEnemyPreset.fighters.Count; i++)
            {
                ArenaMatchFighterEntry copiedEntry;

                if (chosenEnemyPreset.fighters[i] == null)
                {
                    continue;
                }

                if (!chosenEnemyPreset.fighters[i].includeInMatch)
                {
                    continue;
                }

                copiedEntry = CloneEntry(chosenEnemyPreset.fighters[i]);

                if (preset.randomizeEnemyLoadouts)
                {
                    if (copiedEntry.selectedLoadout == null && copiedEntry.gladiatorProfile != null)
                    {
                        copiedEntry.selectedLoadout = copiedEntry.gladiatorProfile.GetRandomAvailableLoadout();
                    }
                }

                candidateEntries.Add(copiedEntry);
            }
        }

        validProfiles = GetValidProfiles(preset.enemyGladiatorPool);

        for (i = 0; i < validProfiles.Count; i++)
        {
            candidateEntries.Add(CreateEntryFromProfile(validProfiles[i], preset.randomizeEnemyLoadouts));
        }

        ShuffleList(candidateEntries);

        for (i = 0; i < candidateEntries.Count; i++)
        {
            ArenaMatchFighterEntry copiedEntry;

            if (finalRoster.Count >= preset.enemyFighterCount)
            {
                break;
            }

            copiedEntry = CloneEntry(candidateEntries[i]);

            if (!preset.allowDuplicateEnemyProfiles)
            {
                if (RosterContainsProfile(finalRoster, copiedEntry.gladiatorProfile))
                {
                    continue;
                }
            }

            finalRoster.Add(copiedEntry);
        }

        if (preset.allowDuplicateEnemyProfiles)
        {
            while (finalRoster.Count < preset.enemyFighterCount && candidateEntries.Count > 0)
            {
                ArenaMatchFighterEntry copiedEntry;

                copiedEntry = CloneEntry(candidateEntries[Random.Range(0, candidateEntries.Count)]);
                finalRoster.Add(copiedEntry);
            }
        }

        return finalRoster;
    }

    // Filter out unusable gladiator profiles.
    // A valid profile must have at least one usable loadout.
    private List<GladiatorProfileData> GetValidProfiles(List<GladiatorProfileData> profilePool)
    {
        List<GladiatorProfileData> validProfiles;
        int i;

        validProfiles = new List<GladiatorProfileData>();

        if (profilePool == null)
        {
            return validProfiles;
        }

        for (i = 0; i < profilePool.Count; i++)
        {
            if (profilePool[i] == null)
            {
                continue;
            }

            if (!profilePool[i].HasAnyAvailableLoadout())
            {
                continue;
            }

            validProfiles.Add(profilePool[i]);
        }

        return validProfiles;
    }

    // Create a fighter entry from a gladiator profile.
    // The loadout can either be randomized or use the default loadout.
    private ArenaMatchFighterEntry CreateEntryFromProfile(
        GladiatorProfileData profile,
        bool randomizeLoadouts
    )
    {
        ArenaMatchFighterEntry entry;

        entry = new ArenaMatchFighterEntry();
        entry.includeInMatch = true;
        entry.gladiatorProfile = profile;
        entry.spawnAsPlayerControlled = false;

        if (profile != null)
        {
            if (randomizeLoadouts)
            {
                entry.selectedLoadout = profile.GetRandomAvailableLoadout();
            }
            else
            {
                entry.selectedLoadout = profile.GetDefaultLoadout();
            }
        }

        return entry;
    }

    private bool RosterContainsProfile(List<ArenaMatchFighterEntry> roster, GladiatorProfileData profile)
    {
        int i;

        if (profile == null || roster == null)
        {
            return false;
        }

        for (i = 0; i < roster.Count; i++)
        {
            if (roster[i] == null)
            {
                continue;
            }

            if (roster[i].gladiatorProfile == profile)
            {
                return true;
            }
        }

        return false;
    }

    // Randomly choose one team preset from the preset pool.
    private ArenaTeamRosterPresetData ChooseRandomTeamPreset(List<ArenaTeamRosterPresetData> presetPool)
    {
        List<ArenaTeamRosterPresetData> validPresets;
        int i;

        validPresets = new List<ArenaTeamRosterPresetData>();

        if (presetPool == null)
        {
            return null;
        }

        for (i = 0; i < presetPool.Count; i++)
        {
            if (presetPool[i] == null)
            {
                continue;
            }

            validPresets.Add(presetPool[i]);
        }

        if (validPresets.Count == 0)
        {
            return null;
        }

        return validPresets[Random.Range(0, validPresets.Count)];
    }

    // Randomly choose one theme from the theme pool.
    private ArenaTeamThemeData ChooseRandomTheme(List<ArenaTeamThemeData> themePool)
    {
        List<ArenaTeamThemeData> validThemes;
        int i;

        validThemes = new List<ArenaTeamThemeData>();

        if (themePool == null)
        {
            return null;
        }

        for (i = 0; i < themePool.Count; i++)
        {
            if (themePool[i] == null)
            {
                continue;
            }

            validThemes.Add(themePool[i]);
        }

        if (validThemes.Count == 0)
        {
            return null;
        }

        return validThemes[Random.Range(0, validThemes.Count)];
    }

    // Clone roster entries so runtime generation does not directly modify the original inspector list.
    private List<ArenaMatchFighterEntry> CloneRosterEntries(List<ArenaMatchFighterEntry> source)
    {
        List<ArenaMatchFighterEntry> result;
        int i;

        result = new List<ArenaMatchFighterEntry>();

        if (source == null)
        {
            return result;
        }

        for (i = 0; i < source.Count; i++)
        {
            if (source[i] == null)
            {
                continue;
            }

            result.Add(CloneEntry(source[i]));
        }

        return result;
    }

    private ArenaMatchFighterEntry CloneEntry(ArenaMatchFighterEntry source)
    {
        ArenaMatchFighterEntry copy;

        copy = new ArenaMatchFighterEntry();

        if (source == null)
        {
            return copy;
        }

        copy.includeInMatch = source.includeInMatch;
        copy.gladiatorProfile = source.gladiatorProfile;
        copy.selectedLoadout = source.selectedLoadout;
        copy.spawnAsPlayerControlled = source.spawnAsPlayerControlled;

        return copy;
    }

    private ArenaFormationSettings CloneFormation(ArenaFormationSettings source)
    {
        ArenaFormationSettings copy;

        copy = new ArenaFormationSettings();

        if (source == null)
        {
            return copy;
        }

        copy.frontLineDistance = source.frontLineDistance;
        copy.middleLineDistance = source.middleLineDistance;
        copy.backLineDistance = source.backLineDistance;
        copy.verticalSpacing = source.verticalSpacing;
        copy.depthOffset = source.depthOffset;
        copy.centerYOffset = source.centerYOffset;

        return copy;
    }

    // Trim the roster to the desired number of fighters.
    // Useful when the preset allows more fighters than this match mode requires.
    private void TrimRosterToCount(
        List<ArenaMatchFighterEntry> roster,
        int desiredCount,
        bool randomizeTrim
    )
    {
        if (roster == null)
        {
            return;
        }

        if (desiredCount <= 0)
        {
            return;
        }

        if (roster.Count <= desiredCount)
        {
            return;
        }

        if (randomizeTrim)
        {
            ShuffleList(roster);
        }

        while (roster.Count > desiredCount)
        {
            roster.RemoveAt(roster.Count - 1);
        }
    }

    // Shuffle a list in place.
    private void ShuffleList<T>(List<T> list)
    {
        int i;

        if (list == null)
        {
            return;
        }

        for (i = 0; i < list.Count; i++)
        {
            int swapIndex;
            T temp;

            swapIndex = Random.Range(i, list.Count);

            temp = list[i];
            list[i] = list[swapIndex];
            list[swapIndex] = temp;
        }
    }

    private Health SpawnSide(
        ArenaTeam team,
        List<ArenaMatchFighterEntry> sourceRoster,
        ArenaFormationSettings formation,
        ArenaTeamThemeData teamTheme
    )
    {
        List<ResolvedFighterEntry> resolvedEntries;
        Dictionary<FormationLane, List<ResolvedFighterEntry>> laneMap;
        Health playerHealthReference;
        int i;

        resolvedEntries = ResolveRoster(team, sourceRoster);
        laneMap = BuildLaneMap();
        playerHealthReference = null;

        // Positions are assigned based on weapon type.
        for (i = 0; i < resolvedEntries.Count; i++)
        {
            FormationLane lane;

            lane = DetermineLane(resolvedEntries[i].resolvedLoadout);
            laneMap[lane].Add(resolvedEntries[i]);
        }

        playerHealthReference = SpawnLane(team, laneMap[FormationLane.Frontline], FormationLane.Frontline, formation, teamTheme, playerHealthReference);
        playerHealthReference = SpawnLane(team, laneMap[FormationLane.Midline], FormationLane.Midline, formation, teamTheme, playerHealthReference);
        playerHealthReference = SpawnLane(team, laneMap[FormationLane.Backline], FormationLane.Backline, formation, teamTheme, playerHealthReference);

        return playerHealthReference;
    }

    private Dictionary<FormationLane, List<ResolvedFighterEntry>> BuildLaneMap()
    {
        Dictionary<FormationLane, List<ResolvedFighterEntry>> map;

        map = new Dictionary<FormationLane, List<ResolvedFighterEntry>>();
        map[FormationLane.Frontline] = new List<ResolvedFighterEntry>();
        map[FormationLane.Midline] = new List<ResolvedFighterEntry>();
        map[FormationLane.Backline] = new List<ResolvedFighterEntry>();

        return map;
    }

    private List<ResolvedFighterEntry> ResolveRoster(ArenaTeam team, List<ArenaMatchFighterEntry> sourceRoster)
    {
        List<ResolvedFighterEntry> resolvedEntries;
        int forcedPlayerIndex;
        int i;

        resolvedEntries = new List<ResolvedFighterEntry>();
        forcedPlayerIndex = -1;

        if (sourceRoster == null)
        {
            return resolvedEntries;
        }

        // Find the pre-selected player character
        if (team == ArenaTeam.PlayerSide)
        {
            for (i = 0; i < sourceRoster.Count; i++)
            {
                if (sourceRoster[i] == null)
                {
                    continue;
                }

                if (!sourceRoster[i].includeInMatch)
                {
                    continue;
                }

                if (sourceRoster[i].spawnAsPlayerControlled)
                {
                    forcedPlayerIndex = i;
                    break;
                }
            }
        }

        // Process weapon data for each character involved in combat.
        for (i = 0; i < sourceRoster.Count; i++)
        {
            ArenaMatchFighterEntry entry;
            WeaponLoadoutData loadout;
            ResolvedFighterEntry resolvedEntry;

            entry = sourceRoster[i];

            if (entry == null)
            {
                continue;
            }

            if (!entry.includeInMatch)
            {
                continue;
            }

            loadout = ResolveLoadout(entry);

            if (loadout == null)
            {
                Debug.LogWarning("Skipped a fighter entry because no loadout could be resolved.");
                continue;
            }

            resolvedEntry = new ResolvedFighterEntry();
            resolvedEntry.sourceEntry = entry;
            resolvedEntry.resolvedLoadout = loadout;
            resolvedEntry.spawnAsPlayerControlled = false;

            if (team == ArenaTeam.PlayerSide)
            {
                if (forcedPlayerIndex >= 0)
                {
                    if (forcedPlayerIndex == i)
                    {
                        resolvedEntry.spawnAsPlayerControlled = true;
                    }
                }
                else if (resolvedEntries.Count == 0)
                {
                    resolvedEntry.spawnAsPlayerControlled = true;
                }
            }

            resolvedEntries.Add(resolvedEntry);
        }

        return resolvedEntries;
    }

    private WeaponLoadoutData ResolveLoadout(ArenaMatchFighterEntry entry)
    {
        if (entry == null)
        {
            return null;
        }

        if (entry.selectedLoadout != null)
        {
            return entry.selectedLoadout;
        }

        if (entry.gladiatorProfile != null)
        {
            return entry.gladiatorProfile.GetDefaultLoadout();
        }

        return null;
    }

    private Health SpawnLane(
        ArenaTeam team,
        List<ResolvedFighterEntry> laneEntries,
        FormationLane lane,
        ArenaFormationSettings formation,
        ArenaTeamThemeData teamTheme,
        Health currentPlayerHealthReference
    )
    {
        int i;
        int count;
        Health playerHealthReference;

        count = laneEntries.Count;
        playerHealthReference = currentPlayerHealthReference;

        for (i = 0; i < count; i++)
        {
            Vector3 spawnPosition;
            GameObject prefabToSpawn;
            GameObject spawnedObject;
            Health spawnedHealth;

            // Decide which prefab to choose: the player prefab or the enemy/ally prefab.
            prefabToSpawn = GetPrefabForEntry(team, laneEntries[i].spawnAsPlayerControlled);

            if (prefabToSpawn == null)
            {
                Debug.LogWarning("Missing prefab for spawned combatant.");
                continue;
            }

            // Calculate the position where the character should stand in this lane.
            spawnPosition = GetSpawnPosition(team, lane, i, count, formation);

            if (combatantRoot != null)
            {
                spawnedObject = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity, combatantRoot);
            }
            else
            {
                spawnedObject = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
            }

            PrepareSpawnedCombatant(
                spawnedObject,
                team,
                laneEntries[i],
                teamTheme
            );

            spawnedCombatants.Add(spawnedObject);

            if (team == ArenaTeam.PlayerSide && laneEntries[i].spawnAsPlayerControlled)
            {
                spawnedHealth = spawnedObject.GetComponent<Health>();

                if (spawnedHealth != null)
                {
                    playerHealthReference = spawnedHealth;
                }
            }
        }

        return playerHealthReference;
    }

    // Assign teams, process tags, switch colors, and load weapon-related data.
    private void PrepareSpawnedCombatant(
        GameObject spawnedObject,
        ArenaTeam team,
        ResolvedFighterEntry resolvedEntry,
        ArenaTeamThemeData teamTheme
    )
    {
        TeamMember teamMember;
        ArenaVisualIdentity visualIdentity;
        WeaponLoadoutApplier loadoutApplier;
        GladiatorInstanceIdentity instanceIdentity;
        string fighterName;
        TeamVisualColor teamColor;
        EnemyController enemyController;

        if (spawnedObject == null)
        {
            return;
        }

        fighterName = BuildFighterName(resolvedEntry);
        spawnedObject.name = fighterName;

        if (team == ArenaTeam.PlayerSide)
        {
            if (resolvedEntry.spawnAsPlayerControlled)
            {
                spawnedObject.tag = "Player";
            }
            else
            {
                spawnedObject.tag = "Teammate";
            }
        }
        else
        {
            spawnedObject.tag = "Enemy";
        }

        teamMember = spawnedObject.GetComponent<TeamMember>();

        if (teamMember != null)
        {
            teamMember.SetTeam(team);
            teamMember.SetCountsAsCombatant(true);
        }

        visualIdentity = spawnedObject.GetComponent<ArenaVisualIdentity>();

        if (visualIdentity != null)
        {
            teamColor = GetThemeColor(teamTheme);
            visualIdentity.SetTeamColor(teamColor);
        }

        loadoutApplier = spawnedObject.GetComponent<WeaponLoadoutApplier>();

        if (loadoutApplier != null)
        {
            loadoutApplier.SetLoadout(resolvedEntry.resolvedLoadout);
        }
       
        enemyController = spawnedObject.GetComponent<EnemyController>();

        if (enemyController != null && team == ArenaTeam.EnemySide)
        {
            enemyController.ApplyAIProfile(runtimeEnemyAIProfile);
        }

        instanceIdentity = spawnedObject.GetComponent<GladiatorInstanceIdentity>();

        if (instanceIdentity != null && resolvedEntry != null && resolvedEntry.sourceEntry != null)
        {
            instanceIdentity.SetIdentity(
                resolvedEntry.sourceEntry.gladiatorProfile,
                resolvedEntry.resolvedLoadout,
                team == ArenaTeam.PlayerSide
            );
        }
    }

    // Set character name
    private string BuildFighterName(ResolvedFighterEntry resolvedEntry)
    {
        string fighterName;

        fighterName = "Combatant";

        if (resolvedEntry != null && resolvedEntry.sourceEntry != null)
        {
            if (resolvedEntry.sourceEntry.gladiatorProfile != null)
            {
                fighterName = resolvedEntry.sourceEntry.gladiatorProfile.GetGladiatorName();
            }
        }

        if (resolvedEntry != null && resolvedEntry.resolvedLoadout != null)
        {
            fighterName = fighterName + " - " + resolvedEntry.resolvedLoadout.name;
        }

        return fighterName;
    }

    // Determine character lane based on weapon type.
    private FormationLane DetermineLane(WeaponLoadoutData loadout)
    {
        if (loadout == null)
        {
            return FormationLane.Frontline;
        }

        if (loadout.weaponType == WeaponType.Ranged)
        {
            return FormationLane.Backline;
        }

        if (loadout.weaponType == WeaponType.Polearm)
        {
            return FormationLane.Midline;
        }

        if (loadout.weaponType == WeaponType.Melee && loadout.hasShield)
        {
            return FormationLane.Frontline;
        }

        return FormationLane.Frontline;
    }

    private Vector3 GetSpawnPosition(
        ArenaTeam team,
        FormationLane lane,
        int laneIndex,
        int laneCount,
        ArenaFormationSettings formation
    )
    {
        float x;
        float y;
        float sideSign;

        sideSign = GetSideSign(team);
        x = GetLaneX(lane, formation, sideSign);
        y = GetLaneY(laneIndex, laneCount, formation);

        return new Vector3(x, y, 0f);
    }

    private float GetLaneX(FormationLane lane, ArenaFormationSettings formation, float sideSign)
    {
        float distanceFromCenter;

        if (lane == FormationLane.Frontline)
        {
            distanceFromCenter = formation.frontLineDistance;
        }
        else if (lane == FormationLane.Midline)
        {
            distanceFromCenter = formation.middleLineDistance;
        }
        else
        {
            distanceFromCenter = formation.backLineDistance;
        }

        return sideSign * (distanceFromCenter + formation.depthOffset);
    }

    private float GetLaneY(int laneIndex, int laneCount, ArenaFormationSettings formation)
    {
        float centeredIndex;

        centeredIndex = laneIndex - ((laneCount - 1) * 0.5f);
        return formation.centerYOffset + centeredIndex * formation.verticalSpacing;
    }

    private float GetSideSign(ArenaTeam team)
    {
        if (team == ArenaTeam.PlayerSide)
        {
            return -1f;
        }

        return 1f;
    }

    private GameObject GetPrefabForEntry(ArenaTeam team, bool spawnAsPlayerControlled)
    {
        if (team == ArenaTeam.PlayerSide)
        {
            if (spawnAsPlayerControlled)
            {
                return playerPrefab;
            }

            return allyPrefab;
        }

        return enemyPrefab;
    }

    private TeamVisualColor GetThemeColor(ArenaTeamThemeData theme)
    {
        if (theme == null)
        {
            return TeamVisualColor.Blue;
        }

        return theme.GetTeamColor();
    }

    private void ReplaceSideTower(Transform anchor, ArenaTeamThemeData theme, ref GameObject currentTower)
    {
        int i;
        GameObject towerPrefab;

        if (anchor == null)
        {
            return;
        }

        for (i = anchor.childCount - 1; i >= 0; i--)
        {
            Destroy(anchor.GetChild(i).gameObject);
        }

        currentTower = null;

        if (theme == null)
        {
            return;
        }

        towerPrefab = theme.GetTowerPrefab();

        if (towerPrefab == null)
        {
            return;
        }

        currentTower = Instantiate(towerPrefab, anchor.position, anchor.rotation, anchor);
    }

    public void ClearCurrentCombatants()
    {
        ClearSpawnedCombatants();
    }

    private void ClearSpawnedCombatants()
    {
        int i;

        for (i = spawnedCombatants.Count - 1; i >= 0; i--)
        {
            if (spawnedCombatants[i] != null)
            {
                Destroy(spawnedCombatants[i]);
            }
        }

        spawnedCombatants.Clear();
    }

}
