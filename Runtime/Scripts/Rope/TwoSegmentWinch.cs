using UnityEngine;

namespace Rope
{
    public class TwoSegmentWinchController : MonoBehaviour
    {
        [Header("Components")]
        public ArticulationBody BaseSpherical;
        public ArticulationBody MiddlePrismatic;
        public BoxCollider MiddlePrismaticCollider;
        ArticulationDrive midPrismaticDrive;
        public ArticulationBody MiddleSpherical;
        public ArticulationBody EndPrismatic;
        public BoxCollider EndPrismaticCollider;
        ArticulationDrive endPrismaticDrive;

        [Header("Controls")]
        [Range(-2, 2)]
        public int WinchSpeed = 0;

        [Header("Settings")]
        public Vector3 Direction = Vector3.down;
        public float RopeLength = 5f;
        public float ColliderThickness = 0.1f;

        [Header("Current State")]
        public float CurrentRopeLength;


        void Awake()
        {
            midPrismaticDrive = MiddlePrismatic.xDrive;
            endPrismaticDrive = EndPrismatic.xDrive;

            // Ignore all self-collisions in the winch
            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach(var col in colliders)
                foreach(var col2 in colliders)
                    if (col != col2)
                        Physics.IgnoreCollision(col, col2);
                
            
            
        }

        void FixedUpdate()
        {
            midPrismaticDrive.targetVelocity = WinchSpeed/2f;
            MiddlePrismatic.xDrive = midPrismaticDrive;

            endPrismaticDrive.targetVelocity = WinchSpeed/2f;
            EndPrismatic.xDrive = endPrismaticDrive;

            CurrentRopeLength = -MiddlePrismatic.transform.localPosition.y + -EndPrismatic.transform.localPosition.y;

            SetColliders();
        }

        void SetColliders()
        {
            var midDist = -MiddlePrismatic.transform.localPosition.y;
            MiddlePrismaticCollider.size = new Vector3(ColliderThickness, midDist, ColliderThickness);
            MiddlePrismaticCollider.center = new Vector3(0, midDist/2, 0);

            var endDist = -EndPrismatic.transform.localPosition.y;
            EndPrismaticCollider.size = new Vector3(ColliderThickness, endDist, ColliderThickness);
            EndPrismaticCollider.center = new Vector3(0, endDist/2, 0);
        }

        public void ApplySettings()
        {
            // First, put the objects where they need to be when the winch is fully extended
            var midLen = Direction.normalized * RopeLength / 2f;

            BaseSpherical.transform.localPosition = Vector3.zero;
            MiddlePrismatic.transform.localPosition = midLen;
            MiddleSpherical.transform.localPosition = Vector3.zero;
            EndPrismatic.transform.localPosition = midLen;

            SetColliders();

            // Then, set the prismatics to have the correct min/max limits
            var midPrismaticDrive = MiddlePrismatic.xDrive;
            midPrismaticDrive.lowerLimit = 0;
            midPrismaticDrive.upperLimit = RopeLength / 2f;
            MiddlePrismatic.xDrive = midPrismaticDrive;

            var endPrismaticDrive = EndPrismatic.xDrive;
            endPrismaticDrive.lowerLimit = 0;
            endPrismaticDrive.upperLimit = RopeLength / 2f;
            EndPrismatic.xDrive = endPrismaticDrive;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(BaseSpherical.transform.position, MiddlePrismatic.transform.position);
            Gizmos.DrawLine(MiddleSpherical.transform.position, EndPrismatic.transform.position);
            Gizmos.DrawSphere(MiddleSpherical.transform.position, 0.05f);   
        }

    }
}