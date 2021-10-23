using Content.Server.Devices.Components;
using Content.Server.Explosion;
using Content.Server.Fluids.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Devices;
using Content.Shared.Interaction;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;

namespace Content.Server.Devices.Systems
{
    public class BombPayloadSystem : EntitySystem
    {
        [Dependency]
        private SolutionContainerSystem _solutionContainerSystem = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<BombPayloadComponent, TriggerEvent>(OnPayloadTriggered);
        }

        //at the moment, this is only triggered on chemical bomb payloads.
        //that's cause there's not really any way to 'craft' explosives at the moment, so bomb payloads
        //do not have dynamic detonation effects.
        //TODO: Add triggering explosive payloads when they're ready to be implemented.

        //This currently ONLY handles chemical payloads. It will need touching up when explosive crafting
        //becomes a thing.
        private void OnPayloadTriggered(EntityUid uid, BombPayloadComponent component, TriggerEvent args)
        {
            var owner = EntityManager.GetEntity(uid);

            if (!owner.TryGetComponent(out ContainerManagerComponent? containerManager))
            {
                Logger.Warning($"Bomb Payload entity {owner} did not have a container manager! Aborting trigger!");
                return;
            }

            //make sure we have both chemical containers.
            if (containerManager.TryGetContainer(
                BombPayloadComponent.ChemBombPayloadChemicalContainer1, out var chemicalContainer1)
                && containerManager.TryGetContainer(
                BombPayloadComponent.ChemBombPayloadChemicalContainer2, out var chemicalContainer2))
            {
                if (chemicalContainer1.ContainedEntities.Count == 0 || chemicalContainer2.ContainedEntities.Count == 0)
                    return;

                var sol1Entity = chemicalContainer1.ContainedEntities[0];
                var sol2Entity = chemicalContainer2.ContainedEntities[0];

                //make sure both entities have one of these, as these will contain our solution nane.
                if (!sol1Entity.TryGetComponent<FitsInDispenserComponent>(out var sol1Comp)
                    || !sol2Entity.TryGetComponent<FitsInDispenserComponent>(out var sol2Comp))
                    return;

                if (!_solutionContainerSystem.TryGetSolution(sol1Entity.Uid, sol1Comp.Solution, out var solution1)
                    || !_solutionContainerSystem.TryGetSolution(sol2Entity.Uid, sol2Comp.Solution, out var solution2))
                    return;

                solution2.AddSolution(solution1);
                solution2.SpillAt(owner, "PuddleSplatter", false);
            }
        }
    }
}
