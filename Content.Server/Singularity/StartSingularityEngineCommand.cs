using Content.Server.Administration;
using Content.Server.ParticleAccelerator.Components;
using Content.Server.ParticleAccelerator.EntitySystems;
using Content.Server.Singularity.Components;
using Content.Server.Singularity.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Singularity.Components;
using Robust.Shared.Console;

namespace Content.Server.Singularity
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class StartSingularityEngineCommand : IConsoleCommand
    {
        public string Command => "startsingularityengine";
        public string Description => "Automatically turns on the particle accelerator and containment field emitters.";
        public string Help => $"{Command}";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 0)
            {
                shell.WriteLine($"Invalid amount of arguments: {args.Length}.\n{Help}");
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var entitySystemManager = IoCManager.Resolve<IEntitySystemManager>();

            // Turn on emitters
            var emitterQuery = entityManager.EntityQueryEnumerator<EmitterComponent>();
            var emitterSystem = entitySystemManager.GetEntitySystem<EmitterSystem>();
            while (emitterQuery.MoveNext(out var uid, out var emitterComponent))
            {
                //FIXME: This turns on ALL emitters, including APEs. It should only turn on the containment field emitters.
                emitterSystem.SwitchOn(uid, emitterComponent);
            }

            // Turn on radiation collectors
            var radiationCollectorQuery = entityManager.EntityQueryEnumerator<RadiationCollectorComponent>();
            var radiationCollectorSystem = entitySystemManager.GetEntitySystem<RadiationCollectorSystem>();
            while (radiationCollectorQuery.MoveNext(out var uid, out var radiationCollectorComponent))
            {
                radiationCollectorSystem.SetCollectorEnabled(uid, enabled: true, user: null, radiationCollectorComponent);
            }

            // Setup PA
            var paSystem = entitySystemManager.GetEntitySystem<ParticleAcceleratorSystem>();
            var paQuery = entityManager.EntityQueryEnumerator<ParticleAcceleratorControlBoxComponent>();
            while (paQuery.MoveNext(out var paId, out var paControl))
            {
                paSystem.RescanParts(paId, controller: paControl);
                if (!paControl.Assembled)
                    continue;

                paSystem.SetStrength(paId, ParticleAcceleratorPowerState.Level0, comp: paControl);
                paSystem.SwitchOn(paId, comp: paControl);
            }

            shell.WriteLine("Done!");
        }
    }
}
