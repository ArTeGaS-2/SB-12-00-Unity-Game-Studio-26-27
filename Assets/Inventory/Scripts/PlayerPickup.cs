using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerPickup : MonoBehaviour
{
    [SerializeField] private InventoryUI inventory;
    [SerializeField, Min(0f)] private float pickupDistance = 4.5f;
    [SerializeField, Range(1f, 180f)] private float pickupAngle = 70f;

    private Collider[] nearby = new Collider[16];

    private void LateUpdate()
    {
        WorldPickup target = inventory.GameplayInputBlocked ? null : FindPickup();
        if (target != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame &&
            target.TryCollect(inventory))
            target = null;

        inventory.ShowAimedPickup(target);
    }

    private WorldPickup FindPickup()
    {
        Vector3 origin = transform.position;
        int count;
        while ((count = Physics.OverlapSphereNonAlloc(origin, pickupDistance, nearby,
                   Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide)) == nearby.Length)
            Array.Resize(ref nearby, nearby.Length * 2);

        WorldPickup nearest = null;
        float nearestDistance = float.PositiveInfinity;
        float reachSquared = pickupDistance * pickupDistance;
        float minCosine = Mathf.Cos(pickupAngle * 0.5f * Mathf.Deg2Rad);
        Vector3 eye = origin + Vector3.up * 0.75f;
        for (int i = 0; i < count; i++)
        {
            WorldPickup item = nearby[i].GetComponentInParent<WorldPickup>();
            if (item == null || !item.isActiveAndEnabled || item.Icon == null)
                continue;

            Vector3 center = nearby[i].bounds.center;
            Vector3 direction = center - origin;
            float squaredDistance = direction.sqrMagnitude;
            if (squaredDistance > reachSquared || squaredDistance >= nearestDistance)
                continue;

            direction.y = 0f;
            if (Vector3.Dot(transform.forward, direction) + 0.00001f < minCosine * direction.magnitude)
                continue;
            if (Physics.Linecast(eye, center, out RaycastHit obstruction,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                obstruction.collider.GetComponentInParent<WorldPickup>() != item)
                continue;

            nearest = item;
            nearestDistance = squaredDistance;
        }
        return nearest;
    }
}
