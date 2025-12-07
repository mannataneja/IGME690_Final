using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BoatController : MonoBehaviour
{
    private Planet planet;
    private HashSet<Polygon> allowedOceanRegion;

    public float moveSpeed = 5f;
    public float turnSpeed = 15f;
    public float gravityStrength = 30f;
    public float surfaceOffset = 0.2f;

    private Rigidbody rb;
    private Transform planetTransform;

    public void Initialize(Planet planetInstance, HashSet<Polygon> oceanRegion)
    {
        planet = planetInstance;
        allowedOceanRegion = oceanRegion;
        planetTransform = planetInstance.transform;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ;
    }

    void FixedUpdate()
    {
        if (planet == null) return;

        Vector3 center = planetTransform.position;
        Vector3 toCenter = (center - transform.position).normalized;

        rb.AddForce(toCenter * gravityStrength, ForceMode.Acceleration);

        if (RaycastOceanSurface(out Vector3 surfPos, out Vector3 surfNormal))
        {
            rb.MovePosition(surfPos + surfNormal * surfaceOffset);


            Quaternion targetRot =
                Quaternion.FromToRotation(transform.up, surfNormal) * transform.rotation;

            rb.MoveRotation(Quaternion.Lerp(transform.rotation, targetRot, 0.15f));
        }


        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");


        if (Mathf.Abs(h) < 0.01f && Mathf.Abs(v) < 0.01f)
            return;

        // tangent basis
        Vector3 up = (transform.position - center).normalized;
        Vector3 right = Vector3.Cross(up, transform.forward).normalized;
        Vector3 forward = Vector3.Cross(right, up).normalized;

        Vector3 moveDir = (forward * v + right * h).normalized;


        if (moveDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetLook = Quaternion.LookRotation(moveDir, up);
            rb.MoveRotation(Quaternion.Lerp(transform.rotation, targetLook, turnSpeed * Time.deltaTime));
        }


        Vector3 desiredPos = rb.position + moveDir * moveSpeed * Time.deltaTime;

        // project onto correct radius
        float radius = (rb.position - center).magnitude;
        Vector3 newDirFromCenter = (desiredPos - center).normalized;
        desiredPos = center + newDirFromCenter * radius;


        if (IsOnAllowedOceanPolygon(desiredPos))
        {
            rb.MovePosition(desiredPos);
        }

    }
    bool RaycastOceanSurface(out Vector3 surfPos, out Vector3 surfNormal)
    {
        Vector3 center = planetTransform.position;
        Vector3 inward = (center - transform.position).normalized;

        Vector3 rayOrigin = transform.position - inward * 0.1f;

        if (Physics.Raycast(rayOrigin, inward, out RaycastHit hit, 3f))
        {
            PlanetSurface ps = hit.collider.GetComponent<PlanetSurface>();
            if (ps == null || ps.planet != planet)
            {
                surfPos = transform.position;
                surfNormal = inward;
                return false;
            }

            surfPos = hit.point;
            surfNormal = hit.normal;
            return true;
        }

        surfPos = transform.position;
        surfNormal = inward;
        return false;
    }


    bool IsOnAllowedOceanPolygon(Vector3 worldPos)
    {
        Vector3 center = planetTransform.position;
        Vector3 inward = (center - worldPos).normalized;
        Vector3 rayOrigin = worldPos - inward * 0.1f;

        if (Physics.Raycast(rayOrigin, inward, out RaycastHit hit, 3f))
        {
            PlanetSurface ps = hit.collider.GetComponent<PlanetSurface>();
            if (ps == null || ps.planet != planet)
                return false;

            Polygon poly = planet.GetPolygonFromTriangleIndex(hit.triangleIndex);
            return allowedOceanRegion.Contains(poly);
        }

        return false;
    }
}
