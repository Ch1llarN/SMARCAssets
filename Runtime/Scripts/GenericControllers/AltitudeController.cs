using UnityEngine;
using Force;


namespace Smarc.GenericControllers
{
    public enum AltitudeControlMode
    {
        AbsoluteAltitude,
        VerticalVelocity
    }

    [AddComponentMenu("Smarc/Generic Controllers/Altitude Controller")]
    public class AltitudeController : MonoBehaviour
    {
        public ArticulationBody RobotAB;
        public Rigidbody RobotRB;
        private MixedBody robotBody;

        public AltitudeControlMode ControlMode = AltitudeControlMode.AbsoluteAltitude;
        [Tooltip("If true, controller will add force to compensate for gravity, this makes the robot float by default")]
        public bool CompensateGravity = true;
        [Tooltip("If true, the total mass and COM calculations will include all child rigidbodies/articulation bodies. If your robot is very complex, the controller might behave funny.")]
        public bool IncludeChildren = false;

        public float AscentRate = 2.0f;
        public float DescentRate = 2.0f;
        [Tooltip("Set to 0 to disable force capping")]
        public float MaxForce = 0f;


        [Header("Velocity Settings")]
        public float TargetVelocity = 0.0f;

        [Header("Position Settings")]
        public float TargetAltitude = 10.0f;
        public float AltitudeTolerance = 0.1f;
        public float GroundLevel = 0f; 


        [Header("Velocity PID")]
        public float VelKp = 5.0f;
        public float VelKi = 0.0f;
        public float VelKd = 0.0f;
        public float VelIntegratorLimit = 5f; // limits integral term (in meter-seconds)
        PID velPID;

        // Use a generated object to apply force at center of mass, parented to the robot transform
        // This way, we don't have to recalculate the world position of the COM every frame
        Transform COM;
        float totalMass;



        void Start()
        {
            robotBody = new MixedBody(RobotAB, RobotRB);
            velPID = new PID(VelKp, VelKi, VelKd, VelIntegratorLimit, maxOutput: MaxForce);
            totalMass = robotBody.GetTotalConnectedMass(includeChildren:IncludeChildren);
            var globalCom = robotBody.GetTotalConnectedCenterOfMass(includeChildren:IncludeChildren);
            COM = new GameObject("AltitudeController_COM").transform;
            COM.parent = robotBody.transform;
            COM.position = globalCom;
        }

        void FixedUpdate()
        {
            if (ControlMode == AltitudeControlMode.AbsoluteAltitude)
            {
                float diff = TargetAltitude - (robotBody.transform.position.y - GroundLevel);
                if (Mathf.Abs(diff) <= AltitudeTolerance) TargetVelocity = 0f;
                else TargetVelocity = Mathf.Sign(diff) * ((diff > 0) ? AscentRate : DescentRate);
            }
            
            VelocityControl();
        }

        float LimitAccelation(float desiredAcc, float currentVel, float deltaTime)
        {
            // limit acceleration so we don't instantly exceed configured ascent/descent rates
            float maxAccThisStepUp = (AscentRate - currentVel) / deltaTime;
            float maxAccThisStepDown = (-DescentRate - currentVel) / deltaTime;
            float minAcc = Mathf.Min(maxAccThisStepDown, maxAccThisStepUp);
            float maxAcc = Mathf.Max(maxAccThisStepDown, maxAccThisStepUp);
            return Mathf.Clamp(desiredAcc, minAcc, maxAcc);
        }


        void VelocityControl()
        {
            float currentVel = robotBody.velocity.y;
            float pidAcc = velPID.Update(TargetVelocity, currentVel, Time.fixedDeltaTime);
            pidAcc = LimitAccelation(pidAcc, currentVel, Time.fixedDeltaTime);

            float requiredForce = CompensateGravity ? pidAcc + totalMass * -Physics.gravity.y : pidAcc;
            requiredForce = MaxForce > 0f ? Mathf.Clamp(requiredForce, -MaxForce, MaxForce) : requiredForce;

            Vector3 upForce = Vector3.up * requiredForce;
            robotBody.AddForceAtPosition(upForce, COM.position, ForceMode.Force);
            Debug.DrawRay(COM.position, upForce * 0.1f, Color.red);
        }
        

        void OnDrawGizmosSelected()
        {
            // Draw target altitude line
            Gizmos.color = Color.green;
            Transform tf = this.transform;
            if(RobotAB != null)
                tf = RobotAB.transform;
            else if(RobotRB != null)
                tf = RobotRB.transform;
            Vector3 startPos = tf.position;
            Vector3 endPos = new(tf.position.x, GroundLevel + TargetAltitude, tf.position.z);
            Gizmos.DrawLine(startPos, endPos);
        }
    }
}