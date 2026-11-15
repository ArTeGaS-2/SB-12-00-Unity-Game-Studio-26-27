using UnityEngine;

public class BunkerAnimatedProp : MonoBehaviour
{
    public Vector3 rotationAxis = Vector3.forward;
    public float rotationSpeed = 18f;
    public bool swing;
    public float swingAngle = 4f;
    public float swingSpeed = 1.4f;
    Quaternion startRotation;

    void Awake() { startRotation = transform.localRotation; }

    void Update()
    {
        if (swing)
        {
            float angle = Mathf.Sin(Time.time * swingSpeed) * swingAngle;
            transform.localRotation = startRotation * Quaternion.AngleAxis(angle, rotationAxis);
        }
        else transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);
    }
}
