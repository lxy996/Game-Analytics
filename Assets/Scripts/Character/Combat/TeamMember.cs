using UnityEngine;

public class TeamMember : MonoBehaviour
{
    [SerializeField] private ArenaTeam team = ArenaTeam.PlayerSide;
    [SerializeField] private bool countsAsCombatant = true;

    public ArenaTeam GetTeam()
    {
        return team;
    }

    // Used to determine whether the unit should be counted as a combatant.
    public bool CountsAsCombatant()
    {
        return countsAsCombatant;
    }
}
