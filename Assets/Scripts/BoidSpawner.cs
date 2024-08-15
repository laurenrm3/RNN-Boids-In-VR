using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Build.Content;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

public class BoidSpawner : MonoBehaviour
{
    public int boidCount = 10;
    private Vector3 boidLocation;
    public float boundaryRadius = 1.5f;
    public GameObject boidPrefab;
    [SerializeField] private Transform spawnLocation;

    [SerializeField] public Volume volume;
    private Boolean toKill = false;

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
            //newBoid = this.AddComponent<Boid>();
            newBoid.SetBoundarySphere(spawnLocation.position, boundaryRadius);
        }
    }

    void Update()
    {
 
    }

    private IEnumerator killEffect()
    {
        float intensity = 1f;
        //_vignette.enabled.Override(true);
        //_vignette.active = true;
        volume.weight = intensity;
        yield return new WaitForSeconds(intensity);
        while (intensity > 0)
        {
            intensity -= 0.2f;
            if (intensity < 0) intensity = 0;
            volume.weight = intensity;
            yield return new WaitForSeconds(0.1f);
        }
        //_vignette.enabled.Override(false);
        volume.weight = 0;
        yield break;
    }
}
