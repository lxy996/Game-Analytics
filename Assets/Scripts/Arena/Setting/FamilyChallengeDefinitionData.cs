using UnityEngine;

[CreateAssetMenu(fileName = "FamilyChallenge", menuName = "Game/Family Challenge")]
public class FamilyChallengeDefinitionData : ScriptableObject
{
    public string familyId;
    public string displayName;
    [TextArea(2, 4)] public string description;
    [TextArea(2, 4)] public string firstClearRewardText;

    public ArenaMatchPresetData matchPreset;

    public string GetDisplayName()
    {
        if (string.IsNullOrEmpty(displayName))
        {
            return name;
        }

        return displayName;
    }
}
