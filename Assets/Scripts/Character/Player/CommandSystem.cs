using System.Collections.Generic;
using UnityEngine;

public class CommandSystem : MonoBehaviour
{
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float selectRadius = 0.5f;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform playerTransform;

    private AllyController[] allies;
    private List<AllyController> selectedAllies = new List<AllyController>();
    private AllySelectionGroup currentGroup = AllySelectionGroup.All;
    //private KeyCode pendingFunctionKey = KeyCode.None;
    void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (playerTransform == null)
        {
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();

            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
    }
    void Start()
    {
        RefreshAllies();
        SelectGroup(AllySelectionGroup.All);
    }

    void Update()
    {
        HandleSelectionInput();
        HandleCommandInput();
    }

    private void RefreshAllies()
    {
        allies = Object.FindObjectsByType<AllyController>(FindObjectsSortMode.None);
    }

    // Use number keys to select ally groups.
    private void HandleSelectionInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SelectGroup(AllySelectionGroup.All);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SelectGroup(AllySelectionGroup.SwordShield);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SelectGroup(AllySelectionGroup.Polearm);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SelectGroup(AllySelectionGroup.Ranged);
        }
    }

    private void HandleCommandInput()
    {
        if (selectedAllies == null || selectedAllies.Count == 0)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.F1))
        {
            FocusTargetCommand();
        }
        else if (Input.GetKeyDown(KeyCode.F2))
        {
            FollowPlayerCommand();
        }
        else if (Input.GetKeyDown(KeyCode.F3))
        {
            AutoCombatCommand();
        }
    }

    private void SelectGroup(AllySelectionGroup group)
    {
        int i;

        RefreshAllies();

        selectedAllies.Clear();
        currentGroup = group;

        if (allies == null)
        {
            return;
        }

        for (i = 0; i < allies.Length; i++)
        {
            CharacterStats stats;

            if (allies[i] == null)
            {
                continue;
            }

            stats = allies[i].GetComponent<CharacterStats>();

            if (stats == null)
            {
                continue;
            }

            if (group == AllySelectionGroup.All)
            {
                selectedAllies.Add(allies[i]);
            }
            else if (group == AllySelectionGroup.SwordShield)
            {
                if (stats.weaponType == WeaponType.Melee && stats.hasShield)
                {
                    selectedAllies.Add(allies[i]);
                }
            }
            else if (group == AllySelectionGroup.Polearm)
            {
                if (stats.weaponType == WeaponType.Polearm)
                {
                    selectedAllies.Add(allies[i]);
                }
            }
            else if (group == AllySelectionGroup.Ranged)
            {
                if (stats.weaponType == WeaponType.Ranged)
                {
                    selectedAllies.Add(allies[i]);
                }
            }
        }

        Debug.Log("Selected group: " + group + ", count = " + selectedAllies.Count);
    }

    private void FocusTargetCommand()
    {
        Vector3 mouseWorld;
        Vector2 point;
        Collider2D hit;
        int i;

        if (mainCamera == null)
        {
            return;
        }

        mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition); // Calculate the mouse's world position depend on screen world
        point = new Vector2(mouseWorld.x, mouseWorld.y);

        hit = Physics2D.OverlapCircle(point, selectRadius, enemyLayer);

        if (hit == null)
        {
            return;
        }

        for (i = 0; i < selectedAllies.Count; i++)
        {
            if (selectedAllies[i] == null)
            {
                continue;
            }

            selectedAllies[i].SetTarget(hit.transform);
            selectedAllies[i].SetCommandMode(AllyCommandMode.FocusTarget);
        }

        Debug.Log("Focus target command issued on " + hit.gameObject.name + " to group " + currentGroup);
    }

    private void FollowPlayerCommand()
    {
        int i;

        for (i = 0; i < selectedAllies.Count; i++)
        {
            if (selectedAllies[i] == null)
            {
                continue;
            }

            selectedAllies[i].SetFollowTarget(playerTransform);
            selectedAllies[i].SetCommandMode(AllyCommandMode.FollowPlayer);
        }

        Debug.Log("Follow player command issued to group " + currentGroup);
    }

    private void AutoCombatCommand()
    {
        int i;

        for (i = 0; i < selectedAllies.Count; i++)
        {
            if (selectedAllies[i] == null)
            {
                continue;
            }

            selectedAllies[i].SetCommandMode(AllyCommandMode.AutoCombat);
        }

        Debug.Log("Auto combat command issued to group " + currentGroup);
    }

    void OnDrawGizmosSelected()
    {
        if (mainCamera == null)
        {
            return;
        }
    }
}
