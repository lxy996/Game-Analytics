using UnityEngine;

[CreateAssetMenu(fileName = "ArenaTeamTheme", menuName = "Game/Arena Team Theme")]
public class ArenaTeamThemeData : ScriptableObject
{
    public string themeName;
    public TeamVisualColor teamColor = TeamVisualColor.Blue;
    public GameObject towerPrefab;

    public TeamVisualColor GetTeamColor()
    {
        return teamColor;
    }

    public GameObject GetTowerPrefab()
    {
        return towerPrefab;
    }
}
