using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Teleportation.Components;

namespace Content.Shared.Teleportation.Systems;

/// <summary>
///     Handles symmetrically linking two entities together, and removing links properly.
///     This does not do anything on its own (outside of deleting entities that have 0 links, if that option is true)
///     Systems can do whatever they please with the linked entities, such as <see cref="SharedPortalSystem"/>.
/// </summary>
public sealed class LinkedEntitySystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LinkedEntityComponent, ComponentShutdown>(OnLinkShutdown);
    }

    private void OnLinkShutdown(EntityUid uid, LinkedEntityComponent component, ComponentShutdown args)
    {
        // Remove any links to this entity when deleted.
        foreach (var ent in component.LinkedEntities.ToArray())
        {
            if (!Deleted(ent) && LifeStage(ent) < EntityLifeStage.Terminating && TryComp<LinkedEntityComponent>(ent, out var link))
            {
                TryUnlink(uid, ent, component, link);
            }
        }
    }

    #region Public API

    /// <summary>
    ///     Links two entities together. Does not require the existence of <see cref="LinkedEntityComponent"/> on either
    ///     already. Linking is symmetrical, so order doesn't matter.
    /// </summary>
    /// <param name="first">The first entity to link</param>
    /// <param name="second">The second entity to link</param>
    /// <param name="deleteOnEmptyLinks">Whether both entities should now delete once their links are removed</param>
    /// <returns>Whether linking was successful (e.g. they weren't already linked)</returns>
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

    /// <summary>
    ///     Unlinks two entities. Deletes either entity if <see cref="LinkedEntityComponent.DeleteOnEmptyLinks"/>
    ///     was true and its links are now empty. Symmetrical, so order doesn't matter.
    /// </summary>
    /// <param name="first">The first entity to unlink</param>
    /// <param name="second">The second entity to unlink</param>
    /// <param name="firstLink">Resolve comp</param>
    /// <param name="secondLink">Resolve comp</param>
    /// <returns>Whether unlinking was successful (e.g. they both were actually linked to one another)</returns>
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

    /// <summary>
    /// Get the first entity this entity is linked to.
    /// If multiple are linked only the first one is picked.
    /// </summary>
    public bool GetLink(EntityUid uid, [NotNullWhen(true)] out EntityUid? dest, LinkedEntityComponent? comp = null)
    {
        dest = null;
        if (!Resolve(uid, ref comp, false))
            return false;

        var first = comp.LinkedEntities.FirstOrDefault();
        if (first != default)
        {
            dest = first;
            return true;
        }

        return false;
    }

    #endregion
}
