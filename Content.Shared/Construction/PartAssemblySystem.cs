using Content.Shared.Construction.Components;
using Content.Shared.Interaction;
using Content.Shared.Tag;
using Robust.Shared.Containers;

namespace Content.Shared.Construction;

/// <summary>
/// This handles <see cref="PartAssemblyComponent"/>
/// </summary>
public sealed class PartAssemblySystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PartAssemblyComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PartAssemblyComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<PartAssemblyComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
    }

    private void OnInit(EntityUid uid, PartAssemblyComponent component, ComponentInit args)
    {
        component.PartsContainer = _container.EnsureContainer<Container>(uid, component.ContainerId);
    }

    private void OnInteractUsing(EntityUid uid, PartAssemblyComponent component, InteractUsingEvent args)
    {
        if (!TryInsertPart(args.Used, uid, component))
            return;
        args.Handled = true;
    }

    private void OnEntRemoved(EntityUid uid, PartAssemblyComponent component, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != component.ContainerId)
            return;
        if (component.PartsContainer.ContainedEntities.Count != 0)
            return;
        component.CurrentAssembly = null;
    }

    /// <summary>
    /// Attempts to insert a part into the current assembly, starting one if there is none.
    /// </summary>
    public bool TryInsertPart(EntityUid part, EntityUid uid, PartAssemblyComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        string? assemblyId = null;
        assemblyId ??= component.CurrentAssembly;

        if (assemblyId == null)
        {
            foreach (var (id, tags) in component.Parts)
            {
                foreach (var tag in tags)
                {
                    if (!_tag.HasTag(part, tag))
                        continue;
                    assemblyId = id;
                    break;
                }

                if (assemblyId != null)
                    break;
            }
        }

        if (assemblyId == null)
            return false;

        if (!IsPartValid(uid, part, assemblyId, component))
            return false;

        component.CurrentAssembly = assemblyId;
        _container.Insert(part, component.PartsContainer);
        var ev = new PartAssemblyPartInsertedEvent();
        RaiseLocalEvent(uid, ev);
        return true;
    }

    /// <summary>
    /// Checks if the given entity is a valid item for the assembly.
    /// </summary>
    public bool IsPartValid(EntityUid uid, EntityUid part, string assemblyId, PartAssemblyComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return true;

        if (!component.Parts.TryGetValue(assemblyId, out var tags))
            return false;

        var openTags = new List<string>(tags);
        var contained = new List<EntityUid>(component.PartsContainer.ContainedEntities);
        foreach (var tag in tags)
        {
            foreach (var ent in component.PartsContainer.ContainedEntities)
            {
                if (!contained.Contains(ent) || !_tag.HasTag(ent, tag))
                    continue;
                openTags.Remove(tag);
                contained.Remove(ent);
                break;
            }
        }

        foreach (var tag in openTags)
        {
            if (_tag.HasTag(part, tag))
                return true;
        }

        return false;
    }

    public bool IsAssemblyFinished(EntityUid uid, string assemblyId, PartAssemblyComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return true;

        if (!component.Parts.TryGetValue(assemblyId, out var parts))
            return false;

        var contained = new List<EntityUid>(component.PartsContainer.ContainedEntities);
        foreach (var tag in parts)
        {
            var valid = false;
            foreach (var ent in new List<EntityUid>(contained))
            {
                if (!_tag.HasTag(ent, tag))
                    continue;
                valid = true;
                contained.Remove(ent);
                break;
            }

            if (!valid)
                return false;
        }

        return true;
    }
}
