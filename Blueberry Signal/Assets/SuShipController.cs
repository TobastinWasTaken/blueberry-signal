using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuShipController : MonoBehaviour
{
    // Suspension tutorial: https://www.youtube.com/watch?v=sWshRRDxdSU by Ash Dev

    [Header("References")]
    [SerializeField] private Rigidbody carRB;
    [SerializeField] private Transform[] rayPoints;
    [SerializeField] private LayerMask drivableMask;

    [Header("Suspension Settings")]
    [SerializeField] private float springStiffness;
    [SerializeField] private float damperStiffness;
    // Optimal damper stiffness formula:
    // D = (2*sqrt(K*m))*Z
    // ---
    // D = Damper Stiffness
    // K = Spring Stiffness
    // m = car's mass
    // Z optimal range = 0.2-1 (adjust to taste)
    // ---
    [SerializeField] private float restLength;
    [SerializeField] private float springTravel;
    [SerializeField] private float wheelRadius;

    private void Start()
    {
        carRB = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        Suspension();
    }

    private void Suspension()
    {

        foreach (Transform rayPoint in rayPoints)
        {
            RaycastHit hit;
            float maxLength = restLength + springTravel;

            if (Physics.Raycast(rayPoint.position, -rayPoint.up, out hit, maxLength + wheelRadius, drivableMask))
            {
                float currentSpringLength = hit.distance - wheelRadius;
                float springCompression = (restLength - currentSpringLength) / springTravel; // Normalized from 0 to 1

                float springVelocity = Vector3.Dot(carRB.GetPointVelocity(rayPoint.position), rayPoint.up);
                float dampForce = damperStiffness * springVelocity;

                float springForce = springCompression * springStiffness;

                float netForce = springForce - dampForce;

                carRB.AddForceAtPosition(netForce * rayPoint.up, rayPoint.position);

                Debug.DrawLine(rayPoint.position, hit.point, Color.red);
            }
            else
            {
                Debug.DrawLine(rayPoint.position, rayPoint.position + (wheelRadius * maxLength) * -rayPoint.up, Color.green);
            }
        }

    }
}
