using Force;
using Smarc.GenericControllers;
using UnityEngine;
using VehicleComponents.Actuators;
using VehicleComponents.Sensors;

namespace dji
{

    public enum DroneFlightState
    {
        Idle,
        TakingOff,
        Hovering,
        Flying,
        Landing
    }

    /// <summary>
    /// This controller bridges between the DJI interface and a simple force-based set of controllers (smarc/generic controllers)
    /// </summary>
    [RequireComponent(typeof(AltitudeController))]
    [RequireComponent(typeof(AttitudeController))]
    [RequireComponent(typeof(HorizontalController))]
    public class DJIController : MonoBehaviour
    {
        [Header("Settings")]
        public bool StartInAir = false;
        public bool GotControl = true;
        public DroneFlightState flightState = DroneFlightState.Idle;

        [Header("Payload")]
        public LoadCell WinchLoadCell;
        [Tooltip("If true, the alt. controller will be given this load as extra mass to compensate for.")]
        public bool CompensateForPayload = true; 


        [Header("Propellers(Upper)")]
        public Propeller frontLeftPropeller;
        public Propeller frontRightPropeller;
        public Propeller backLeftPropeller;
        public Propeller backRightPropeller;
        public float FloatRPM = 1000f;

        [Header("Propellers(Lower)")]
        [Tooltip("If true, will also control the downwards facing propellers")]
        public bool IsDualProp = false;
        public Propeller frontLeftDownPropeller;
        public Propeller frontRightDownPropeller;
        public Propeller backLeftDownPropeller;
        public Propeller backRightDownPropeller;

        float takeOffAltitude = 1.5f; // what the real thing does is 1.5m
        float homeAltitude; // altitude at which the drone took off

        AltitudeController altCtrl;
        AttitudeController attCtrl;
        HorizontalController horizCtrl;
        
        Vector3 commandedVelocity = Vector3.zero;
        float commandedYawRate = 0f;
        float lastCommandTime = 0f;

        MixedBody robotBody;

        float stoppedFor = 0f;

        void Awake()
        {
            altCtrl = GetComponent<AltitudeController>();
            altCtrl.ControlMode = AltitudeControlMode.VerticalVelocity;
            altCtrl.TargetVelocity = 0f;

            attCtrl = GetComponent<AttitudeController>();
            attCtrl.YawControlMode = YawControlMode.YawRate;
            attCtrl.TargetYawRate = 0f;
            attCtrl.TiltMode = TiltMode.ReactToAcceleration;

            horizCtrl = GetComponent<HorizontalController>();
            horizCtrl.ControlMode = HorizontalControlMode.Velocity;
            horizCtrl.TargetVelocity = Vector3.zero;

            robotBody = new MixedBody(altCtrl.RobotAB, altCtrl.RobotRB);
            homeAltitude = robotBody.position.y;

            Ignition(StartInAir);
            if (StartInAir)
            {
                flightState = DroneFlightState.Flying;
                altCtrl.ControlMode = AltitudeControlMode.AbsoluteAltitude;
                altCtrl.TargetAltitude = homeAltitude;
                GotControl = true;
            }
        }


        void FixedUpdate()
        {
            RPMsFromMotion();

            if (CompensateForPayload && WinchLoadCell != null) altCtrl.ExtraMassToCompensate = WinchLoadCell.Weight;

            if (!GotControl) return;
            

            switch(flightState)
            {
                case DroneFlightState.TakingOff:
                    TakingOff();
                    break;
                case DroneFlightState.Landing:
                    Landing();
                    break;
                case DroneFlightState.Flying:
                    // if we haven't received a command in a while, station keep still in the air
                    float timeout = 0.2f;
                    if (Time.time - lastCommandTime > timeout)
                    {   
                        Hover();
                        break;
                    }
                    CommandVelocities();
                    break;
                case DroneFlightState.Idle:
                case DroneFlightState.Hovering:
                default:
                    // do nothing
                    break;
            }
        }

        void TakingOff()
        {
            altCtrl.ControlMode = AltitudeControlMode.AbsoluteAltitude;
            altCtrl.TargetAltitude = homeAltitude + takeOffAltitude;
            if (robotBody.position.y >= altCtrl.TargetAltitude - altCtrl.AltitudeTolerance)
            {
                flightState = DroneFlightState.Flying;
                Debug.Log("Takeoff complete, now flying");
            }
        }

        void Landing()
        {
            altCtrl.ControlMode = AltitudeControlMode.VerticalVelocity;
            altCtrl.TargetVelocity = -altCtrl.DescentRate;
            bool stopped = Mathf.Abs(robotBody.velocity.y) <= 0.2f;
            if (stopped) stoppedFor += Time.fixedDeltaTime;
            else stoppedFor = 0f;
            bool stuck = stoppedFor >= 1.0f; // if we've been stopped for 1 second, consider ourselves stuck
            if (stuck)
            {
                flightState = DroneFlightState.Idle;
                Debug.Log("Landing complete, now idle");
                Ignition(false);
            }
        }

        void Hover()
        {
            Vector3 horizontalVelocity = new(robotBody.velocity.x, 0f, robotBody.velocity.z);
            if (horizontalVelocity.magnitude > 0.1f)
            {
                horizCtrl.ControlMode = HorizontalControlMode.Velocity;
                horizCtrl.TargetVelocity = Vector3.zero;
            }
            else
            {
                horizCtrl.ControlMode = HorizontalControlMode.UnityPosition;
                horizCtrl.TargetUnityPosition = robotBody.position;
            }

            altCtrl.ControlMode = AltitudeControlMode.AbsoluteAltitude;
            altCtrl.TargetAltitude = robotBody.position.y;

            attCtrl.YawControlMode = YawControlMode.YawRate;
            attCtrl.TargetYawRate = 0f;

            if (robotBody.velocity.magnitude <= 0.1f) flightState = DroneFlightState.Hovering;
        }

