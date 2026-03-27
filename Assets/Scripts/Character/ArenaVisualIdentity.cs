using UnityEngine;

public class ArenaVisualIdentity : MonoBehaviour
{
    [SerializeField] private TeamVisualColor teamColor = TeamVisualColor.Blue;

    public void SetTeamColor(TeamVisualColor newColor)
    {
        teamColor = newColor;
    }

    public TeamVisualColor GetTeamColor()
    {
        return teamColor;
    }
}
