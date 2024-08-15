using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.Build.Content;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

public class Boid : MonoBehaviour
{
    public static List<Boid> boidList;
    private Vector3 boundaryCenter;
    private float boundaryRadius = 5f;
    private bool turningAround = false;
    private Vector3 vectorBetween, velocityOther, targetPosition;
    private Quaternion targetRotation;
    
    private Vector3 velocity;
    private Vector3 prevVelocity;
    
    private float speed;

    private RNNRLAgent agent;
    private Dictionary<Boid, (Vector3, Vector3, Vector3, float, int)> neighbors;
    private float sqrPerceptionRange, sqrMagnitudeTemp;
    public float perceptionRange = 1.0f;
    private Vector3 acceleration, separationForce, alignmentForce, cohesionForce;
    public float cohesionStrength = 0.4f;
    public float alignmentStrength = 1f;
    public float separationStrength = 1f;
    public float closestDistance = 100; //maybe change

    

    private void Awake()
    {
        if (boidList == null)
            boidList = new List<Boid>();
        boidList.Add(this);
        if (neighbors == null)
            neighbors = new Dictionary<Boid, (Vector3, Vector3, Vector3, float, int)>();
    }

    void Start()
    {
        agent = new RNNRLAgent(10, 10, 3);
        Initialize();
        InvokeRepeating("RunProgram", 1.0f, 2.0f);
    }

    public void Initialize()
    {
        transform.forward = UnityEngine.Random.insideUnitSphere.normalized;
        speed = UnityEngine.Random.Range(0.1f, 4f);
        velocity = transform.forward * speed;
        prevVelocity = velocity;
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log("here");
        FindNeighbors();       
        TurnAtBounds();
        Move();
    }

    public void RunProgram()
    {
        //kill();
        if (!outOfBounds())
        {
            GetAverageDirection();
            GetMassCenterOfNeighbors();
            GetDistanceToClosestNeighbor();
            float[] inputs = new float[] { prevVelocity.x, prevVelocity.y, prevVelocity.z, alignmentForce.x, alignmentForce.y, alignmentForce.z, cohesionForce.x, cohesionForce.y, cohesionForce.z, closestDistance };
            float[] output = agent.ForwardPropagation(inputs);

            prevVelocity = velocity;
            velocity = new Vector3(output[0], output[1], output[2]);
            Debug.Log("running RNN");
        }
        else {
            Debug.Log("out of bounds");
            TurnAtBounds();
            Move();
        }
            

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
        /*if (velocity.sqrMagnitude <= .1f)
            velocity = transform.forward * speed;*/

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
            //targetRotation = Quaternion.LookRotation(targetPosition - transform.position);
            targetRotation = Quaternion.LookRotation(targetPosition); //original but changed to above and now works and fish don't go out of bounds so much 

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * speed);
            velocity = Vector3.Slerp(velocity, targetPosition - transform.position, Time.deltaTime * speed);
        }
        // After reaching target rotation (within range), stop turning around
        else if (Quaternion.Angle(transform.rotation, targetRotation) <= .01f)
            turningAround = false;
    }

    private void FindNeighbors()
    {
        //neighbors.Clear();

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
                    if (neighbors.ContainsKey(other))
                    {
                        int count = neighbors[other].Item5 + 1;
                        neighbors[other] = (other.transform.position, velocityOther, vectorBetween, sqrMagnitudeTemp, count);
                    }
                    else
                    {
                        neighbors.Add(other, (other.transform.position, velocityOther, vectorBetween, sqrMagnitudeTemp, 0));
                    }
                    // Store the neighbor Boid as dictionary for fast lookups, with value = a tuple of Vector3 position, velocityOther, vectorBetween, and float of the distance squared.
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
            foreach (KeyValuePair<Boid, (Vector3, Vector3, Vector3, float, int)> item in neighbors)
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
            foreach (KeyValuePair<Boid, (Vector3, Vector3, Vector3, float, int)> item in neighbors)
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
            foreach (KeyValuePair<Boid, (Vector3, Vector3, Vector3, float,int)> item in neighbors)
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

    private void kill(ref Boolean toKill)
    {
 /*       foreach (KeyValuePair<Boid, (Vector3, Vector3, Vector3, float, int)> item in neighbors)
        {
            if (item.Value.Item5 > 3)  
            {
                Debug.Log("killed");
                neighbors[item.Key] = (item.Value.Item1, item.Value.Item2, item.Value.Item3, item.Value.Item4, 0);
            }
            else
                Debug.Log(item.Value.Item5);
        }*/

        //get key collection from dictionary into a list to loop through
        List<Boid> keys = new List<Boid>(neighbors.Keys);
        foreach (Boid item in keys)
        {
            (Vector3, Vector3, Vector3, float, int) curr = neighbors[item];
            if (curr.Item5 > 3)
            {
                Debug.Log("killed");
                //StartCoroutine(killEffect());
                toKill = true;
                neighbors[item] = (curr.Item1, curr.Item2, curr.Item3, curr.Item4, 0);
            }
            else
            {
                Debug.Log(curr.Item5);
                toKill = false;
            }

        }
    }

    private Boolean outOfBounds()
    {
        return ((transform.position - boundaryCenter).sqrMagnitude >= (boundaryRadius * boundaryRadius));
    }


}