        void CommandVelocities()
        {
            horizCtrl.ControlMode = HorizontalControlMode.Velocity;
            horizCtrl.TargetVelocity = commandedVelocity;

            altCtrl.ControlMode = AltitudeControlMode.VerticalVelocity;
            altCtrl.TargetVelocity = commandedVelocity.y;

            attCtrl.YawControlMode = YawControlMode.YawRate;
            attCtrl.TargetYawRate = commandedYawRate;
        }

        public bool TakeOff()
        {
            if (!GotControl) 
            {
                Debug.Log("Cannot take off, do not have control");
                return false;
            }

            if (flightState != DroneFlightState.Idle)
            {
                Debug.Log("Cannot take off, drone not idle");
                return false;
            }

            Debug.Log("DJI Taking off");
            flightState = DroneFlightState.TakingOff;
            Ignition(true);
            homeAltitude = robotBody.position.y;
            return true;
        }

        public bool Land()
        {
            if (!GotControl) 
            {
                Debug.Log("Cannot land, do not have control");
                return false;
            }

            if (flightState != DroneFlightState.Flying)
            {
                Debug.Log("Cannot land, drone not flying");
                return false;
            }

            Debug.Log("DJI Landing");
            flightState = DroneFlightState.Landing;
            stoppedFor = 0f;
            return true;
        }

        public bool TakeControl()
        {
            if (GotControl)
            {
                Debug.Log("Already have control");
                return true;
            }
            Debug.Log("DJI Taking control");
            GotControl = true;
            return true;
        }

        public bool ReleaseControl()
        {
            if (!GotControl)
            {
                Debug.Log("Do not have control to release");
                return false;
            }
            Debug.Log("DJI Releasing control");
            GotControl = false;
            return true;
        }

        void Ignition(bool on)
        {
            if (!GotControl)
            {
                Debug.Log("Cannot change ignition state, do not have control");
                return;
            }

            frontLeftPropeller.SetRpm(on? FloatRPM : 0f);
            frontRightPropeller.SetRpm(on? FloatRPM : 0f);
            backLeftPropeller.SetRpm(on? FloatRPM : 0f);
            backRightPropeller.SetRpm(on? FloatRPM : 0f);
            altCtrl.enabled = on;
            altCtrl.CompensateGravity = on;
            attCtrl.enabled = on;
            horizCtrl.enabled = on;

            if (IsDualProp)
            {
                frontLeftDownPropeller.SetRpm(frontLeftPropeller.rpm);
                frontRightDownPropeller.SetRpm(frontRightPropeller.rpm);
                backLeftDownPropeller.SetRpm(backLeftPropeller.rpm);
                backRightDownPropeller.SetRpm(backRightPropeller.rpm);
            }
        }

        void RPMsFromMotion()
        {
            float tiltAngle = Vector3.Angle(robotBody.transform.up, Vector3.up);

            // more tilt = higher RPM needed for all the props
            float tiltFactor = 1f + (tiltAngle / 90f) * 0.5f; // scales from 1.0 to 1.5

            // + if the hub of a prop is moving up or down, it needs more or less RPM
            float maxSpeed = 5f;

            float propSpeedFactor(Propeller p)
            {
                MixedBody body = p.GetMixedBody();
                float verticalVelocity = Vector3.Dot(body.velocity, Vector3.up);
                return 1f + (verticalVelocity / maxSpeed);
            }

            float flSpeedFactor = propSpeedFactor(frontLeftPropeller);
            float frSpeedFactor = propSpeedFactor(frontRightPropeller);
            float blSpeedFactor = propSpeedFactor(backLeftPropeller);
            float brSpeedFactor = propSpeedFactor(backRightPropeller);

            frontLeftPropeller.SetRpm(FloatRPM * tiltFactor * flSpeedFactor);
            frontRightPropeller.SetRpm(FloatRPM * tiltFactor * frSpeedFactor);
            backLeftPropeller.SetRpm(FloatRPM * tiltFactor * blSpeedFactor);
            backRightPropeller.SetRpm(FloatRPM * tiltFactor * brSpeedFactor);

            if (IsDualProp)
            {
                frontLeftDownPropeller.SetRpm(frontLeftPropeller.rpm);
                frontRightDownPropeller.SetRpm(frontRightPropeller.rpm);
                backLeftDownPropeller.SetRpm(backLeftPropeller.rpm);
                backRightDownPropeller.SetRpm(backRightPropeller.rpm);
            }
        }

        public void CommandFLUYawRate(float forward, float left, float up, float yawRate)
        {
            if (!GotControl)
            {
                Debug.Log("Cannot command drone, do not have control");
                return;
            }
            // Unity is RUF, do the mapping here.
            commandedVelocity = new Vector3(-left, up, forward);
            commandedYawRate = -yawRate;
            lastCommandTime = Time.time;
            flightState = DroneFlightState.Flying;
        }

        public void CommandFLUYawRate01(float forward, float left, float up, float yawRate)
        {
            CommandFLUYawRate(forward * horizCtrl.MaxSpeed, left * horizCtrl.MaxSpeed, up * altCtrl.AscentRate, yawRate * attCtrl.DesiredYawRate);
        }
    }
}