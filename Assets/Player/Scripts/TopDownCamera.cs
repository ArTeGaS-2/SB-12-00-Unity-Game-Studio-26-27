using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class TopDownCamera : MonoBehaviour
{
    private const float ClipMargin = 0.12f;
    private Transform player;
    private Camera view;
    private Vector3 offset;
    private Quaternion viewRotation;
    private float defaultNearPlane;
    private float playerClearance;
    private RaycastHit[] hits = new RaycastHit[8];
    private readonly List<Renderer> closeObstructions = new List<Renderer>();
    private readonly List<Renderer> hiddenByCamera = new List<Renderer>();

    private void Awake()
    {
        player = transform.parent;
        view = GetComponent<Camera>();
        // Локальний ракурс префаба задає постійний напрямок відносно мапи.
        offset = transform.localPosition;
        viewRotation = transform.localRotation;
        defaultNearPlane = view.nearClipPlane;
        var controller = player.GetComponent<CharacterController>();
        playerClearance = controller.height * 0.5f + controller.radius;
        FollowPlayer();
    }

    private void LateUpdate()
    {
        FollowPlayer();
    }

    private void FollowPlayer()
    {
        Vector3 focus = player.position;
        transform.SetPositionAndRotation(focus + offset, viewRotation);

        float distance = offset.magnitude;
        float nearPlane = defaultNearPlane;
        closeObstructions.Clear();
        int count;
        while ((count = Physics.RaycastNonAlloc(focus, offset, hits, distance,
                   Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) == hits.Length)
            Array.Resize(ref hits, hits.Length * 2);

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.transform.IsChildOf(player) || hit.collider.GetComponentInParent<WorldPickup>() != null)
                continue;

            nearPlane = Mathf.Max(nearPlane, distance - hit.distance + ClipMargin);
            // У будинку кілька секцій стіни можуть займати те саме місце.
            if (hit.distance < playerClearance + ClipMargin && hit.collider.TryGetComponent(out Renderer mesh))
                closeObstructions.Add(mesh);
        }

        UpdateHiddenRenderers();
        nearPlane = Mathf.Clamp(nearPlane, defaultNearPlane, Mathf.Max(defaultNearPlane, distance - playerClearance));
        if (!Mathf.Approximately(view.nearClipPlane, nearPlane))
            view.nearClipPlane = nearPlane;
    }

    private void UpdateHiddenRenderers()
    {
        for (int i = hiddenByCamera.Count - 1; i >= 0; i--)
        {
            Renderer mesh = hiddenByCamera[i];
            if (mesh != null && closeObstructions.Contains(mesh)) continue;
            if (mesh != null) mesh.forceRenderingOff = false;
            hiddenByCamera.RemoveAt(i);
        }
        foreach (Renderer mesh in closeObstructions)
        {
            if (mesh.forceRenderingOff) continue;
            mesh.forceRenderingOff = true;
            hiddenByCamera.Add(mesh);
        }
    }

    private void OnDisable()
    {
        closeObstructions.Clear();
        UpdateHiddenRenderers();
        if (view != null) view.nearClipPlane = defaultNearPlane;
    }
}
