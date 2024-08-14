using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build.Content;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

public class BoidSpawner : MonoBehaviour
{
    public int boidCount = 10;
    private Vector3 boidLocation;
    public float boundaryRadius = 1.5f;
    public GameObject boidPrefab;
    [SerializeField] private Transform spawnLocation;

    // Start is called before the first frame update
    void Start()
    {
        SpawnBoidsIndividual();
    }

    private void SpawnBoidsIndividual()
    {
        // Create or clear BoidList
        if (Boid.boidList == null)
            Boid.boidList = new List<Boid>();
        else
            Boid.boidList.Clear();
        // Spawn Boids
        Boid newBoid;
        for (int i = 0; i < boidCount; i++)
        {
            boidLocation = UnityEngine.Random.insideUnitSphere.normalized * UnityEngine.Random.Range(0, boundaryRadius * 0.9f);
            newBoid = Instantiate(boidPrefab, boidLocation, Quaternion.identity, this.transform).GetComponent<Boid>();
            //newBoid.boidSettings = boidSettings;
            newBoid.SetBoundarySphere(spawnLocation.position, boundaryRadius);
        }
    }
}
