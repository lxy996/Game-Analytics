using UnityEngine;

public class TeamMember : MonoBehaviour
{
    [SerializeField] private ArenaTeam team = ArenaTeam.PlayerSide;
    [SerializeField] private bool countsAsCombatant = true;

    public ArenaTeam GetTeam()
    {
        return team;
    }
    public void SetTeam(ArenaTeam newTeam)
    {
        team = newTeam;
    }

    // Used to determine whether the unit should be counted as a combatant.
    public bool CountsAsCombatant()
    {
        return countsAsCombatant;
    }

    public void SetCountsAsCombatant(bool value)
    {
        countsAsCombatant = value;
    }
}
