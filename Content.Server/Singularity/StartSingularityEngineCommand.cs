using System.Linq;
using Content.Server.Administration;
using Content.Server.Construction.Components;
using Content.Server.ParticleAccelerator.Components;
using Content.Server.Singularity.Components;
using Content.Server.Singularity.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Singularity.Components;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Singularity
{
    [AdminCommand(AdminFlags.Admin)]
    public class StartSingularityEngineCommand : IConsoleCommand
    {
        public string Command => "startsingularityengine";
        public string Description => "Automatically sets up the PA, turns on the particle accelerator and the containment field emitters.";
        public string Help => $"{Command}";

        public async void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 0)
            {
                shell.WriteLine($"Invalid amount of arguments: {args.Length}.\n{Help}");
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            // Dear code reviewers: I don't think there's a good way to do this,
            //  and the command exists to give fast turnaround time on testing singulo,
            //  which, given all the singulo bugs, seems necessary,
            //  so it can't just be removed.
            // What this code does: Constructs all PA components by forcing node changes into the completed state.
            foreach (var comp in entityManager.ComponentManager.EntityQuery<ConstructionComponent>().ToArray())
            {
                // Check for valid graph
                var graph = comp.GraphPrototype;
                if (graph == null)
                    continue;
                // Construct PAs
                if (graph.ID.StartsWith("particleAccelerator") && graph.Nodes.ContainsKey("completed"))
                    await comp.ChangeNode("completed");
            }
            // Turn on emitters and collectors.
            foreach (var comp in entityManager.ComponentManager.EntityQuery<EmitterComponent>())
            {
                EntitySystem.Get<EmitterSystem>().SwitchOn(comp);
            }
            foreach (var comp in entityManager.ComponentManager.EntityQuery<RadiationCollectorComponent>())
            {
                comp.Collecting = true;
            }
            // Actually turn on the PA.
            foreach (var comp in entityManager.ComponentManager.EntityQuery<ParticleAcceleratorControlBoxComponent>())
            {
                comp.RescanParts();
                comp.SetStrength(ParticleAcceleratorPowerState.Level0);
                comp.SwitchOn();
            }
            shell.WriteLine("Done!");
        }
    }
}
