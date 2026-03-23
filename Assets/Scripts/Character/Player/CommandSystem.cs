using UnityEngine;

public class CommandSystem : MonoBehaviour
{
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float selectRadius = 0.5f;
    [SerializeField] private Camera mainCamera;

    private KeyCode pendingFunctionKey = KeyCode.None;
    void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F4))
        {
            pendingFunctionKey = KeyCode.F4;
            return;
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            pendingFunctionKey = KeyCode.F3;
            return;
        }

        if (pendingFunctionKey == KeyCode.F4)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                FocusTargetCommand();
                pendingFunctionKey = KeyCode.None;
            }
        }

        if (pendingFunctionKey == KeyCode.F3)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                FollowPlayerCommand();
                pendingFunctionKey = KeyCode.None;
            }
        }
    }

    private void FocusTargetCommand()
    {
        Vector3 mouseWorld;
        Vector2 point;
        Collider2D hit;
        AllyController[] allies;
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

        allies = Object.FindObjectsByType<AllyController>(FindObjectsSortMode.None);

        for (i = 0; i < allies.Length; i++)
        {
            if (allies[i] != null)
            {
                allies[i].SetTarget(hit.transform);
            }
        }

        Debug.Log("Focus target command issued on " + hit.gameObject.name);
    }

    private void FollowPlayerCommand()
    {
        return;
    }

    void OnDrawGizmosSelected()
    {
        if (mainCamera == null)
        {
            return;
        }
    }
}
