using UnityEngine;
using DefaultNamespace;
using Force;
using UnityEngine.UIElements;

/// <summary>
/// Source: https://github.com/GaryMcWhorter/Verlet-Chain-Unity/tree/main
/// Modified to interact with rigid/arti bodies on the two ends
/// </summary>

[RequireComponent(typeof(LineRenderer))]
public class RopeVerlet : MonoBehaviour
{
    [Header("Physical Properties")]
    public Rigidbody StartRB;
    public ArticulationBody StartAB;
    MixedBody StartBody;

    public Rigidbody EndRB;
    public ArticulationBody EndAB;
    MixedBody EndBody;


    [Min(0f)]
    public float RopeLength = 1f;
    [Min(2), Tooltip("The number of chain links. Decreases performance with high values and high iteration")]
    public int SegmentCount = 10;
    [Min(0.01f), Tooltip("The mass of each chain link, used for calculating how much force to apply to locked nodes")]
    public float SegmentMass = 1f;

    [Space]

    [Header("Instanced Mesh Details")]
    [SerializeField, Tooltip("The Mesh of chain link to render")] 
    Mesh link;

    [SerializeField, Tooltip("The chain link material, must have gpu instancing enabled!")] 
    Material linkMaterial;


    [Header("Verlet Parameters")]
    [Tooltip("The distance between each link in the chain")] 
    float nodeDistance{ get { return RopeLength / (SegmentCount - 1); } }

    [SerializeField, Tooltip("The radius of the sphere collider used for each chain link")] 
    float nodeColliderRadius = 0.2f;

    [SerializeField, Tooltip("Works best with a lower value")] 
    float gravityStrength = 2f;


    [SerializeField, Range(0, 1), Tooltip("Modifier to dampen velocity so the simulation can stabilize")] 
    float velocityDampen = 0.95f;

    [SerializeField, Range(0, 0.99f), Tooltip("The stiffness of the simulation. Set to lower values for more elasticity")] 
    float stiffness = 0.8f;

    [SerializeField, Tooltip("Setting this will test collisions for every n iterations. Possibly more performance but less stable collisions")] 
    int iterateCollisionsEvery = 1;

    [SerializeField, Tooltip("Iterations for the simulation. More iterations is more expensive but more stable")] 
    int iterations = 100;

    [SerializeField, Tooltip("How many colliders to test against for every node.")] 
    int colliderBufferSize = 1;

    Collider[] colliderHitBuffer;


    // Need a better way of stepping through collisions for high Gravity
    // And high Velocity
    Vector3 gravity;


    [Space]

    // For Debug Drawing the chain/rope
    [Header("Line Renderer")]
    [SerializeField, Tooltip("Width for the line renderer")] 
    float ropeWidth = 0.1f;

    LineRenderer lineRenderer;
    Vector3[] linePositions;

    Vector3[] previousNodePositions;

    Vector3[] currentNodePositions;
    Quaternion[] currentNodeRotations;

    SphereCollider nodeCollider;
    GameObject nodeTester;
    Matrix4x4[] matrices;


   

    void Awake()
    {
        StartBody = new(StartAB, StartRB);
        EndBody = new(EndAB, EndRB);

        currentNodePositions = new Vector3[SegmentCount];
        previousNodePositions = new Vector3[SegmentCount];
        currentNodeRotations = new Quaternion[SegmentCount];

        colliderHitBuffer = new Collider[colliderBufferSize];
        gravity = new Vector3(0, -gravityStrength, 0);
        lineRenderer = this.GetComponent<LineRenderer>();

        // using a single dynamically created GameObject to test collisions on every node
        nodeTester = new GameObject
        {
            name = "Node Tester",
            layer = 8
        };
        nodeCollider = nodeTester.AddComponent<SphereCollider>();
        nodeCollider.radius = nodeColliderRadius;


        matrices = new Matrix4x4[SegmentCount];

        Vector3 startPosition = Vector3.zero;
        if (StartBody.isValid && EndBody.isValid)
        {
            startPosition = (StartBody.position + EndBody.position) * 0.5f;
        }
        else if (StartBody.isValid)
        {
            startPosition = StartBody.position;
        }
        else if (EndBody.isValid)
        {
            startPosition = EndBody.position;
        }
        for (int i = 0; i < SegmentCount; i++)
        {

            currentNodePositions[i] = startPosition;
            currentNodeRotations[i] = Quaternion.identity;

            previousNodePositions[i] = startPosition;

            matrices[i] = Matrix4x4.TRS(startPosition, Quaternion.identity, Vector3.one);

            startPosition.y -= nodeDistance;

        }

        // for line renderer data
        linePositions = new Vector3[SegmentCount];


    }


