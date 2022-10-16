using Content.Server.Administration;
using Content.Server.ParticleAccelerator.Components;
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
            foreach (var comp in entityManager.EntityQuery<EmitterComponent>())
            {
                entitySystemManager.GetEntitySystem<EmitterSystem>().SwitchOn(comp);
            }
            foreach (var comp in entityManager.EntityQuery<RadiationCollectorComponent>())
            {
                entitySystemManager.GetEntitySystem<RadiationCollectorSystem>().SetCollectorEnabled(comp.Owner, true, null, comp);
            }
            foreach (var comp in entityManager.EntityQuery<ParticleAcceleratorControlBoxComponent>())
            {
                comp.RescanParts();
                comp.SetStrength(ParticleAcceleratorPowerState.Level0);
                comp.SwitchOn();
            }
            shell.WriteLine("Done!");
        }
    }
}
