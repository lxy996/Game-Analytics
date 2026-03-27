using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ArenaMatchPreset", menuName = "Game/Arena Match Preset")]
public class ArenaMatchPresetData : ScriptableObject
{
    [Header("Identity")]
    public string matchName;

    [Header("Team Sizes")]
    public int playerFighterCount = 3;
    public int enemyFighterCount = 3;

    [Header("Player Source")]
    public ArenaTeamRosterPresetData playerRosterPreset;

    [Header("Enemy Generation")]
    public EnemyRosterGenerationMode enemyRosterMode = EnemyRosterGenerationMode.RandomTeamPreset;

    [Header("Enemy Team Preset Pool")]
    public List<ArenaTeamRosterPresetData> enemyTeamPresetPool = new List<ArenaTeamRosterPresetData>();

    [Header("Enemy Gladiator Pool")]
    public List<GladiatorProfileData> enemyGladiatorPool = new List<GladiatorProfileData>();

    [Header("Enemy Theme Pool")]
    public bool randomizeEnemyTheme = true;
    public List<ArenaTeamThemeData> enemyThemePool = new List<ArenaTeamThemeData>();

    [Header("Enemy Random Rules")]
    public bool allowDuplicateEnemyProfiles = false;
    public bool randomizeEnemyLoadouts = true;

    public string GetMatchName()
    {
        if (string.IsNullOrEmpty(matchName))
        {
            return name;
        }

        return matchName;
    }
}