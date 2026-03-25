using UnityEngine;

public class WeaponDropOnDeath : MonoBehaviour
{
    [SerializeField] private WeaponDropPickup weaponDropPrefab;
    [SerializeField] private Vector3 dropOffset = Vector3.zero;
    [SerializeField] private bool dropOnDeath = true;

    private Health health;
    private WeaponLoadoutApplier loadoutApplier;
    private bool hasDropped = false;

    void Awake()
    {
        health = GetComponent<Health>();
        loadoutApplier = GetComponent<WeaponLoadoutApplier>();
    }

    void OnEnable()
    {
        if (health != null)
        {
            health.OnDied += HandleDied;
        }
    }

    void OnDisable()
    {
        if (health != null)
        {
            health.OnDied -= HandleDied;
        }
    }

    private void HandleDied()
    {
        WeaponLoadoutData currentLoadout;
        WeaponDropPickup spawnedPickup;

        if (!dropOnDeath)
        {
            return;
        }

        if (hasDropped)
        {
            return;
        }

        if (weaponDropPrefab == null)
        {
            return;
        }

        if (loadoutApplier == null)
        {
            return;
        }

        currentLoadout = loadoutApplier.GetCurrentLoadout();

        if (currentLoadout == null)
        {
            return;
        }

        hasDropped = true;

        spawnedPickup = Instantiate(
            weaponDropPrefab,
            transform.position + dropOffset,
            Quaternion.identity
        );

        spawnedPickup.SetDroppedLoadout(currentLoadout);
    }
}
