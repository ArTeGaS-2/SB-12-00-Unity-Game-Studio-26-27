using TMPro;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Movement : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI txtObj;
    [SerializeField] private InventoryUI inventory;

    [Header("Movement")]
    public float walkSpeed = 5f;
    public float runMultiplier = 2f;
    public float maxStamina = 100f;
    public float stamina;

    private CharacterController controller;
    private float verticalSpeed;

    public bool IsRunning { get; private set; }
    public float MoveAmount { get; private set; }

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        stamina = maxStamina;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        if (Time.timeScale == 0f || (inventory != null && inventory.GameplayInputBlocked))
        {
            MoveAmount = 0f;
            IsRunning = false;
            return;
        }

        Vector3 move = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        move = Vector3.ClampMagnitude(move, 1f);
        MoveAmount = move.magnitude;
        if (MoveAmount > 0.01f)
            transform.rotation = Quaternion.LookRotation(move);
        IsRunning = Input.GetKey(KeyCode.LeftShift) && MoveAmount > 0.1f && stamina > 0f;
        stamina = Mathf.Clamp(stamina + (IsRunning ? -20f : 15f) * Time.deltaTime, 0f, maxStamina);

        if (controller.isGrounded && verticalSpeed < 0f)
            verticalSpeed = -2f;
        verticalSpeed += Physics.gravity.y * Time.deltaTime;
        float speed = IsRunning ? walkSpeed * runMultiplier : walkSpeed;
        controller.Move((move * speed + Vector3.up * verticalSpeed) * Time.deltaTime);

        if (txtObj != null)
            txtObj.text = $"is Running? = {IsRunning}, Stamina = {stamina}";
    }
}
