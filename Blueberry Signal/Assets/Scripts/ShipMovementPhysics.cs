using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShipMovementPhysics : MonoBehaviour
{
    CharacterController moveComponent;
    PlayerInput input;

    [SerializeField]
    Vector3 shipVelocity;
    float shipAngularMomentum;

    [SerializeField]
    float
        moveSpeedMultiplier = 100f,
        gravityMultiplier = 9.81f,
        groundFrictionMultiplier = 0.1f,
        slideFrictionMultiplier = 0.8f,
        turnSpeedMultiplier = 10f,
        turnFrictionMultiplier = 0.7f;

    [SerializeField]
    float
        driftAngle = 5f;

    float acceleratorInput = 0f;
    Vector2 steerInput;

    enum DriveMode
    {
        Ground,
        Hover
    }

    DriveMode shipDriveMode = DriveMode.Ground;

    private void Awake()
    {
        moveComponent = GetComponent<CharacterController>();
        input = GetComponent<PlayerInput>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        switch (shipDriveMode)
        {
            case DriveMode.Ground:
                GroundDrive();
                break;
            case DriveMode.Hover:
                HoverDrive();
                break;
        }

        MoveShip(shipVelocity, shipAngularMomentum);
    }

    void GroundDrive()
    {
        Vector3 accelVec = CalculateAcceleration();
        float rotVal = CalculateTurn();

        // Rotate the vehicle
        shipAngularMomentum += rotVal;
        // Dampen the rotation
        shipAngularMomentum *= (1 - (turnFrictionMultiplier * Time.deltaTime));

        // Accelerate the vehicle
        shipVelocity += accelVec;
        // Decelerate the vehicle
        shipVelocity *= ApplyFriction();
        // Apply gravity
        //shipVelocity += Vector3.down * ApplyGravity();
    }

    void HoverDrive()
    {

    }

    public void AccelerateInput(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            acceleratorInput = 1f;
            return;
        }

        if (context.canceled)
        {
            acceleratorInput = 0f;
            return;
        }
    }

    public void SteerInput(InputAction.CallbackContext context)
    {
        Vector2 v = context.ReadValue<Vector2>();
        steerInput = v;
    }

    float ApplyGravity()
    {
        return gravityMultiplier * Time.deltaTime;
    }

    Vector3 CalculateAcceleration()
    {
        return transform.forward * moveSpeedMultiplier * acceleratorInput * Time.deltaTime;
    }

    float CalculateTurn()
    {
        return steerInput.x * turnSpeedMultiplier * Time.deltaTime;
    }

    float ApplyFriction()
    {
        Debug.Log(Mathf.Abs(Vector3.Angle(transform.forward, shipVelocity)));
        Debug.DrawRay(transform.position, transform.forward, Color.red);
        Debug.DrawRay(transform.position, shipVelocity, Color.blue);

        // TODO: Look at angle between move speed and facing speed, and change the friction based on that
        if (Mathf.Abs(Vector3.Angle(transform.forward, shipVelocity)) > driftAngle)
        {
            return 1f - slideFrictionMultiplier * Time.deltaTime;
        }

        return 1f - groundFrictionMultiplier * Time.deltaTime;
    }

    void MoveShip(Vector3 vec, float turn)
    {
        moveComponent.Move(vec * moveSpeedMultiplier * Time.deltaTime);
        transform.Rotate(transform.up * turn * Time.deltaTime);
    }
}
