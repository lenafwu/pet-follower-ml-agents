using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;

public class TargetFollow : Agent
{
    public Transform target;
    public float moveSpeed = 1.5f;
    public float rotateSpeed = 200f;
    public float maxSpeed = 3f;

    [Header("Reward Settings")]
    public float facingRewardMultiplier = 0.1f;
    public float movingTowardRewardMultiplier = 0.5f;
    public float reachingReward = 10.0f;
    public float fallingPenalty = -1.0f;
    public float rotationPenaltyMultiplier = 0.01f;
    public float movementBonus = 0.05f;

    private Rigidbody rb;
    private float lastDistanceToTarget;
    private int stepsWithoutProgress = 0;
    private const int MAX_STEPS_WITHOUT_PROGRESS = 100;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        // Spawn near the target
        transform.localPosition = target.localPosition + new Vector3(Random.Range(-2f, 2f), 0.5f, Random.Range(-2f, 2f));

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        lastDistanceToTarget = Vector3.Distance(transform.localPosition, target.localPosition);
        stepsWithoutProgress = 0;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 directionToTarget = (target.localPosition - transform.localPosition).normalized;
        sensor.AddObservation(directionToTarget);                      // 3
        sensor.AddObservation(rb.velocity.normalized);                // 3
        float distanceToTarget = Vector3.Distance(transform.localPosition, target.localPosition);
        sensor.AddObservation(distanceToTarget);                      // 1
        sensor.AddObservation(Mathf.Clamp01(distanceToTarget / 20f)); // 1
        // Total: 8 observations
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float rotate = actions.ContinuousActions[0];      // A/D
        float moveForward = actions.ContinuousActions[1]; // W/S

        // Rotate
        transform.Rotate(Vector3.up * rotate * rotateSpeed * Time.deltaTime);

        // Move
        rb.velocity = transform.forward * moveForward * moveSpeed;

        // Clamp speed
        if (rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }

        // === Rewards ===
        Vector3 directionToTarget = (target.localPosition - transform.localPosition).normalized;
        float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
        float currentDistanceToTarget = Vector3.Distance(transform.localPosition, target.localPosition);

        // Reward for facing the target AND moving
        float facingReward = Mathf.Cos(angleToTarget * Mathf.Deg2Rad);
        if (facingReward > 0.5f && moveForward > 0.1f)
        {
            AddReward(facingReward * facingRewardMultiplier * Time.deltaTime);
        }

        // Penalty for rotation
        AddReward(-Mathf.Abs(rotate) * rotationPenaltyMultiplier);

        // Bonus for moving
        if (moveForward > 0.1f)
        {
            AddReward(movementBonus * Time.deltaTime);
        }

        // Reward for approaching the target
        float distanceChange = lastDistanceToTarget - currentDistanceToTarget;
        if (distanceChange > 0.01f)
        {
            AddReward(distanceChange * movingTowardRewardMultiplier * 2f);
            stepsWithoutProgress = 0;
        }
        else
        {
            stepsWithoutProgress++;
        }

        // Penalize if stuck too long
        if (stepsWithoutProgress > MAX_STEPS_WITHOUT_PROGRESS)
        {
            AddReward(-0.5f);
            EndEpisode();
        }

        // Reward for reaching target
        if (currentDistanceToTarget < 1.0f)
        {
            AddReward(reachingReward);
            EndEpisode();
        }

        // Penalty for falling
        if (transform.localPosition.y < 0)
        {
            AddReward(fallingPenalty);
            EndEpisode();
        }

        lastDistanceToTarget = currentDistanceToTarget;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Horizontal"); // rotate
        continuousActions[1] = Input.GetAxis("Vertical");   // move
    }
}
