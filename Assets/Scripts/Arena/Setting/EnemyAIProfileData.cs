using UnityEngine;

public enum EnemyTacticStyle
{
    Basic,
    IronWall,
    Shadow,
    Berserker,
    Sharpshooter
}

[CreateAssetMenu(fileName = "EnemyAIProfile", menuName = "Game/Enemy AI Profile")]
public class EnemyAIProfileData : ScriptableObject
{
    public EnemyTacticStyle tacticStyle = EnemyTacticStyle.Basic;

    [Header("Targeting")]
    public bool prioritizePlayerCharacter = false;

    [Header("Guard")]
    public bool enableAutoGuard = false;
    public float guardEnterDistance = 1.8f;
    public float guardHoldTime = 0.9f;
    public float lowHealthGuardThreshold = 0.45f;
    public float guardChance = 0.45f;

    [Header("Spacing")]
    public float rangedPreferredDistance = 4f;
    public float retreatDistance = 2f;

    [Header("Aggression")]
    public float dashAggressionMultiplier = 1f;

    [Header("AI Senses")]
    public float pickupSearchRadius = 6f;
    public float hazardAvoidDistance = 2.5f;
}
