using System.Linq;
using Content.Shared.Body;
using Content.Shared.Changeling.Components;
using Content.Shared.Gibbing;
using Content.Shared.Metabolism;
using Content.Shared.Revolutionary;
using Content.Shared.Species.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Changeling.Systems;

public sealed partial class ChangelingResilienceSystem : EntitySystem
{
    [Dependency] private MetabolizerSystem _metabolizer = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingResilienceComponent, MapInitEvent>(OnMapInit, after: [typeof(InitialBodySystem)]);
    }

    private void OnMapInit(Entity<ChangelingResilienceComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.AppendedMetabolizer != null || ent.Comp.OrganRemovedComponents != null)
            UpdateOrgans(ent);
    }

    [SubscribeLocalEvent]
    private void OnAttemptRevConvert(Entity<ChangelingResilienceComponent> ent, ref AttemptConvertRevolutionaryEvent args)
    {
        args.Cancelled |= ent.Comp.PreventConversion;
    }

    [SubscribeLocalEvent]
    private void OnGibAttempt(Entity<ChangelingResilienceComponent> ent, ref AttemptGibEvent args)
    {
        args.Cancelled |= ent.Comp.PreventGibbing;
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
            if (TryComp<MetabolizerComponent>(organ, out var metabolizer) && ent.Comp.AppendedMetabolizer != null)
                _metabolizer.TryAddMetabolizerType((organ, metabolizer), ent.Comp.AppendedMetabolizer.Value);

            if (ent.Comp.OrganRemovedComponents != null)
                EntityManager.RemoveComponents(organ, ent.Comp.OrganRemovedComponents);
        }
    }
}
