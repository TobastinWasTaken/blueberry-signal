using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

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
    [SerializeField] private GameObject[] tirePivots = new GameObject[4];
    [SerializeField] private GameObject[] chargeMeters = new GameObject[2];
    [SerializeField] private GameObject[] tireLightnings = new GameObject[2];
    [SerializeField] private TrailRenderer[] rearSkidMarks = new TrailRenderer[2];
    [SerializeField] private ParticleSystem[] rearSkidSmokes = new ParticleSystem[2];
    [SerializeField] private CinemachineVirtualCamera carCam;
    [SerializeField] private TireEffectsController[] tireEffects = new TireEffectsController[2];

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
    private float hoverInput = 0;

    [Header("Car Settings")]
    [SerializeField] private float rbDrag = 0.2f;
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float maxSpeed = 100f;
    [SerializeField] private float steerStrength = 15f;
    [SerializeField] private AnimationCurve turningCurve;
    [SerializeField] private float dragCoefficient = 1f;
    [SerializeField] private float minSkidSideVelocity = 10f;

    private Vector3 currentCarLocalVelocity = Vector3.zero;
    private Vector3 currentCarLocalAcceleration = Vector3.zero;
    private float carVelocityRatio = 0;
    private float carSkidVelocityRatio = 0;
    private float currentSteerStrength;

    [Header("Power Slide")]
    [SerializeField] private float wheelStaticBuildupSpeed = 0.2f;
    [SerializeField] private float powerBoostChargeSpeed = 0.2f;
    [SerializeField] private float powerBoostDepleteSpeed = 0.5f;
    [SerializeField] private float powerBoostAcceleration = 10f;
    [SerializeField] private float boostRealignChargeThreshold = 0.15f;

    private bool carIsPowerSliding = false;
    private float wheelStaticBuildupLevel = 0f;
    private float powerBoostChargeLevel = 0f;

    [Header("Hovering")]
    [SerializeField] float hoverSteerStrength = 25f;

    private bool carIsHovering = false;

    [Header("Visuals")]
    [SerializeField] float tirePositionLerpAmt = 0.5f;
    [SerializeField] float tireRotSpeed = 3000f;
    [SerializeField] float maxSteeringAngle = 30f;
    [SerializeField] float cameraAccelFOVModifier = 0f;

    [Header("Camera")]
    [SerializeField] float baseCamFOV = 60f;
    [SerializeField] float FOVMultiplier = 1f;
    [SerializeField] float maxFOV = 90f;
    [SerializeField] float minFOV = 45f;
    [SerializeField] float FOVSetting = 1f;

    #region Unity Functions

    private void Awake()
    {
        carRB = GetComponent<Rigidbody>();
        carRB.drag = rbDrag;
        currentSteerStrength = steerStrength;
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
            Hover();
            SidewaysDrag();
        }
    }

    private void Acceleration()
    {
        if (carIsHovering)
            return;

        carRB.AddForceAtPosition(acceleration * accelInput * transform.forward, accelerationPoint.position, ForceMode.Acceleration);
    }

    private void Deceleration()
    {
        if (carIsHovering)
            return;

        carRB.AddForceAtPosition(deceleration * accelInput * -transform.forward, accelerationPoint.position, ForceMode.Acceleration);
    }

    private void Turn()
    {
        if (carIsHovering)
            carRB.AddRelativeTorque(currentSteerStrength * steerInput * carRB.transform.up, ForceMode.Acceleration);
        else
            carRB.AddRelativeTorque(currentSteerStrength * steerInput * turningCurve.Evaluate(Mathf.Abs(carVelocityRatio)) * Mathf.Sign(carVelocityRatio) * carRB.transform.up, ForceMode.Acceleration);
    }

    private void Hover()
    {
        HoverToggle(hoverInput == 1f);

        if (!carIsHovering && powerBoostChargeLevel > 0)
        {
            if (powerBoostChargeLevel > boostRealignChargeThreshold)
            {
                // Re-orient the car's velocity if the charge level is greater than the threshold
                Vector3 newVelocity = transform.forward;
                float magnitude = currentCarLocalVelocity.magnitude;

                carRB.velocity = newVelocity * magnitude;
            }
            else if (currentCarLocalVelocity.z < 0)
            {
                // Zero out the z velocity if the car is moving relatively backwards
                Vector3 newVelocity = currentCarLocalVelocity;
                newVelocity.z = 0f;

                carRB.velocity = transform.TransformDirection(newVelocity);
            }

            carRB.AddForceAtPosition(powerBoostAcceleration * transform.forward, accelerationPoint.position, ForceMode.Acceleration);
        }
    }

    private void SidewaysDrag()
    {
        if (carIsHovering)
            return;

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

    #region Hovering

    private void HoverToggle(bool hovering)
    {
        if (carIsHovering == hovering)
            return;

        carIsHovering = hovering;

        if (hovering)
        {
            HoverOn();
        }
        else
        {
            HoverOff();
        }
    }

    private void HoverOn()
    {
        carRB.drag = 0f;
        currentSteerStrength = hoverSteerStrength;
        carIsPowerSliding = false;

        // Visuals
        PivotTires(true);

        for (int i = 0; i < tireEffects.Length; i++)
        {
            tireEffects[i].EnableMiniLightning(false);
        }
    }

    private void HoverOff()
    {
        carRB.drag = rbDrag;
        currentSteerStrength = steerStrength;

        // Visuals
        PivotTires(false);
    }

    #endregion

    #region Power Sliding

    private void PowerSlideToggler(bool powerSliding)
    {
        if (carIsPowerSliding == powerSliding)
            return;

        carIsPowerSliding = powerSliding;

        for (int i = 0; i < tireEffects.Length; i++)
        {
            tireEffects[i].EnableMiniLightning(powerSliding);
        }
    }

    private void CalculatePowerSlide()
    {
        if (carIsPowerSliding)
        {
            wheelStaticBuildupLevel += Mathf.Abs(wheelStaticBuildupSpeed * carSkidVelocityRatio * Time.deltaTime);
            wheelStaticBuildupLevel = Mathf.Clamp01(wheelStaticBuildupLevel);
        }

        if (wheelStaticBuildupLevel == 1f)
        {
            for (int i = 0; i < tireEffects.Length; i++)
            {
                tireEffects[i].EnableClouds(true);
            }
        }
        else
        {
            for (int i = 0; i < tireEffects.Length; i++)
            {
                tireEffects[i].EnableClouds(false);
            }
        }

        if (carIsHovering)
        {
            float energyTransfer = Mathf.Min(powerBoostChargeSpeed * Time.deltaTime, wheelStaticBuildupLevel);
            energyTransfer = Mathf.Clamp01(energyTransfer);

            wheelStaticBuildupLevel -= energyTransfer;
            wheelStaticBuildupLevel = Mathf.Clamp01(wheelStaticBuildupLevel);

            powerBoostChargeLevel += energyTransfer;
        }

        if (!carIsHovering && powerBoostChargeLevel > 0)
        {
            powerBoostChargeLevel -= powerBoostDepleteSpeed * Time.deltaTime;
        }

        powerBoostChargeLevel = Mathf.Clamp01(powerBoostChargeLevel);
        //Debug.Log("Wheel Static Buildup: " + wheelStaticBuildupLevel + "; Power Boost Charge: " + powerBoostChargeLevel + "; Hovering: " + carIsHovering);
    }

    #endregion

    #region Visuals

    private void Visuals()
    {
        TireVisuals();
        SetChargeMeters();
        SetTireLightning();
        AccelerationFOVEffects();
    }

    private void TireVisuals()
    {
        if (carIsHovering)
            return;

        float steeringAngle = steerInput * maxSteeringAngle;

        for(int i = 0; i < tireModels.Length; i++)
        {
            // Front tires

            if (i < 2)
            {
                tireModels[i].transform.GetChild(0).Rotate(Vector3.up, tireRotSpeed * carVelocityRatio * Time.deltaTime, Space.Self);

                frontTireParents[i].transform.localEulerAngles = new Vector3(frontTireParents[i].transform.localEulerAngles.x, steeringAngle, frontTireParents[i].transform.localEulerAngles.x);
            }
            else
            {
                tireModels[i].transform.GetChild(0).Rotate(Vector3.up, tireRotSpeed * accelInput * Time.deltaTime, Space.Self);
            }
        }
    }

    private void SetTirePosition(GameObject tire, Vector3 targetPosition)
    {
        if (carIsHovering)
            return;

        tire.transform.position = Vector3.Lerp(tire.transform.position, targetPosition, tirePositionLerpAmt);
    }

    private void PivotTires(bool pivot)
    {
        float angle = 0f;

        if (pivot)
            angle = 90f;

        for(int i = 0; i < tirePivots.Length; i++)
        {
            if (i < 2)
            {
                // Left side wheels
                tirePivots[i].transform.localEulerAngles = new Vector3(0, 0, -angle);
            }
            else
            {
                // Right side wheels
                tirePivots[i].transform.localEulerAngles = new Vector3(0, 0, angle);
            }
        }
    }

    private void SetTireLightning()
    {
        for (int i = 0; i < tireLightnings.Length; i++)
        {
            if (wheelStaticBuildupLevel > 0)
            {
                tireLightnings[i].SetActive(true);
                tireLightnings[i].transform.localScale = new Vector3(1, 1, wheelStaticBuildupLevel);
            }
            else
            {
                tireLightnings[i].SetActive(false);
            }

        }
    }

    private void SetChargeMeters()
    {
        for(int i = 0; i < chargeMeters.Length; i++)
        {
            if (powerBoostChargeLevel > 0)
            {
                chargeMeters[i].SetActive(true);
                chargeMeters[i].transform.localScale = new Vector3(1, powerBoostChargeLevel, 1);
            }
            else
            {
                chargeMeters[i].SetActive(false);
            }
            
        }
    }

    private void AccelerationFOVEffects()
    {
        float newFOV = baseCamFOV + (currentCarLocalAcceleration.z * FOVMultiplier * FOVSetting);
        newFOV = Mathf.Clamp(newFOV, minFOV, maxFOV);

        carCam.m_Lens.FieldOfView = Mathf.Lerp(carCam.m_Lens.FieldOfView, newFOV, 0.1f);
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
        Vector3 previousCarLocalVelocity = currentCarLocalVelocity;
        currentCarLocalVelocity = transform.InverseTransformDirection(carRB.velocity);

        currentCarLocalAcceleration = currentCarLocalVelocity - previousCarLocalVelocity;

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

    public void HoverInput(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            hoverInput = 1f;
            return;
        }

        if (context.canceled)
        {
            hoverInput = 0f;
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
