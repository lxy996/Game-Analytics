using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ArenaTeamRosterPreset", menuName = "Game/Arena Team Roster Preset")]
public class ArenaTeamRosterPresetData : ScriptableObject
{
    [Header("Identity")]
    public string rosterName;

    [Header("Theme")]
    public ArenaTeamThemeData defaultTheme;

    [Header("Formation Override")]
    public bool overrideFormation = false;
    public ArenaFormationSettings formationOverride = new ArenaFormationSettings();

    [Header("Fighters")]
    public List<ArenaMatchFighterEntry> fighters = new List<ArenaMatchFighterEntry>();

    public string GetRosterName()
    {
        if (string.IsNullOrEmpty(rosterName))
        {
            return name;
        }

        return rosterName;
    }

    public int GetIncludedFighterCount()
    {
        int count;
        int i;

        count = 0;

        if (fighters == null)
        {
            return 0;
        }

        for (i = 0; i < fighters.Count; i++)
        {
            if (fighters[i] == null)
            {
                continue;
            }

            if (!fighters[i].includeInMatch)
            {
                continue;
            }

            count++;
        }

        return count;
    }
}