    void Update()
    {
        DrawRope();

        Mesh scaledLink = link;
        if (link.bounds.size.z != nodeDistance)
        {
            // if the link mesh isn't the correct length, scale it in the shader using instancing data
            scaledLink = Instantiate(link);
            float s = nodeDistance / link.bounds.size.z;
            Vector3 scale = new Vector3(s,s,s);
            Vector3[] vertices = scaledLink.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = Vector3.Scale(vertices[i], scale);
            }
            scaledLink.vertices = vertices;
            scaledLink.RecalculateBounds();
        }
        // Instanced drawing here is really performant over using GameObjects
        Graphics.DrawMeshInstanced(scaledLink, 0, linkMaterial, matrices, SegmentCount);
    }

    private void FixedUpdate()
    {
        Simulate();

        for (int i = 0; i < iterations; i++)
        {
            ApplyConstraint();

            if(i % iterateCollisionsEvery == 0)
            {
                AdjustCollisions();
            }
        }

        SetAngles();
        TranslateMatrices();
    }

    private void Simulate()
    {
        var fixedDt = Time.fixedDeltaTime;
        for (int i = 0; i < SegmentCount; i++)
        {
            Vector3 velocity = currentNodePositions[i] - previousNodePositions[i];
            velocity *= velocityDampen;

            previousNodePositions[i] = currentNodePositions[i];

            // calculate new position
            Vector3 newPos = currentNodePositions[i] + velocity;
            newPos += gravity * fixedDt;
            Vector3 direction = currentNodePositions[i] - newPos;

            currentNodePositions[i] = newPos;
        }
    }
    
    private void AdjustCollisions()
    {
        for (int i = 0; i < SegmentCount; i++)
        {
            if(i % 2 == 0) continue;

            int result = -1;
            result = Physics.OverlapSphereNonAlloc(currentNodePositions[i], nodeColliderRadius + 0.01f, colliderHitBuffer, ~(1 << 8));
            for (int n = 0; n < result; n++)
            {

                // check if this collider is one of our end locks, if so skip it
                if (StartBody.isValid && colliderHitBuffer[n].gameObject == StartBody.gameObject) continue;
                if (EndBody.isValid && colliderHitBuffer[n].gameObject == EndBody.gameObject) continue;
                Vector3 colliderPosition = colliderHitBuffer[n].transform.position;
                Quaternion colliderRotation = colliderHitBuffer[n].gameObject.transform.rotation;

                Vector3 dir;
                float distance;

                Physics.ComputePenetration(nodeCollider, currentNodePositions[i], Quaternion.identity, colliderHitBuffer[n], colliderPosition, colliderRotation, out dir, out distance);
                
                currentNodePositions[i] += dir * distance;
            }
        }

    }    


    private void ApplyConstraint()
    {
        if(StartBody.isValid) currentNodePositions[0] = StartBody.position;
        if(EndBody.isValid) currentNodePositions[SegmentCount - 1] = EndBody.position;

        for (int i = 0; i < SegmentCount - 1; i++)
        {
            var node1 = currentNodePositions[i];
            var node2 = currentNodePositions[i + 1];

            // Get the current distance between rope nodes
            float currentDistance = (node1 - node2).magnitude;
            float difference = Mathf.Abs(currentDistance - nodeDistance);
            Vector3 direction = Vector3.zero;

            // determine what direction we need to adjust our nodes
            if (currentDistance > nodeDistance)
            {
                direction = (node1 - node2).normalized;
            }
            else if (currentDistance < nodeDistance)
            {
                direction = (node2 - node1).normalized;
            }

            // calculate the movement vector, split equally between nodes by default
            Vector3 totalMovement = direction * difference * stiffness;
            Vector3 movement_i = totalMovement * 0.5f;
            Vector3 movement_i1 = totalMovement * 0.5f;
            
            // if (i==0 && StartBody.isValid)
            // {
            //     // split the movement between the two nodes based on their mass, so the heavier locked node moves less than the lighter unlocked node
            //     // and use the mass of the locked node to determine how much movement to apply to the unlocked node
            //     movement_i = totalMovement * (SegmentMass / (SegmentMass + StartBody.mass));
            //     movement_i1 = totalMovement - movement_i;
            //     StartBody.AddForceAtPosition(StartBody.mass * Time.fixedDeltaTime * -movement_i, currentNodePositions[i], ForceMode.Impulse);
            // }

            // if (i == SegmentCount - 2 && EndBody.isValid)
            // {
            //     movement_i1 = totalMovement * (SegmentMass / (SegmentMass + EndBody.mass));
            //     movement_i = totalMovement - movement_i1;
            //     EndBody.AddForceAtPosition(EndBody.mass * Time.fixedDeltaTime * movement_i1, currentNodePositions[i + 1], ForceMode.Impulse);
            // }
            
            // apply correction
            currentNodePositions[i] -= movement_i;
            currentNodePositions[i + 1] += movement_i1;
        }
    }

    void SetAngles()
    {
        for (int i = 0; i < SegmentCount - 1; i++)
        {
            var node1 = currentNodePositions[i];
            var node2 = currentNodePositions[i + 1];

            var dir = (node2 - node1).normalized;
            if(dir != Vector3.zero)
            {
                if( i > 0)
                {
                    Quaternion desiredRotation = Quaternion.LookRotation(dir, Vector3.right);
                    currentNodeRotations[i + 1] = desiredRotation;
                }
                else if( i < SegmentCount - 1)
                {
                    Quaternion desiredRotation = Quaternion.LookRotation(dir, Vector3.right);
                    currentNodeRotations[i + 1] = desiredRotation;
                }
                else
                {
                    Quaternion desiredRotation = Quaternion.LookRotation(dir, Vector3.right);
                    currentNodeRotations[i] = desiredRotation;
                }
            }

            if( i % 2 == 0 && i != 0)
            {
                currentNodeRotations[i + 1] *= Quaternion.Euler(0, 0, 90);
            }
        }
    }

    void TranslateMatrices()
    {
        for(int i = 0; i < SegmentCount; i++)
        {
            matrices[i].SetTRS(currentNodePositions[i], currentNodeRotations[i], Vector3.one);
        }
    }

    private void DrawRope()
    {
        lineRenderer.startWidth = ropeWidth;
        lineRenderer.endWidth = ropeWidth;

        for (int n = 0; n < SegmentCount; n++)
        {
            linePositions[n] = currentNodePositions[n];
        }

        lineRenderer.positionCount = linePositions.Length;
        lineRenderer.SetPositions(linePositions);
    }

}
