using Content.Server.Mech.Components;
using Content.Shared.Interaction;
using Content.Shared.Tag;
using Content.Shared.Tools.Components;
using Robust.Server.Containers;
using Robust.Shared.Containers;

namespace Content.Server.Mech.Systems;

/// <summary>
/// Handles <see cref="MechAssemblyComponent"/> and the insertion
/// and removal of parts from the assembly.
/// </summary>
public sealed class MechAssemblySystem : EntitySystem
{
    [Dependency] private readonly ContainerSystem _container = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MechAssemblyComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<MechAssemblyComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInit(EntityUid uid, MechAssemblyComponent component, ComponentInit args)
    {
        component.PartsContainer = _container.EnsureContainer<Container>(uid, "mech-assembly-container");
    }

    private void OnInteractUsing(EntityUid uid, MechAssemblyComponent component, InteractUsingEvent args)
    {
        if (TryComp<ToolComponent>(args.Used, out var toolComp) && toolComp.Qualities.Contains(component.QualityNeeded))
        {
            foreach (var tag in component.RequiredParts.Keys)
            {
                component.RequiredParts[tag] = false;
            }
            _container.EmptyContainer(component.PartsContainer);
            return;
        }

        if (!TryComp<TagComponent>(args.Used, out var tagComp))
            return;

        foreach (var (tag, val) in component.RequiredParts)
        {
            if (!val && tagComp.Tags.Contains(tag))
            {
                component.RequiredParts[tag] = true;
                _container.Insert(args.Used, component.PartsContainer);
                break;
            }
        }

        //check to see if we have all the parts
        foreach (var val in component.RequiredParts.Values)
        {
            if (!val)
                return;
        }
        Spawn(component.FinishedPrototype, Transform(uid).Coordinates);
        EntityManager.DeleteEntity(uid);
    }
}
