using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor.Build.Content;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

public class Boid : MonoBehaviour
{
    public static List<Boid> boidList;
    private Vector3 boundaryCenter;
    private float boundaryRadius = 1.5f;
    private bool turningAround = false;
    private Vector3 vectorBetween, velocityOther, targetPosition;
    private Quaternion targetRotation;
    private Vector3 velocity;
    private float speed;

    private RNNRLAgent agent;
    private Dictionary<Boid, (Vector3, Vector3, Vector3, float)> neighbors;
    private float sqrPerceptionRange, sqrMagnitudeTemp;
    public float perceptionRange = 1.0f;
    private Vector3 acceleration, separationForce, alignmentForce, cohesionForce;
    public float cohesionStrength = 0.4f;
    public float alignmentStrength = 1f;
    public float separationStrength = 1f;
    public float closestDistance = 100; //maybe change

    // Start is called before the first frame update
    void Start()
    {
        //agent = new RNNRLAgent(8, 10, 3);
        Initialize();
    }

    public void Initialize()
    {
        transform.forward = UnityEngine.Random.insideUnitSphere.normalized;
        speed = UnityEngine.Random.Range(0.1f, 1.5f);
        velocity = transform.forward * speed;
    }

    // Update is called once per frame
    void Update()
    {
        TurnAtBounds();
        //float[] inputs = new float[] { velocity.x, velocity.y, velocity.z, cohesionForce.x, cohesionForce.y, cohesionForce.z, closestDistance };
        //float[] output = agent.ForwardPropagation(inputs);
        Move();
    }
    public void SetBoundarySphere(Vector3 center, float radius)
    {
        boundaryCenter = center;
        boundaryRadius = radius;
    }

    private void Move()
    {
        // Update velocity by (clamped) acceleration
        //acceleration = Vector3.ClampMagnitude(acceleration, boidSettings.maxAccel);
        // velocity = Vector3.Lerp(velocity, velocity + acceleration, Time.deltaTime * boidSettings.speed * 2);
        //velocity += acceleration;
        velocity = Vector3.ClampMagnitude(velocity, speed);
        if (velocity.sqrMagnitude <= .1f)
            velocity = transform.forward * speed;
        // Update position and rotation
        if (velocity != Vector3.zero)
        {
            transform.position += velocity * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(velocity);
        }
    }

    private void TurnAtBounds()
    {
        if ((transform.position - boundaryCenter).sqrMagnitude >= (boundaryRadius * boundaryRadius))
        {
            if (!turningAround)
            {
                // If not already turning around, set a new target position on the opposite side of the boundary sphere
                targetPosition = boundaryCenter + (boundaryCenter - transform.position);
                turningAround = true;
            }
            // Keep turning and moving towards targetPosition until targetRotation reached
            targetRotation = Quaternion.LookRotation(targetPosition - transform.position);
            //targetRotation = Quaternion.LookRotation(targetPosition); original but changed to above and now works and fish don't go out of bounds so much 

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * speed);
            velocity = Vector3.Slerp(velocity, targetPosition - transform.position, Time.deltaTime * speed);
        }
        // After reaching target rotation (within range), stop turning around
        else if (Quaternion.Angle(transform.rotation, targetRotation) <= .01f)
            turningAround = false;
    }

    private void FindNeighbors()
    {
        neighbors.Clear();

        // SqrMagnitude is a bit faster than Magnitude since it doesn't require sqrt function
        sqrPerceptionRange = perceptionRange * perceptionRange;
        // Reset values before looping through neighbors
        velocityOther = vectorBetween = Vector3.zero;
        sqrMagnitudeTemp = 0f;
        foreach (Boid other in boidList)
        {
            velocityOther = other.velocity;
            vectorBetween = other.transform.position - transform.position;
            sqrMagnitudeTemp = vectorBetween.sqrMagnitude;
            if (sqrMagnitudeTemp < sqrPerceptionRange)
            {
                // Skip self
                if (other != this)
                {
                    // Store the neighbor Boid as dictionary for fast lookups, with value = a tuple of Vector3 position, velocityOther, vectorBetween, and float of the distance squared.
                    neighbors.Add(other, (other.transform.position, velocityOther, vectorBetween, sqrMagnitudeTemp));
                }
            }
        }
    }
    private void GetAverageDirection()
    {
        if (neighbors == null || neighbors.Count <= 0)
            return;
        else
        {
            foreach (KeyValuePair<Boid, (Vector3, Vector3, Vector3, float)> item in neighbors)
            {
                alignmentForce += item.Value.Item2; // Sum all neighbor velocities (Item2 == velocity)
            }
            alignmentForce /= neighbors.Count;
            //alignmentForce *= alignmentStrength;
            //alignmentForce = Vector3.ClampMagnitude(alignmentForce, boidSettings.maxAccel);
            //return alignmentForce;
        }
    }

    private void GetMassCenterOfNeighbors()
    {
        if (neighbors == null || neighbors.Count <= 0)
            return;
        else
        {
            cohesionForce = Vector3.zero;
            foreach (KeyValuePair<Boid, (Vector3, Vector3, Vector3, float)> item in neighbors)
            {
                cohesionForce += item.Value.Item1; // Sum all neighbor positions (Item1 == position)
            }
            cohesionForce /= neighbors.Count;   // Get average position (center)
            cohesionForce -= transform.position; // Convert to a vector pointing from boid to center
            //cohesionForce *= cohesionStrength;
            //cohesionForce = Vector3.ClampMagnitude(cohesionForce, maxAccel);
            //return cohesionForce;
        }
    }

    private void GetDistanceToClosestNeighbor()
    {
        if (neighbors == null || neighbors.Count <= 0)
            return;
        else
        {
            float distance = 10000;
            foreach (KeyValuePair<Boid, (Vector3, Vector3, Vector3, float)> item in neighbors)
            {
                // Adjust range depending on strength
                if (item.Value.Item4 < distance)  // Item4 == squaredDistance
                {
                    distance = item.Value.Item4;   // Item3 == vectorBetween
                }
            }
            closestDistance = Mathf.Sqrt(distance);
        }
        //return 0.0f;
    }
}
