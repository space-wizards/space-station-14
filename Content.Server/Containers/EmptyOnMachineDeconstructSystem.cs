using Content.Shared.Verbs;
using Content.Server.Construction.Components;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Containers
{
    /// <summary>
    /// Implements functionality of EmptyOnMachineDeconstructComponent.
    /// </summary>
    [UsedImplicitly]
    public class EmptyOnMachineDeconstructSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EmptyOnMachineDeconstructComponent, MachineDeconstructedEvent>(OnDeconstruct);
        }

        private void OnDeconstruct(EntityUid uid, EmptyOnMachineDeconstructComponent component, MachineDeconstructedEvent ev)
        {
            if (!EntityManager.TryGetComponent<IContainerManager>(uid, out var mComp))
                return;
            var baseCoords = component.Owner.Transform.Coordinates;
            foreach (var v in component.Containers)
            {
                if (mComp.TryGetContainer(v, out var container))
                {
                    container.EmptyContainer(true, baseCoords);
                }
            }
        }
    }
}
