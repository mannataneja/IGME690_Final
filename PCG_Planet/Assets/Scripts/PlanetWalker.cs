using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlanetWalker : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float gravityStrength = 20f;
    public float rotationSpeed = 10f;
    public float groundOffset = 0.1f;   // how far above surface to hover

    [Header("Planet")]
    public Transform planetTransform;     // assigned at runtime
    public Planet planet;               // reference to Planet script

    HashSet<Polygon> allowedPolygons;     // this continent

    Rigidbody rigidbodyComponent;

    public void Initialize(Planet planetInstance, HashSet<Polygon> allowedContinentPolygons)
    {
        planet = planetInstance;
        planetTransform = planetInstance.transform;
        allowedPolygons = allowedContinentPolygons;
    }

    void Start()
    {
        rigidbodyComponent = GetComponent<Rigidbody>();
        rigidbodyComponent.useGravity = false;
        rigidbodyComponent.constraints |= RigidbodyConstraints.FreezeRotationX |
                                          RigidbodyConstraints.FreezeRotationZ;
    }

    void FixedUpdate()
    {
        if (planetTransform == null || planet == null)
            return;

        Vector3 planetCenter = planetTransform.position;
        Vector3 fromCenter = (transform.position - planetCenter).normalized;

        // 1) Custom gravity toward planet
        rigidbodyComponent.AddForce(-fromCenter * gravityStrength, ForceMode.Acceleration);

        // 2) Align up with planet normal
        Quaternion targetRotation =
            Quaternion.FromToRotation(transform.up, fromCenter * -1f) * transform.rotation;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );

        // 3) Input-based movement, tangent to planet surface
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        if (Mathf.Approximately(horizontalInput, 0f) && Mathf.Approximately(verticalInput, 0f))
        {
            // Still maintain grounding even if not moving
            SnapToGround(fromCenter);
            return;
        }

        Vector3 forward = Vector3.Cross(transform.right, fromCenter).normalized;
        Vector3 right = Vector3.Cross(fromCenter, forward).normalized;

        Vector3 moveDirection = (forward * verticalInput + right * horizontalInput).normalized;

        float deltaMove = moveSpeed * Time.deltaTime;
        Vector3 desiredPosition = transform.position + moveDirection * deltaMove;

        // Keep approximately same radius
        float currentRadius = (transform.position - planetCenter).magnitude;
        Vector3 desiredDirectionFromCenter = (desiredPosition - planetCenter).normalized;
        desiredPosition = planetCenter + desiredDirectionFromCenter * currentRadius;

        // 4) Check if the new position stays on the same continent
        if (IsPositionOnAllowedContinent(desiredPosition, out Vector3 correctedPosition, out Vector3 surfaceNormal))
        {
            // Allowed: move and snap to surface
            Vector3 finalPosition = correctedPosition + surfaceNormal * groundOffset;
            rigidbodyComponent.MovePosition(finalPosition);

            // Re-align rotation to the new surface normal
            Quaternion newRotation =
                Quaternion.FromToRotation(transform.up, surfaceNormal) * transform.rotation;

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                newRotation,
                rotationSpeed * Time.deltaTime
            );
        }
        else
        {
            // Blocked: invisible wall – do not move
            SnapToGround(fromCenter);
        }
    }

    void SnapToGround(Vector3 fromCenter)
    {
        if (planetTransform == null || planet == null)
            return;

        Vector3 rayOrigin = transform.position + fromCenter * 0.5f;
        Vector3 rayDirection = -fromCenter;

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hitInfo, 5f))
        {
            PlanetSurface surface = hitInfo.collider.GetComponent<PlanetSurface>();
            if (surface != null && surface.planet == planet)
            {
                Vector3 finalPosition = hitInfo.point + hitInfo.normal * groundOffset;
                rigidbodyComponent.MovePosition(finalPosition);

                Quaternion newRotation =
                    Quaternion.FromToRotation(transform.up, hitInfo.normal) * transform.rotation;

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    newRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }
    }

    bool IsPositionOnAllowedContinent(Vector3 worldPosition, out Vector3 surfacePosition, out Vector3 surfaceNormal)
    {
        surfacePosition = worldPosition;
        surfaceNormal = (worldPosition - planetTransform.position).normalized;

        Vector3 fromCenter = (worldPosition - planetTransform.position).normalized;
        Vector3 rayOrigin = worldPosition + fromCenter * 0.5f;
        Vector3 rayDirection = -fromCenter;

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hitInfo, 5f))
        {
            PlanetSurface surface = hitInfo.collider.GetComponent<PlanetSurface>();
            if (surface == null || surface.planet != planet)
                return false;

            Polygon polygon = planet.GetPolygonFromTriangleIndex(hitInfo.triangleIndex);
            if (polygon == null)
                return false;

            // If no continent data, treat as allowed
            if (allowedPolygons == null || allowedPolygons.Count == 0)
                return true;

            if (!allowedPolygons.Contains(polygon))
                return false;

            surfacePosition = hitInfo.point;
            surfaceNormal = hitInfo.normal.normalized;
            return true;
        }

        return false;
    }
}
