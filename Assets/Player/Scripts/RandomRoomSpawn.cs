using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class RandomRoomSpawn : MonoBehaviour
{
    [SerializeField] private Transform[] roomStarts;

    private void Awake()
    {
        if (roomStarts == null || roomStarts.Length == 0)
            return;

        Transform start = roomStarts[Random.Range(0, roomStarts.Length)];
        var controller = GetComponent<CharacterController>();
        controller.enabled = false;
        transform.SetPositionAndRotation(start.position, start.rotation);
        controller.enabled = true;
    }
}
