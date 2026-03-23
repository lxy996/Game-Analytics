using UnityEngine;

public class Projectile : MonoBehaviour
{
    private float speed;
    private float damage;
    private float lifeTime;
    private Vector2 direction;
    private LayerMask targetLayer;
    private bool initialized = false;

    public void Initialize(Vector2 newDirection, float newSpeed, float newDamage, float newLifeTime, LayerMask newTargetLayer)
    {
        direction = newDirection.normalized;
        speed = newSpeed;
        damage = newDamage;
        lifeTime = newLifeTime;
        targetLayer = newTargetLayer;
        initialized = true;

        RotateToDirection(direction);

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        Vector3 moveAmount;

        if (!initialized)
        {
            return;
        }

        moveAmount = new Vector3(direction.x, direction.y, 0f) * speed * Time.deltaTime;
        transform.position = transform.position + moveAmount;
    }

    private void RotateToDirection(Vector2 dir)
    {
        float angle;

        angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        int otherLayerBit;
        Health targetHealth;

        // Used to detect whether the colliding layer is the target layer.
        otherLayerBit = 1 << other.gameObject.layer; // Move 1 to the left, the number of bits depends on the layer index.

        if ((targetLayer.value & otherLayerBit) == 0)
        {
            return;
        }

        targetHealth = other.GetComponent<Health>();

        if (targetHealth != null)
        {
            if (!targetHealth.GetIsDead())
            {
                targetHealth.TakeDamage(damage);
            }
        }

        Destroy(gameObject);
    }
}
