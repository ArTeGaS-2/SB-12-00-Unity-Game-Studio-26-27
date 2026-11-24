using UnityEngine;

[RequireComponent(typeof(Movement))]
public class Animation : MonoBehaviour
{
    private Movement movement;
    private Animator anime;

    private void Start()
    {
        movement = GetComponent<Movement>();
        anime = GetComponentInChildren<Animator>();
    }

    private void LateUpdate()
    {
        anime.SetBool("runKey", movement.IsRunning);
        anime.SetFloat("Walk", movement.MoveAmount);
    }
}
