using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class ArenaMatchManager : MonoBehaviour
{
    [Header("Match Rules")]
    [SerializeField] private float timeLimit = 90f;
    [SerializeField] private bool endOnTeamWipe = true; // End the match when one team is wiped out
    [SerializeField] private bool autoStartWithSceneCombatants = false;

    [Header("Score Rules")]
    [SerializeField] private int winScoreBonus = 100;
    [SerializeField] private int killScoreBonus = 20;
    [SerializeField] private int doubleKillBonus = 10;
    [SerializeField] private int tripleKillBonus = 20;
    [SerializeField] private float streakWindow = 4f; // Maximum kill streak interval
    [SerializeField] private int noDeathBonus = 50;
    [SerializeField] private int noHitBonus = 30;
    [SerializeField] private int maxSpeedBonus = 60;

    [Header("References")]
    [SerializeField] private Health playerHealth;
    [SerializeField] private Transform combatantSearchRoot;

    [Header("UI")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text resultText;

    private List<Health> trackedHealths = new List<Health>(); // Record all the fighters on both sides
    // Link health to fighter's information (TeamMember)
    private Dictionary<Health, TeamMember> memberLookup = new Dictionary<Health, TeamMember>();
    private Dictionary<Health, Action> deathCallbacks = new Dictionary<Health, Action>();

    private int playerSideAlive;
    private int enemySideAlive;

    private int playerSideKills;
    private int enemySideKills;

    private int playerSideDeaths;
    private int enemySideDeaths;

    private int performanceScore;
    private float matchStartTime;
    private bool matchEnded = false;
    private bool matchStarted = false;
    private bool lastPlayerWon = false;

    private float lastPlayerKillTime = -999f;
    private int currentKillStreak = 0;

    private bool playerWasHit = false;
    private float lastKnownPlayerHealth = -1f;

    void Start()
    {
        if (autoStartWithSceneCombatants)
        {
            BeginMatch(playerHealth);
            return;
        }

        UpdateTimerUIToFull();
        UpdateUI();
    }

    void Update()
    {
        if (!matchStarted)
        {
            return;
        }

        if (matchEnded)
        {
            return;
        }

        UpdateTimerUI();

        if (Time.time >= matchStartTime + timeLimit)
        {
            EndMatchByTimeout();
        }
    }

    public void BeginMatch(Health currentPlayerHealth)
    {
        lastPlayerWon = false;
        ClearCurrentSubscriptions();
        ResetRuntimeState();

        playerHealth = currentPlayerHealth;
        RegisterCombatants();

        matchStartTime = Time.time;
        matchStarted = true;

        if (playerHealth != null)
        {
            lastKnownPlayerHealth = playerHealth.GetCurrentHealth();
            playerHealth.OnHealthChanged += OnPlayerHealthChanged;
        }

        UpdateTimerUI();
        UpdateUI();

        if (resultText != null)
        {
            resultText.text = string.Empty;
        }
    }

    // To facilitate searching for Combatant in the scene
    public void SetCombatantSearchRoot(Transform root)
    {
        combatantSearchRoot = root;
    }

    private void ResetRuntimeState()
    {
        trackedHealths.Clear();
        memberLookup.Clear();
        deathCallbacks.Clear();

        playerSideAlive = 0;
        enemySideAlive = 0;

        playerSideKills = 0;
        enemySideKills = 0;

        playerSideDeaths = 0;
        enemySideDeaths = 0;

        performanceScore = 0;
        matchEnded = false;

        lastPlayerKillTime = -999f;
        currentKillStreak = 0;

        playerWasHit = false;
        lastKnownPlayerHealth = -1f;
    }

    private void ClearCurrentSubscriptions()
    {
        int i;
        List<Health> healthKeys;

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= OnPlayerHealthChanged;
        }

        healthKeys = new List<Health>(deathCallbacks.Keys);

        for (i = 0; i < healthKeys.Count; i++)
        {
            if (healthKeys[i] == null)
            {
                continue;
            }

            if (!deathCallbacks.ContainsKey(healthKeys[i]))
            {
                continue;
            }

            healthKeys[i].OnDied -= deathCallbacks[healthKeys[i]];
        }
    }

    // Initialize the team member information for both sides.
    private void RegisterCombatants()
    {
        TeamMember[] members;
        int i;

        if (combatantSearchRoot != null)
        {
            members = combatantSearchRoot.GetComponentsInChildren<TeamMember>();
        }
        else
        {
            members = GameObject.FindObjectsByType<TeamMember>(FindObjectsSortMode.None);
        }

        for (i = 0; i < members.Length; i++)
        {
            Health h;
            Action deathCallback;

            if (members[i] == null || !members[i].CountsAsCombatant())
            {
                continue;
            }

            h = members[i].GetComponent<Health>();

            if (h == null)
            {
                continue;
            }

            trackedHealths.Add(h);
            memberLookup[h] = members[i];

            // To make it easier to unsubscribe
            deathCallback = delegate
            {
                OnCombatantDied(h);
            };

            deathCallbacks[h] = deathCallback;
            h.OnDied += deathCallback;

            if (members[i].GetTeam() == ArenaTeam.PlayerSide)
            {
                playerSideAlive++;
            }
            else
            {
                enemySideAlive++;
            }
        }
    }

    // Used to determine if the player is unharmed.
    private void OnPlayerHealthChanged(float current, float max)
    {
        if (lastKnownPlayerHealth < 0f)
        {
            lastKnownPlayerHealth = current;
            return;
        }

        if (current < lastKnownPlayerHealth)
        {
            playerWasHit = true;
        }

        lastKnownPlayerHealth = current;
    }

    // After a fighter dies in the arena, process all relevant data.
    private void OnCombatantDied(Health deadHealth)
    {
        TeamMember member;
        float now;

        if (matchEnded)
        {
            return;
        }

        if (!memberLookup.ContainsKey(deadHealth))
        {
            return;
        }

        member = memberLookup[deadHealth];
        now = Time.time;

        if (member.GetTeam() == ArenaTeam.PlayerSide)
        {
            playerSideAlive--;
            playerSideDeaths++;
            enemySideKills++;
        }
        else
        {
            enemySideAlive--;
            enemySideDeaths++;
            playerSideKills++;
            performanceScore += killScoreBonus;

            // Determine if this kill is a kill streak.
            if (now <= lastPlayerKillTime + streakWindow)
            {
                currentKillStreak++;
            }
            else
            {
                currentKillStreak = 1;
            }

            lastPlayerKillTime = now;

            // Calculate killstreak bonus points
            if (currentKillStreak == 2)
            {
                performanceScore += doubleKillBonus;
            }
            else if (currentKillStreak >= 3)
            {
                performanceScore += tripleKillBonus;
            }
        }

        UpdateUI();

        if (endOnTeamWipe)
        {
            if (enemySideAlive <= 0)
            {
                EndMatch(true);
                return;
            }

            if (playerSideAlive <= 0)
            {
                EndMatch(false);
                return;
            }
        }
    }

    // Handle situations where no team has won by the time limit.
    private void EndMatchByTimeout()
    {
        bool playerWon;

        if (playerSideKills > enemySideKills)
        {
            playerWon = true;
        }
        else if (playerSideKills < enemySideKills)
        {
            playerWon = false;
        }
        else
        {
            // If both sides have the same number of kills, compare the total health of all remaining units.
            playerWon = GetRemainingTotalHealth(ArenaTeam.PlayerSide) >= GetRemainingTotalHealth(ArenaTeam.EnemySide);
        }

        EndMatch(playerWon);
    }

    // Calculate total health of all remaining units
    private float GetRemainingTotalHealth(ArenaTeam team)
    {
        float total;
        int i;

        total = 0f;

        for (i = 0; i < trackedHealths.Count; i++)
        {
            TeamMember member;

            if (trackedHealths[i] == null)
            {
                continue;
            }

            if (!memberLookup.ContainsKey(trackedHealths[i]))
            {
                continue;
            }

            member = memberLookup[trackedHealths[i]];

            // Determine whether the unit belongs to the team.
            if (member.GetTeam() != team)
            {
                continue;
            }

            if (trackedHealths[i].GetIsDead())
            {
                continue;
            }

            total += trackedHealths[i].GetCurrentHealth();
        }

        return total;
    }

    private void EndMatch(bool playerWon)
    {
        float elapsed; // Total match time
        int speedBonus;
        string grade;

        if (matchEnded)
        {
            return;
        }

        matchEnded = true;
        lastPlayerWon = playerWon;
        elapsed = Time.time - matchStartTime;

        if (playerWon)
        {
            performanceScore += winScoreBonus;

            // Calculate the speedrun bonus based on the remaining time.
            speedBonus = Mathf.RoundToInt(Mathf.Clamp01((timeLimit - elapsed) / timeLimit) * maxSpeedBonus);
            performanceScore += speedBonus;
        }

        if (playerSideDeaths == 0)
        {
            performanceScore += noDeathBonus;
        }

        if (!playerWasHit)
        {
            performanceScore += noHitBonus;
        }

        // Grades are awarded based on scores. From C to S
        grade = CalculateGrade(performanceScore);

        Debug.Log("Match Ended. PlayerWon = " + playerWon);
        Debug.Log("Performance Score = " + performanceScore + ", Grade = " + grade);

        if (resultText != null)
        {
            if (playerWon)
            {
                resultText.text = "Victory\nScore: " + performanceScore + "\nGrade: " + grade;
            }
            else
            {
                resultText.text = "Defeat\nScore: " + performanceScore + "\nGrade: " + grade;
            }
        }

        UpdateUI();
    }

    private string CalculateGrade(int score)
    {
        if (score >= 220)
        {
            return "S";
        }

        if (score >= 160)
        {
            return "A";
        }

        if (score >= 100)
        {
            return "B";
        }

        return "C";
    }

    private void UpdateTimerUI()
    {
        float remain;
        int seconds;

        if (timerText == null)
        {
            return;
        }

        remain = Mathf.Max(0f, (matchStartTime + timeLimit) - Time.time);
        seconds = Mathf.CeilToInt(remain);
        timerText.text = "Time: " + seconds;
    }

    private void UpdateTimerUIToFull()
    {
        if (timerText == null)
        {
            return;
        }

        timerText.text = "Time: " + Mathf.CeilToInt(timeLimit);
    }

    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text =
                "Kills " + playerSideKills +
                " : " + enemySideKills +
                "\nAlive " + playerSideAlive +
                " : " + enemySideAlive +
                "\nScore " + performanceScore;
        }
    }

    public bool GetMatchEnded()
    {
        return matchEnded;
    }

    public int GetPerformanceScore()
    {
        return performanceScore;
    }

    public int GetPlayerSideKills()
    {
        return playerSideKills;
    }

    public int GetEnemySideKills()
    {
        return enemySideKills;
    }
    public bool GetLastPlayerWon()
    {
        return lastPlayerWon;
    }
}
