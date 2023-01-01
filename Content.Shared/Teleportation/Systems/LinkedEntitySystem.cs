using System.Linq;
using Content.Shared.Teleportation.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Teleportation.Systems;

/// <summary>
///     Handles symmetrically linking two entities together, and removing links properly.
/// </summary>
public sealed class LinkedEntitySystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LinkedEntityComponent, ComponentShutdown>(OnLinkShutdown);

        SubscribeLocalEvent<LinkedEntityComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<LinkedEntityComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnGetState(EntityUid uid, LinkedEntityComponent component, ref ComponentGetState args)
    {
        args.State = new LinkedEntityComponentState(component.LinkedEntities);
    }

    private void OnHandleState(EntityUid uid, LinkedEntityComponent component, ref ComponentHandleState args)
    {
        if (args.Current is LinkedEntityComponentState state)
            component.LinkedEntities = state.LinkedEntities;
    }

    private void OnLinkShutdown(EntityUid uid, LinkedEntityComponent component, ComponentShutdown args)
    {
        // Remove any links to this entity when deleted.
        foreach (var ent in component.LinkedEntities.ToArray())
        {
            if (LifeStage(ent) < EntityLifeStage.Terminating && TryComp<LinkedEntityComponent>(ent, out var link))
            {
                TryUnlink(uid, ent, component, link);
            }
        }
    }

    #region Public API

    public bool TryLink(EntityUid first, EntityUid second, bool deleteOnEmptyLinks=false)
    {
        var firstLink = EnsureComp<LinkedEntityComponent>(first);
        var secondLink = EnsureComp<LinkedEntityComponent>(second);

        firstLink.DeleteOnEmptyLinks = deleteOnEmptyLinks;
        secondLink.DeleteOnEmptyLinks = deleteOnEmptyLinks;

        _appearance.SetData(first, LinkedEntityVisuals.HasAnyLinks, true);
        _appearance.SetData(second, LinkedEntityVisuals.HasAnyLinks, true);

        Dirty(firstLink);
        Dirty(secondLink);

        return firstLink.LinkedEntities.Add(second)
            && secondLink.LinkedEntities.Add(first);
    }

    public bool TryUnlink(EntityUid first, EntityUid second,
        LinkedEntityComponent? firstLink=null, LinkedEntityComponent? secondLink=null)
    {
        if (!Resolve(first, ref firstLink))
            return false;

        if (!Resolve(second, ref secondLink))
            return false;

        var success = firstLink.LinkedEntities.Remove(second)
                      && secondLink.LinkedEntities.Remove(first);

        _appearance.SetData(first, LinkedEntityVisuals.HasAnyLinks, firstLink.LinkedEntities.Any());
        _appearance.SetData(second, LinkedEntityVisuals.HasAnyLinks, secondLink.LinkedEntities.Any());

        Dirty(firstLink);
        Dirty(secondLink);

        if (firstLink.LinkedEntities.Count == 0 && firstLink.DeleteOnEmptyLinks)
            QueueDel(first);

        if (secondLink.LinkedEntities.Count == 0 && secondLink.DeleteOnEmptyLinks)
            QueueDel(second);

        return success;
    }

    #endregion
}
