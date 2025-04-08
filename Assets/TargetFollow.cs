using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;

public class TargetFollow : Agent
{
    public Transform target;
    public float moveSpeed = 3f;
    public float rotateSpeed = 300f;
    public float maxSpeed = 5f;
    
    [Header("Reward Settings")]
    public float facingRewardMultiplier = 0.3f;
    public float movingTowardRewardMultiplier = 1.0f;
    public float reachingReward = 5.0f;
    public float fallingPenalty = -2.0f;
    public float rotationPenaltyMultiplier = 0.005f;
    public float movementBonus = 0.1f;
    
    private Rigidbody rb;
    private Vector3 lastPosition;
    private float lastDistanceToTarget;
    private int stepsWithoutProgress = 0;
    private const int MAX_STEPS_WITHOUT_PROGRESS = 150;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        // Reset agent position with a smaller range to make initial learning easier
        transform.localPosition = new Vector3(Random.Range(-2f, 2f), 0.5f, Random.Range(-2f, 2f));

    

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        
        lastPosition = transform.position;
        lastDistanceToTarget = Vector3.Distance(transform.localPosition, target.localPosition);
        stepsWithoutProgress = 0;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Direction to target (relative)
        Vector3 directionToTarget = (target.localPosition - transform.localPosition).normalized;
        sensor.AddObservation(directionToTarget);                    // 3

        // Agent's forward direction
        sensor.AddObservation(transform.forward.normalized);         // 3

        // Agent velocity
        sensor.AddObservation(rb.linearVelocity.normalized);               // 3

        // Distance info
        float distanceToTarget = Vector3.Distance(transform.localPosition, target.localPosition);
        sensor.AddObservation(distanceToTarget);                     // 1
        sensor.AddObservation(Mathf.Clamp01(distanceToTarget / 10f));// 1
    }


    public override void OnActionReceived(ActionBuffers actions)
    {
        float rotate = actions.ContinuousActions[0];
        float moveForward = actions.ContinuousActions[1];

        // Apply rotation and forward movement
        transform.Rotate(Vector3.up * rotate * rotateSpeed * Time.deltaTime);
        rb.linearVelocity = transform.forward * moveForward * moveSpeed;

        // Clamp velocity
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }

        // Directional reward logic
        Vector3 directionToTarget = (target.localPosition - transform.localPosition).normalized;
        float currentDistanceToTarget = Vector3.Distance(transform.localPosition, target.localPosition);

        float facingDot = Vector3.Dot(transform.forward.normalized, directionToTarget); // [-1, 1]

        // --- Facing Reward (only when moving)
        if (facingDot > 0.7f && moveForward > 0.1f)
        {
            AddReward(facingDot * facingRewardMultiplier * Time.deltaTime);
        }

        // --- Rotation Penalty
        AddReward(-Mathf.Abs(rotate) * rotationPenaltyMultiplier);

        // --- Movement Bonus
        if (moveForward > 0.1f)
        {
            AddReward(movementBonus * Time.deltaTime);
        }

        // --- Reward if moving closer while facing
        float distanceChange = lastDistanceToTarget - currentDistanceToTarget;
        if (distanceChange > 0.01f && facingDot > 0.7f && moveForward > 0.1f)
        {
            AddReward(distanceChange * movingTowardRewardMultiplier);
            stepsWithoutProgress = 0;
        }
        else
        {
            stepsWithoutProgress++;
        }

        // --- No progress timeout
        if (stepsWithoutProgress > MAX_STEPS_WITHOUT_PROGRESS)
        {
            AddReward(-0.2f);
            EndEpisode();
        }

        // --- Reached target
        if (currentDistanceToTarget < 1.0f)
        {
            AddReward(reachingReward);
            EndEpisode();
        }

        // --- Fell off
        if (transform.localPosition.y < 0)
        {
            AddReward(fallingPenalty);
            EndEpisode();
        }

        lastDistanceToTarget = currentDistanceToTarget;
    }


    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // For testing manually
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Horizontal");
        continuousActions[1] = Input.GetAxis("Vertical");
    }
}