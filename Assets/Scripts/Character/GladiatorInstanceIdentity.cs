using UnityEngine;

public class GladiatorInstanceIdentity : MonoBehaviour
{
    [SerializeField] private GladiatorProfileData gladiatorProfile;
    [SerializeField] private WeaponLoadoutData selectedLoadout;
    [SerializeField] private bool belongsToPlayerSide;

    public void SetIdentity(GladiatorProfileData profile, WeaponLoadoutData loadout, bool isPlayerSide)
    {
        gladiatorProfile = profile;
        selectedLoadout = loadout;
        belongsToPlayerSide = isPlayerSide;
    }

    public GladiatorProfileData GetGladiatorProfile()
    {
        return gladiatorProfile;
    }

    public WeaponLoadoutData GetSelectedLoadout()
    {
        return selectedLoadout;
    }

    public bool GetBelongsToPlayerSide()
    {
        return belongsToPlayerSide;
    }
}
