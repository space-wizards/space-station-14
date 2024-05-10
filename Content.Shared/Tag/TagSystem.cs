using System.Linq;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Tag;

public sealed class TagSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TagComponent, ComponentGetState>(OnTagGetState);
        SubscribeLocalEvent<TagComponent, ComponentHandleState>(OnTagHandleState);

#if DEBUG
        SubscribeLocalEvent<TagComponent, ComponentInit>(OnTagInit);
    }

    private void OnTagInit(EntityUid uid, TagComponent component, ComponentInit args)
    {
        foreach (var tag in component.Tags)
        {
            AssertValidTag(tag);
        }
#endif
    }


    private void OnTagHandleState(EntityUid uid, TagComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not TagComponentState state)
            return;

        component.Tags.Clear();

        foreach (var tag in state.Tags)
        {
            AssertValidTag(tag);
            component.Tags.Add(tag);
        }
    }

    private static void OnTagGetState(EntityUid uid, TagComponent component, ref ComponentGetState args)
    {
        var tags = new string[component.Tags.Count];
        var i = 0;

        foreach (var tag in component.Tags)
        {
            tags[i] = tag;
            i++;
        }

        args.State = new TagComponentState(tags);
    }

    private void AssertValidTag(string id)
    {
        DebugTools.Assert(_proto.HasIndex<TagPrototype>(id), $"Unknown tag: {id}");
    }

    /// <summary>
    ///     Tries to add a tag to an entity if the tag doesn't already exist.
    /// </summary>
    /// <param name="entity">The entity to add the tag to.</param>
    /// <param name="id">The tag to add.</param>
    /// <returns>
    ///     true if it was added, false otherwise even if it already existed.
    /// </returns>
    /// <exception cref="UnknownPrototypeException">
    ///     Thrown if no <see cref="TagPrototype"/> exists with the given id.
    /// </exception>
    public bool AddTag(EntityUid entity, string id)
    {
        return AddTag(entity, EnsureComp<TagComponent>(entity), id);
    }

    /// <summary>
    ///     Tries to add the given tags to an entity if the tags don't already exist.
    /// </summary>
    /// <param name="entity">The entity to add the tag to.</param>
    /// <param name="ids">The tags to add.</param>
    /// <returns>
    ///     true if any tags were added, false otherwise even if they all already existed.
    /// </returns>
    /// <exception cref="UnknownPrototypeException">
    ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
    /// </exception>
    public bool AddTags(EntityUid entity, params string[] ids)
    {
        return AddTags(entity, EnsureComp<TagComponent>(entity), ids);
    }

    /// <summary>
    ///     Tries to add the given tags to an entity if the tags don't already exist.
    /// </summary>
    /// <param name="entity">The entity to add the tag to.</param>
    /// <param name="ids">The tags to add.</param>
    /// <returns>
    ///     true if any tags were added, false otherwise even if they all already existed.
    /// </returns>
    /// <exception cref="UnknownPrototypeException">
    ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
    /// </exception>
    public bool AddTags(EntityUid entity, IEnumerable<string> ids)
    {
        return AddTags(entity, EnsureComp<TagComponent>(entity), ids);
    }

    /// <summary>
    ///     Tries to add a tag to an entity if it has a <see cref="TagComponent"/>
    ///     and the tag doesn't already exist.
    /// </summary>
    /// <param name="entity">The entity to add the tag to.</param>
    /// <param name="id">The tag to add.</param>
    /// <returns>
    ///     true if it was added, false otherwise even if it already existed.
    /// </returns>
    /// <exception cref="UnknownPrototypeException">
    ///     Thrown if no <see cref="TagPrototype"/> exists with the given id.
    /// </exception>
    public bool TryAddTag(EntityUid entity, string id)
    {
        return TryComp<TagComponent>(entity, out var component) &&
               AddTag(entity, component, id);
    }

    /// <summary>
    ///     Tries to add the given tags to an entity if it has a
    ///     <see cref="TagComponent"/> and the tags don't already exist.
    /// </summary>
    /// <param name="entity">The entity to add the tag to.</param>
    /// <param name="ids">The tags to add.</param>
    /// <returns>
    ///     true if any tags were added, false otherwise even if they all already existed.
    /// </returns>
    /// <exception cref="UnknownPrototypeException">
    ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
    /// </exception>
    public bool TryAddTags(EntityUid entity, params string[] ids)
    {
        return TryComp<TagComponent>(entity, out var component) &&
               AddTags(entity, component, ids);
    }

    /// <summary>
    ///     Tries to add the given tags to an entity if it has a
    ///     <see cref="TagComponent"/> and the tags don't already exist.
    /// </summary>
    /// <param name="entity">The entity to add the tag to.</param>
    /// <param name="ids">The tags to add.</param>
    /// <returns>
    ///     true if any tags were added, false otherwise even if they all already existed.
    /// </returns>
    /// <exception cref="UnknownPrototypeException">
    ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
    /// </exception>
    public bool TryAddTags(EntityUid entity, IEnumerable<string> ids)
    {
        return TryComp<TagComponent>(entity, out var component) &&
               AddTags(entity, component, ids);
    }

    /// <summary>
    ///     Checks if a tag has been added to an entity.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="id">The tag to check for.</param>
    /// <returns>true if it exists, false otherwise.</returns>
    /// <exception cref="UnknownPrototypeException">
    ///     Thrown if no <see cref="TagPrototype"/> exists with the given id.
    /// </exception>
    public bool HasTag(EntityUid entity, string id)
    {
        return TryComp<TagComponent>(entity, out var component) &&
               HasTag(component, id);
    }

    /// <summary>
    ///     Checks if a tag has been added to an entity.
    /// </summary>
    public bool HasTag(EntityUid entity, string id, EntityQuery<TagComponent> tagQuery)
    {
        return tagQuery.TryGetComponent(entity, out var component) &&
               HasTag(component, id);
    }

    /// <summary>
    ///     Checks if all of the given tags have been added to an entity.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="id">The tags to check for.</param>
    /// <returns>true if they all exist, false otherwise.</returns>
    /// <exception cref="UnknownPrototypeException">
    ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
    /// </exception>
    public bool HasAllTags(EntityUid entity, string id) => HasTag(entity, id);

    /// <summary>
    ///     Checks if all of the given tags have been added to an entity.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="ids">The tags to check for.</param>
    /// <returns>true if they all exist, false otherwise.</returns>
    /// <exception cref="UnknownPrototypeException">
    ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
    /// </exception>
    public bool HasAllTags(EntityUid entity, List<string> ids)
    {
        return TryComp<TagComponent>(entity, out var component) &&
               HasAllTags(component, ids);
    }

    /// <summary>
    ///     Checks if all of the given tags have been added to an entity.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="ids">The tags to check for.</param>
    /// <returns>true if they all exist, false otherwise.</returns>
    /// <exception cref="UnknownPrototypeException">
    ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
    /// </exception>
    public bool HasAllTags(EntityUid entity, IEnumerable<string> ids)
    {
        return TryComp<TagComponent>(entity, out var component) &&
               HasAllTags(component, ids);
    }

    /// <summary>
    ///     Checks if all of the given tags have been added to an entity.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="ids">The tags to check for.</param>
    /// <returns>true if any of them exist, false otherwise.</returns>
    /// <exception cref="UnknownPrototypeException">
    ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
    /// </exception>
    public bool HasAnyTag(EntityUid entity, params string[] ids)
    {
        return TryComp<TagComponent>(entity, out var component) &&
               HasAnyTag(component, ids);
    }

    /// <summary>
    ///     Checks if all of the given tags have been added to an entity.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="id">The tag to check for.</param>
    /// <returns>true if any of them exist, false otherwise.</returns>
    /// <exception cref="UnknownPrototypeException">
    ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
    /// </exception>
    public bool HasAnyTag(EntityUid entity, string id) => HasTag(entity, id);

    /// <summary>
    ///     Checks if all of the given tags have been added to an entity.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="ids">The tags to check for.</param>
    /// <returns>true if any of them exist, false otherwise.</returns>
    /// <exception cref="UnknownPrototypeException">
    ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
    /// </exception>
    public bool HasAnyTag(EntityUid entity, List<string> ids)
    {
        return TryComp<TagComponent>(entity, out var component) &&
               HasAnyTag(component, ids);
    }

    /// <summary>
    ///     Checks if all of the given tags have been added to an entity.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="ids">The tags to check for.</param>
    /// <returns>true if any of them exist, false otherwise.</returns>
    /// <exception cref="UnknownPrototypeException">
    ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
    /// </exception>
    public bool HasAnyTag(EntityUid entity, IEnumerable<string> ids)
    {
        return TryComp<TagComponent>(entity, out var component) &&
               HasAnyTag(component, ids);
    }

    /// <summary>
    ///     Tries to remove a tag from an entity if it exists.
    /// </summary>
    /// <param name="entity">The entity to remove the tag from.</param>
    /// <param name="id">The tag to remove.</param>
    /// <returns>
    ///     true if it was removed, false otherwise even if it didn't exist.
    /// </returns>
    /// <exception cref="UnknownPrototypeException">
    ///     Thrown if no <see cref="TagPrototype"/> exists with the given id.
    /// </exception>
    public bool RemoveTag(EntityUid entity, string id)
    {
        return TryComp<TagComponent>(entity, out var component) &&
               RemoveTag(entity, component, id);
    }

    /// <summary>
    ///     Tries to remove a tag from an entity if it exists.
    /// </summary>
    /// <param name="entity">The entity to remove the tag from.</param>
    /// <param name="ids">The tag to remove.</param>
    /// <returns>
    ///     true if it was removed, false otherwise even if it didn't exist.
    /// <exception cref="UnknownPrototypeException">
    ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
    /// </exception>
    /// </returns>
    public bool RemoveTags(EntityUid entity, params string[] ids)
    {
        return TryComp<TagComponent>(entity, out var component) &&
               RemoveTags(entity, component, ids);
    }

    /// <summary>
    ///     Tries to remove a tag from an entity if it exists.
    /// </summary>
    /// <param name="entity">The entity to remove the tag from.</param>
    /// <param name="ids">The tag to remove.</param>
    /// <returns>
    ///     true if it was removed, false otherwise even if it didn't exist.
    /// </returns>
    /// <exception cref="UnknownPrototypeException">
    ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
    /// </exception>
    public bool RemoveTags(EntityUid entity, IEnumerable<string> ids)
    {
        return TryComp<TagComponent>(entity, out var component) &&
               RemoveTags(entity, component, ids);
    }

    /// <summary>
    ///     Tries to add a tag if it doesn't already exist.
    /// </summary>
    /// <param name="id">The tag to add.</param>
    /// <returns>true if it was added, false if it already existed.</returns>
    /// <exception cref="UnknownPrototypeException">
    ///     Thrown if no <see cref="TagPrototype"/> exists with the given id.
    /// </exception>
    public bool AddTag(EntityUid uid, TagComponent component, string id)
    {
        AssertValidTag(id);
        var added = component.Tags.Add(id);

        if (added)
        {
            Dirty(uid, component);
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Tries to add the given tags if they don't already exist.
    /// </summary>
    /// <param name="ids">The tags to add.</param>
    /// <returns>true if any tags were added, false if they all already existed.</returns>
    /// <exception cref="UnknownPrototypeException">
    ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
    /// </exception>
    public bool AddTags(EntityUid uid, TagComponent component, params string[] ids)
    {
        return AddTags(uid, component, ids.AsEnumerable());
    }

    /// <summary>
    ///     Tries to add the given tags if they don't already exist.
    /// </summary>
    /// <param name="ids">The tags to add.</param>
    /// <returns>true if any tags were added, false if they all already existed.</returns>
    /// <exception cref="UnknownPrototypeException">
    ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
    /// </exception>
    public bool AddTags(EntityUid uid, TagComponent component, IEnumerable<string> ids)
    {
        var count = component.Tags.Count;

        foreach (var id in ids)
        {
            AssertValidTag(id);
            component.Tags.Add(id);
        }

        if (component.Tags.Count > count)
        {
            Dirty(uid, component);
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Checks if a tag has been added.
    /// </summary>
    /// <param name="id">The tag to check for.</param>
    /// <returns>true if it exists, false otherwise.</returns>
    /// <exception cref="UnknownPrototypeException">
    ///     Thrown if no <see cref="TagPrototype"/> exists with the given id.
    /// </exception>
    public bool HasTag(TagComponent component, string id)
    {
        AssertValidTag(id);
        return component.Tags.Contains(id);
    }

    /// <summary>
    ///     Checks if all of the given tags have been added.
    /// </summary>
    /// <param name="ids">The tags to check for.</param>
    /// <returns>true if they all exist, false otherwise.</returns>
    /// <exception cref="UnknownPrototypeException">
    ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
    /// </exception>
    public bool HasAllTags(TagComponent component, params string[] ids)
    {
        return HasAllTags(component, ids.AsEnumerable());
    }

    /// <summary>
    ///     Checks if all of the given tags have been added.
    /// </summary>
    /// <param name="id">The tag to check for.</param>
    /// <returns>true if they all exist, false otherwise.</returns>
    /// <exception cref="UnknownPrototypeException">
    ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
    /// </exception>
    public bool HasAllTags(TagComponent component, string id) => HasTag(component, id);

    /// <summary>
    ///     Checks if all of the given tags have been added.
    /// </summary>
    /// <param name="ids">The tags to check for.</param>
    /// <returns>true if they all exist, false otherwise.</returns>
    /// <exception cref="UnknownPrototypeException">
    ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
    /// </exception>
    public bool HasAllTags(TagComponent component, List<string> ids)
    {
        foreach (var id in ids)
        {
            AssertValidTag(id);

            if (!component.Tags.Contains(id))
                return false;
        }

        return true;
    }

    /// <summary>
    ///     Checks if all of the given tags have been added.
    /// </summary>
    /// <param name="ids">The tags to check for.</param>
    /// <returns>true if they all exist, false otherwise.</returns>
    /// <exception cref="UnknownPrototypeException">
    ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
    /// </exception>
    public bool HasAllTags(TagComponent component, IEnumerable<string> ids)
    {
        foreach (var id in ids)
        {
            AssertValidTag(id);

            if (!component.Tags.Contains(id))
                return false;

        }

        return true;
    }

    /// <summary>
    ///     Checks if any of the given tags have been added.
    /// </summary>
    /// <param name="ids">The tags to check for.</param>
    /// <returns>true if any of them exist, false otherwise.</returns>
    /// <exception cref="UnknownPrototypeException">
    ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
    /// </exception>
    public bool HasAnyTag(TagComponent component, params string[] ids)
    {
        return HasAnyTag(component, ids.AsEnumerable());
    }


    /// <summary>
    ///     Checks if any of the given tags have been added.
    /// </summary>
    /// <param name="id">The tag to check for.</param>
    /// <returns>true if any of them exist, false otherwise.</returns>
    /// <exception cref="UnknownPrototypeException">
    ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
    /// </exception>
    public bool HasAnyTag(TagComponent component, string id) => HasTag(component, id);

    /// <summary>
    ///     Checks if any of the given tags have been added.
    /// </summary>
    /// <param name="ids">The tags to check for.</param>
    /// <returns>true if any of them exist, false otherwise.</returns>
    /// <exception cref="UnknownPrototypeException">
    ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
    /// </exception>
    public bool HasAnyTag(TagComponent component, List<string> ids)
    {
        foreach (var id in ids)
        {
            AssertValidTag(id);

            if (component.Tags.Contains(id))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Checks if any of the given tags have been added.
    /// </summary>
    /// <param name="ids">The tags to check for.</param>
    /// <returns>true if any of them exist, false otherwise.</returns>
    /// <exception cref="UnknownPrototypeException">
    ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
    /// </exception>
    public bool HasAnyTag(TagComponent component, IEnumerable<string> ids)
    {
        foreach (var id in ids)
        {
            AssertValidTag(id);

            if (component.Tags.Contains(id))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Tries to remove a tag if it exists.
    /// </summary>
    /// <returns>
    ///     true if it was removed, false otherwise even if it didn't exist.
    /// </returns>
    /// <exception cref="UnknownPrototypeException">
    ///     Thrown if no <see cref="TagPrototype"/> exists with the given id.
    /// </exception>
    public bool RemoveTag(EntityUid uid, TagComponent component, string id)
    {
        AssertValidTag(id);

        if (component.Tags.Remove(id))
        {
            Dirty(uid, component);
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Tries to remove all of the given tags if they exist.
    /// </summary>
    /// <param name="ids">The tags to remove.</param>
    /// <returns>
    ///     true if it was removed, false otherwise even if they didn't exist.
    /// </returns>
    /// <exception cref="UnknownPrototypeException">
    ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
    /// </exception>
    public bool RemoveTags(EntityUid uid, TagComponent component, params string[] ids)
    {
        return RemoveTags(uid, component, ids.AsEnumerable());
    }

    /// <summary>
    ///     Tries to remove all of the given tags if they exist.
    /// </summary>
    /// <param name="ids">The tags to remove.</param>
    /// <returns>true if any tag was removed, false otherwise.</returns>
    /// <exception cref="UnknownPrototypeException">
    ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
    /// </exception>
    public bool RemoveTags(EntityUid uid, TagComponent component, IEnumerable<string> ids)
    {
        var count = component.Tags.Count;

        foreach (var id in ids)
        {
            AssertValidTag(id);
            component.Tags.Remove(id);
        }

        if (component.Tags.Count < count)
        {
            Dirty(uid, component);
            return true;
        }

        return false;
    }
}
