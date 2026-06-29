using System.Linq;
using Content.Shared.Body;
using Content.Shared.Changeling.Components;
using Content.Shared.Gibbing;
using Content.Shared.Revolutionary;
using Content.Shared.Species.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Changeling.Systems;

public abstract partial class SharedChangelingResilienceSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingResilienceComponent, MapInitEvent>(OnMapInit, after: [typeof(InitialBodySystem)]);

        SubscribeLocalEvent<ChangelingResilienceComponent, AttemptConvertRevolutionaryEvent>(OnAttemptRevConvert);
        SubscribeLocalEvent<ChangelingResilienceComponent, AttemptGibEvent>(OnGibAttempt);
    }

    private void OnMapInit(Entity<ChangelingResilienceComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.ReplacementOrgans.Count > 0 || ent.Comp.PreventOrganNymphs)
            ReplaceOrgans(ent);

        if (ent.Comp.PreventGibbing)
            PreventGibbing(ent);

    }

    private void OnAttemptRevConvert(Entity<ChangelingResilienceComponent> ent, ref AttemptConvertRevolutionaryEvent args)
    {
        args.Cancelled |= ent.Comp.PreventConversion;
    }

    private void OnGibAttempt(Entity<ChangelingResilienceComponent> ent, ref AttemptGibEvent args)
    {
        args.Cancelled |= ent.Comp.PreventGibbing;
    }

    protected virtual void PreventGibbing(Entity<ChangelingResilienceComponent> ent)
    {

    }

    private void ReplaceOrgans(Entity<ChangelingResilienceComponent> ent)
    {
        if (!TryComp<ContainerManagerComponent>(ent, out var containerComp))
            return;

        if (TerminatingOrDeleted(ent) || !Exists(ent))
            return;

        if (!_container.TryGetContainer(ent, BodyComponent.ContainerID, out var container, containerComp))
        {
            Log.Error($"Entity {ToPrettyString(ent)} with a {nameof(ChangelingResilienceComponent)} is missing a container ({BodyComponent.ContainerID}) when attempting to replace organs.");
            return;
        }

        var organs = container.ContainedEntities.ToList(); // Copy it as to not delete organs we iterate over.

        foreach (var organ in organs)
        {
            foreach (var replacement in ent.Comp.ReplacementOrgans)
            {
                if (TryComp<OrganComponent>(organ, out var organComp) && organComp.Category == replacement.Key)
                {
                    if (TrySpawnInContainer(replacement.Value, ent, BodyComponent.ContainerID, out _))
                    {
                        QueueDel(organ);
                        break; // Only replace the first instance of the organ.
                    }
                }
            }

            if (ent.Comp.PreventOrganNymphs)
                RemCompDeferred<NymphComponent>(organ);
        }
    }
}
