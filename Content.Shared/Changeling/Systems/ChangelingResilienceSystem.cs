using System.Linq;
using Content.Shared.Body;
using Content.Shared.Changeling.Components;
using Content.Shared.Destructible;
using Content.Shared.Revolutionary;
using Robust.Shared.Containers;

namespace Content.Shared.Changeling.Systems;

public sealed partial class ChangelingResilienceSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingResilienceComponent, MapInitEvent>(OnMapInit, after: [typeof(InitialBodySystem)]);
    }

    private void OnMapInit(Entity<ChangelingResilienceComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.OrganRemovedComponents != null)
            UpdateOrgans(ent);
    }

    /// <summary>
    /// Prevent destruction via non-ashing sources, if appropriate.
    /// </summary>
    /// <param name="ent">Changeling entity.</param>
    /// <param name="args">The destruction event args. Canceled if the component is set to disable gibbing.</param>
    [SubscribeLocalEvent]
    private void OnDestruction(Entity<ChangelingResilienceComponent> ent, ref DestructionAttemptEvent args)
    {
        if (ent.Comp.PreventGibbing)
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnAttemptRevConvert(Entity<ChangelingResilienceComponent> ent, ref AttemptConvertRevolutionaryEvent args)
    {
        args.Cancelled |= ent.Comp.PreventConversion;
    }

    private void UpdateOrgans(Entity<ChangelingResilienceComponent> ent)
    {
        if (!TryComp<ContainerManagerComponent>(ent, out var containerComp))
            return;

        if (TerminatingOrDeleted(ent) || !Exists(ent))
            return;

        if (!_container.TryGetContainer(ent, BodyComponent.ContainerID, out var container, containerComp))
        {
            Log.Error($"Entity {ToPrettyString(ent)} with a {nameof(ChangelingResilienceComponent)} is missing a container ({BodyComponent.ContainerID}) when attempting to update organs.");
            return;
        }

        var organs = container.ContainedEntities.ToList();

        foreach (var organ in organs)
        {
            if (ent.Comp.OrganRemovedComponents != null)
                EntityManager.RemoveComponents(organ, ent.Comp.OrganRemovedComponents);
        }
    }
}
