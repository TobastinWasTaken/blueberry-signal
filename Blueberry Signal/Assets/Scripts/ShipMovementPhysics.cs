using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShipMovementPhysics : MonoBehaviour
{
    CharacterController moveComponent;
    PlayerInput input;
    Rigidbody body;

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
        turnFrictionMultiplier = 0.7f,
        moveSpeedMaximum = 10f;

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
        //moveComponent = GetComponent<CharacterController>();
        body = GetComponent<Rigidbody>();
        input = GetComponent<PlayerInput>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //shipVelocity = Vector3.zero;

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
        float rotVal = CalculateTurn();

        // Rotate the vehicle
        shipAngularMomentum += rotVal;
        // Dampen the rotation
        shipAngularMomentum *= turnFrictionMultiplier;

        // Accelerate the vehicle
        if (shipVelocity.magnitude < moveSpeedMaximum)
            shipVelocity += AddAcceleration();
        // Decelerate the vehicle via friction
        shipVelocity += AddWheelFriction();
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

    Vector3 AddAcceleration()
    {
        return transform.forward * moveSpeedMultiplier * acceleratorInput * Time.deltaTime;
    }

    float CalculateTurn()
    {
        return steerInput.x * turnSpeedMultiplier * Time.deltaTime;
    }

    Vector3 ApplyFriction()
    {
        Debug.Log(Mathf.Abs(Vector3.Angle(transform.forward, shipVelocity)));
        Debug.DrawRay(transform.position, transform.forward, Color.red);
        Debug.DrawRay(transform.position, shipVelocity, Color.blue);

        // Scale of the ship's current velocity
        float mag = shipVelocity.magnitude;
        // frictionVec: Wheels' turning slowing down. 180deg from forward direction. Proportional to move speed
        Vector3 frictionVec = transform.forward * mag * groundFrictionMultiplier * -1f;
        // slideFrictionVec: Wheels skidding against the ground, slowing the ship down. 180deg from moving direction
        Vector3 slideFrictionVec = shipVelocity * slideFrictionMultiplier * -1f;

        // Look at angle between move speed and facing speed, and if it's above a threshold, add some additional friction to slow down the ship faster
        if (Mathf.Abs(Vector3.Angle(transform.forward, shipVelocity)) < driftAngle)
        {
            slideFrictionVec = Vector3.zero;
        }

        // If the ship is moving more backwards than forwards, invert the ground friction
        if (Mathf.Abs(Vector3.Angle(transform.forward, shipVelocity)) > 90f)
        {
            frictionVec *= -1f;
        }

        // Add the vectors together
        return (frictionVec + slideFrictionVec) * Time.deltaTime;
    }

    Vector3 AddWheelFriction()
    {
        Vector3 forwardFriction = transform.forward;
        Vector3 lateralFriction = transform.right;

        float angle = Vector3.SignedAngle(transform.forward, shipVelocity, Vector3.up);

        // Add or subtract velocity for each direction, based on the size of the angle
        Debug.Log(angle);

        Vector3 frictionVelocityAdd = Vector3.zero;

        // Angle > 0: Apply force to the left
        if (angle > 0f)
        {
            frictionVelocityAdd -= lateralFriction * slideFrictionMultiplier * Time.deltaTime;
        }
        // Angle < 0: Apply force to the right
        if (angle < 0f)
        {
            frictionVelocityAdd += lateralFriction * slideFrictionMultiplier * Time.deltaTime;
        }

        float unsignedAngle = Mathf.Abs(angle);

        // Moving forward: Apply backward friction
        if (unsignedAngle < 90)
        {
            frictionVelocityAdd -= forwardFriction * groundFrictionMultiplier * Time.deltaTime;
        }
        else
        {
            frictionVelocityAdd += forwardFriction * groundFrictionMultiplier * Time.deltaTime;
        }

        return frictionVelocityAdd;
    }

    void MoveShip(Vector3 vec, float turn)
    {
        //moveComponent.Move(vec * moveSpeedMultiplier * Time.deltaTime);

        body.AddForce(vec, ForceMode.Force);

        Vector3 torque = Vector3.up * turn;

        body.AddRelativeTorque(torque);
    }
}
