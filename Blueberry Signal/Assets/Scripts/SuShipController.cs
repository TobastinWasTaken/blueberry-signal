using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SuShipController : MonoBehaviour
{
    // Suspension tutorial: https://www.youtube.com/watch?v=sWshRRDxdSU by Ash Dev

    [Header("References")]
    [SerializeField] private Rigidbody carRB;
    [SerializeField] private Transform[] rayPoints;
    [SerializeField] private LayerMask drivableMask;
    [SerializeField] private PlayerInput input;
    [SerializeField] private Transform accelerationPoint;
    [SerializeField] private GameObject[] tireModels = new GameObject[4];
    [SerializeField] private GameObject[] frontTireParents = new GameObject[2];
    [SerializeField] private TrailRenderer[] rearSkidMarks = new TrailRenderer[2];
    [SerializeField] private ParticleSystem[] rearSkidSmokes = new ParticleSystem[2];

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

    private int[] wheelsIsGrounded = new int[4];
    private bool isGrounded = false;

    [Header("Input")]
    private float accelInput = 0;
    private float steerInput = 0;
    private float boostInput = 0;

    [Header("Car Settings")]
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float maxSpeed = 100f;
    [SerializeField] private float steerStrength = 15f;
    [SerializeField] private AnimationCurve turningCurve;
    [SerializeField] private float dragCoefficient = 1f;
    [SerializeField] private float minSkidSideVelocity = 10f;

    private Vector3 currentCarLocalVelocity = Vector3.zero;
    private float carVelocityRatio = 0;
    private float carSkidVelocityRatio = 0;

    [Header("Power Slide")]
    [SerializeField] private float powerBoostChargeSpeed = 0.2f;
    [SerializeField] private float powerBoostDepleteSpeed = 0.5f;
    [SerializeField] private float powerBoostAcceleration = 10f;

    private bool carIsPowerSliding = false;
    private float carBoostTimer = 0f;
    private float powerBoostChargeLevel = 0f;

    [Header("Visuals")]
    [SerializeField] float tirePositionLerpAmt = 0.5f;
    [SerializeField] float tireRotSpeed = 3000f;
    [SerializeField] float maxSteeringAngle = 30f;

    #region Unity Functions

    private void Start()
    {
        carRB = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        Suspension();
        GroundCheck();
        CalculateCarVelocity();
        Movement();
        CalculatePowerSlide();
        Visuals();
    }

    #endregion

    #region Movement

    private void Movement()
    {
        if (isGrounded)
        {
            Acceleration();
            Deceleration();
            Turn();
            Boost();
            SidewaysDrag();
        }
    }

    private void Acceleration()
    {
        carRB.AddForceAtPosition(acceleration * accelInput * transform.forward, accelerationPoint.position, ForceMode.Acceleration);
    }

    private void Deceleration()
    {
        carRB.AddForceAtPosition(deceleration * accelInput * -transform.forward, accelerationPoint.position, ForceMode.Acceleration);
    }

    private void Turn()
    {
        carRB.AddRelativeTorque(steerStrength * steerInput * turningCurve.Evaluate(Mathf.Abs(carVelocityRatio)) * Mathf.Sign(carVelocityRatio) * carRB.transform.up, ForceMode.Acceleration);
    }

    private void Boost()
    {
        carBoostTimer = 1 * boostInput;

        if (carBoostTimer > 0 && powerBoostChargeLevel > 0)
        {
            carRB.AddForceAtPosition(powerBoostAcceleration * transform.forward, accelerationPoint.position, ForceMode.Acceleration);
        }
    }

    private void SidewaysDrag()
    {
        float currentSidewaysSpeed = currentCarLocalVelocity.x;

        if (isGrounded && Mathf.Abs(currentSidewaysSpeed) > minSkidSideVelocity)
        {
            // Begin power slide
            PowerSlideToggler(true);
        }
        else
        {
            // End power slide
            PowerSlideToggler(false);
        }

        float dragMagnitude = -currentSidewaysSpeed * dragCoefficient;

        Vector3 dragForce = dragMagnitude * transform.right;

        carRB.AddForceAtPosition(dragForce, accelerationPoint.position, ForceMode.Acceleration);
    }

    #endregion

    #region Power Sliding

    private void PowerSlideToggler(bool powerSliding)
    {
        if (carIsPowerSliding == powerSliding)
            return;

        carIsPowerSliding = powerSliding;
    }

    private void CalculatePowerSlide()
    {
        if (carIsPowerSliding)
        {
            powerBoostChargeLevel += Mathf.Abs(powerBoostChargeSpeed * carSkidVelocityRatio * Time.deltaTime);
        }

        if (carBoostTimer > 0)
        {
            carBoostTimer -= Time.deltaTime;
            powerBoostChargeLevel -= powerBoostDepleteSpeed * Time.deltaTime;
        }

        powerBoostChargeLevel = Mathf.Clamp01(powerBoostChargeLevel);
        Debug.Log("Power Boost Charge: " + powerBoostChargeLevel);
    }

    #endregion

    #region Visuals

    private void Visuals()
    {
        TireVisuals();
    }

    private void TireVisuals()
    {
        float steeringAngle = steerInput * maxSteeringAngle;

        for(int i = 0; i < tireModels.Length; i++)
        {
            // Front tires

            if (i < 2)
            {
                tireModels[i].transform.Rotate(Vector3.right, tireRotSpeed * carVelocityRatio * Time.deltaTime, Space.Self);

                frontTireParents[i].transform.localEulerAngles = new Vector3(frontTireParents[i].transform.localEulerAngles.x, steeringAngle, frontTireParents[i].transform.localEulerAngles.x);
            }
            else
            {
                tireModels[i].transform.Rotate(Vector3.right, tireRotSpeed * accelInput * Time.deltaTime, Space.Self);
            }
        }
    }

    private void SetTirePosition(GameObject tire, Vector3 targetPosition)
    {
        tire.transform.position = Vector3.Lerp(tire.transform.position, targetPosition, tirePositionLerpAmt);
    }

    #endregion

    #region Car Status Check

    private void GroundCheck()
    {
        int tempGroundedWheels = 0;

        for (int i = 0; i < wheelsIsGrounded.Length; i++)
        {
            tempGroundedWheels += wheelsIsGrounded[i];
        }

        if (tempGroundedWheels > 1)
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    private void CalculateCarVelocity()
    {
        currentCarLocalVelocity = transform.InverseTransformDirection(carRB.velocity);
        carVelocityRatio = currentCarLocalVelocity.z / maxSpeed;
        carSkidVelocityRatio = currentCarLocalVelocity.x / maxSpeed;
    }

    #endregion

    #region Input Handling

    public void AccelerateInput(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            accelInput = 1f;
            return;
        }

        if (context.canceled)
        {
            accelInput = 0f;
            return;
        }
    }

    public void SteerInput(InputAction.CallbackContext context)
    {
        float f = context.ReadValue<Vector2>().x;
        steerInput = f;
    }

    public void BoostInput(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            boostInput = 1f;
            return;
        }

        if (context.canceled)
        {
            boostInput = 0f;
            return;
        }
    }

    #endregion

    #region Suspension Functions

    private void Suspension()
    {

        for (int i = 0; i < rayPoints.Length; i++)
        {
            RaycastHit hit;
            float maxLength = restLength + springTravel;

            if (Physics.Raycast(rayPoints[i].position, -rayPoints[i].up, out hit, maxLength + wheelRadius, drivableMask))
            {
                wheelsIsGrounded[i] = 1;

                float currentSpringLength = hit.distance - wheelRadius;
                float springCompression = (restLength - currentSpringLength) / springTravel; // Normalized from 0 to 1

                float springVelocity = Vector3.Dot(carRB.GetPointVelocity(rayPoints[i].position), rayPoints[i].up);
                float dampForce = damperStiffness * springVelocity;

                float springForce = springCompression * springStiffness;

                float netForce = springForce - dampForce;

                carRB.AddForceAtPosition(netForce * rayPoints[i].up, rayPoints[i].position);

                // Visuals

                SetTirePosition(tireModels[i], hit.point + rayPoints[i].up * wheelRadius);

                // Debug

                Debug.DrawLine(rayPoints[i].position, hit.point, Color.red);
            }
            else
            {
                wheelsIsGrounded[i] = 0;

                // Visuals

                SetTirePosition(tireModels[i], rayPoints[i].position - rayPoints[i].up * maxLength);

                // Debug

                Debug.DrawLine(rayPoints[i].position, rayPoints[i].position + (wheelRadius * maxLength) * -rayPoints[i].up, Color.green);
            }
        }

    }

    #endregion
}
