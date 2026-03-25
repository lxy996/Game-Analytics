using UnityEngine;

public class WeaponDropPickup : ArenaPickup
{
    [SerializeField] private WeaponLoadoutData droppedLoadout;
    [SerializeField] private SpriteRenderer visualRenderer;

    void Awake()
    {
        RefreshVisual();
    }

    // Used to set the weapon loadout data and appearance of the pick-up item.
    public void SetDroppedLoadout(WeaponLoadoutData newLoadout)
    {
        droppedLoadout = newLoadout;
        RefreshVisual();
    }

    public WeaponLoadoutData GetDroppedLoadout()
    {
        return droppedLoadout;
    }

    protected override bool TryApplyPickup(Collider2D other)
    {
        WeaponLoadoutApplier applier;

        if (droppedLoadout == null)
        {
            return false;
        }

        applier = other.GetComponent<WeaponLoadoutApplier>();

        if (applier == null)
        {
            return false;
        }

        applier.SetLoadout(droppedLoadout);
        return true;
    }

    private void RefreshVisual()
    {
        if (visualRenderer == null)
        {
            return;
        }

        if (droppedLoadout == null)
        {
            return;
        }

        if (droppedLoadout.pickupIcon != null)
        {
            visualRenderer.sprite = droppedLoadout.pickupIcon;
        }
    }
}
