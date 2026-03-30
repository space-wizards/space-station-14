using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Client.Chemistry.Containers.EntitySystems;

public sealed partial class SolutionContainerSystem : SharedSolutionContainerSystem
{
    /// <summary>
    /// This updates the solution cache on client to prevent prediction desyncs.
    /// </summary>
    protected override void OnHandleState(Entity<SolutionComponent> entity, ref AfterAutoHandleStateEvent args)
    {
        base.OnHandleState(entity, ref args);

        if (!TryComp<ContainedSolutionComponent>(entity, out var contained) || !SolutionManagerQuery.TryComp(contained.Container, out var manager))
            return;

        manager.Solutions[entity.Comp.Id] = entity;
    }
}
