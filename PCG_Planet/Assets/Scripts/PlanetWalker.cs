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
    }

    void FixedUpdate()
    {
        Vector3 planetCenter = planet.position;
        Vector3 toCenter = (transform.position - planetCenter).normalized;

        // Apply gravity toward the planet
        rb.AddForce(-toCenter * gravityStrength, ForceMode.Acceleration);

        // 2. Align the player so up is away from the planet
        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, toCenter * -1f) * transform.rotation;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );

        // Move along tangent 
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // Tangent plane basis
        Vector3 forward = Vector3.Cross(transform.right, toCenter).normalized;
        Vector3 right = Vector3.Cross(toCenter, forward).normalized;

        Vector3 moveDir = (forward * v + right * h).normalized;

        rb.MovePosition(transform.position + moveDir * moveSpeed * Time.deltaTime);
    }
}
