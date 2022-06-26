using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleSeek : MonoBehaviour
{
    public Transform target;
    public float force = 10.0f;

    new ParticleSystem particleSystem;
    ParticleSystem.Particle[] particles;

    ParticleSystem.MainModule particleSystemMainModule;

    void Start()
    {
        particleSystem = GetComponent<ParticleSystem>();
        particleSystemMainModule = particleSystem.main;
    }

    void LateUpdate()
    {
        int maxParticles = particleSystemMainModule.maxParticles;

        // as long as maxParticles is bigger then our list, we know how many particles we have
        if (particles == null || particles.Length < maxParticles)
        {
            // create a vector with the size of particle amount
            particles = new ParticleSystem.Particle[maxParticles];
        }

        // get every particle
        particleSystem.GetParticles(particles);

        //create force float outside the loop, so we save some cumputational capacity
        float forceDeltaTime = force * Time.deltaTime;

        //transform the target position out the loop
        Vector3 targetTransformedPosition;

        // to correct the offset take world space into account
        switch (particleSystemMainModule.simulationSpace)
        {
            case ParticleSystemSimulationSpace.Local:
                {
                    targetTransformedPosition = transform.InverseTransformPoint(target.position);
                    break;
                }
            case ParticleSystemSimulationSpace.Custom:
                {
                    targetTransformedPosition = particleSystemMainModule.customSimulationSpace.InverseTransformPoint(target.position);
                    break;
                }
            case ParticleSystemSimulationSpace.World:
                {
                    targetTransformedPosition = target.position;
                    break;
                }
            default:
                {
                    throw new System.NotSupportedException(

                        string.Format("Unsupported simulation space '{0}'.",
                        System.Enum.GetName(typeof(ParticleSystemSimulationSpace), particleSystemMainModule.simulationSpace)));
                }
        }

        int particleCount = particleSystem.particleCount;

        // go over every particle
        for (int i = 0; i < particleCount; i++)
        {
            //calculate distance and create force vector
            Vector3 directionToTarget = Vector3.Normalize(targetTransformedPosition - particles[i].position);
            Vector3 seekForce = directionToTarget * forceDeltaTime;

            particles[i].velocity += seekForce;
        }

        // set every particle back to the system
        particleSystem.SetParticles(particles, particleCount);
    }
}
