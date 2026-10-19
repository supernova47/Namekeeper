using UnityEngine;
using Object = UnityEngine.Object;

namespace SuperNova.ValheimNamekeeper
{
    internal static class NamekeeperEffects
    {
        internal static void Play(Vector3 position)
        {
            var host = new GameObject("NamekeeperGoldSparkles");
            host.transform.position = position;
            var particles = host.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = false;
            main.duration = 1.15f;
            main.startLifetime = 1.05f;
            main.startSpeed = 1.7f;
            main.startSize = .09f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, .77f, .25f, 1f));
            main.maxParticles = 42;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = .4f;
            particles.Emit(35);
            particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Object.Destroy(host, 3f);
        }
    }
}
