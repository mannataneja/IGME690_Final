using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Transform planet;
    public float moveSpeed = 5f;
    public float gravityStrength = 20f;
    public float rotationSpeed = 10f;
    public float offsetAboveSurface = 2f;

    Rigidbody rb;
    float baseRadius;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.freezeRotation = true;

        Vector3 center = planet.position;
        baseRadius = (transform.position - center).magnitude;
    }

    void FixedUpdate()
    {
        Vector3 center = planet.position;

        Vector3 fromCenter = transform.position - center;
        Vector3 up = fromCenter.normalized;
        Vector3 inward = -up;

        rb.AddForce(inward * gravityStrength, ForceMode.Acceleration);

        float targetRadius = baseRadius + offsetAboveSurface;
        Vector3 desiredPos = center + up * targetRadius;

        rb.position = desiredPos;

        Quaternion targetRot = Quaternion.FromToRotation(transform.up, up) * transform.rotation;

        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.deltaTime));

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 forward = Vector3.Cross(transform.right, up).normalized;
        Vector3 right = Vector3.Cross(up, forward).normalized;

        Vector3 moveDir = (forward * v + right * h);

        if (moveDir.sqrMagnitude < 0.001f)
        {
            rb.linearVelocity = Vector3.zero;
        }
        else
        {
            rb.linearVelocity = moveDir.normalized * moveSpeed;
        }
    }
}
