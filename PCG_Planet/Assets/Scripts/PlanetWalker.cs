using UnityEngine;

public class PlanetWalker : MonoBehaviour
{
    public Transform planet;
    public float moveSpeed = 5f;
    public float gravityStrength = 20f;
    public float rotationSpeed = 10f;

    Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.freezeRotation = true;  
    }

    void FixedUpdate()
    {
        Vector3 planetCenter = planet.position;

        Vector3 up = (transform.position - planetCenter).normalized;

        Vector3 gravityDir = -up; // inward
        rb.AddForce(gravityDir * gravityStrength, ForceMode.Acceleration);

        Quaternion targetRot = Quaternion.FromToRotation(transform.up, up) * transform.rotation;
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.deltaTime));

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 forward = Vector3.Cross(transform.right, up).normalized;
        Vector3 right = Vector3.Cross(up, forward).normalized;

        Vector3 moveDir = (forward * v + right * h).normalized;

        rb.MovePosition(rb.position + moveDir * moveSpeed * Time.deltaTime);
    }
}
