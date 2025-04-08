using UnityEngine;

public class MovingTarget : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float speedVariation = 0.5f;
    public Transform plane; // Reference to the plane transform
    private Vector3 moveDirection;
    private Vector3 startPosition;
    private float planeHalfWidth;
    private float planeHalfLength;

    void Start()
    {
        if (plane == null)
        {
            Debug.LogError("Please assign the plane transform in the inspector!");
            return;
        }

        // Get the plane's dimensions from its scale
        // Assuming the plane is oriented along XZ plane and scale represents size
        planeHalfWidth = plane.localScale.x * 5f; // Unity's default plane is 10 units wide
        planeHalfLength = plane.localScale.z * 5f; // Unity's default plane is 10 units long
        
        startPosition = transform.position;
        SetRandomDirection();
        InvokeRepeating(nameof(SetRandomDirection), 2f, 2f);
    }

    void Update()
    {
        // Move the target
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        // Check boundaries based on plane size
        if (Mathf.Abs(transform.position.x - plane.position.x) > planeHalfWidth || 
            Mathf.Abs(transform.position.z - plane.position.z) > planeHalfLength)
        {
            ResetPosition();
        }
    }

    void SetRandomDirection()
    {
        float x = Random.Range(-1f, 1f);
        float z = Random.Range(-1f, 1f);
        moveDirection = new Vector3(x, 0, z).normalized;
        
        // Randomly vary the speed
        moveSpeed = Random.Range(2f - speedVariation, 2f + speedVariation);
    }

    void ResetPosition()
    {
        transform.position = startPosition;
        SetRandomDirection();
    }
}
