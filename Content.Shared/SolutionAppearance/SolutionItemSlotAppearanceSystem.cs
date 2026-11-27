using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared.SolutionAppearance;

/// <summary>
/// Visual system for devices with <see cref="SolutionItemSlotAppearanceComponent" /> and <see cref="SolutionContainerVisualsComponent" />
/// Allows the visuals of device to be set using Solution within inserted item. Solution needs to have <see cref="SolutionAppearanceComponent" />.
/// </summary>
public sealed partial class SolutionItemSlotAppearanceSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    [SubscribeLocalEvent]
    private void OnStartup(Entity<SolutionAppearanceComponent> ent, ref ComponentStartup args)
    {
        if (_container.TryGetContainingContainer(ent.Owner, out var container))
        {
            ent.Comp.CachedContainer = container;
            UpdateAppearance(ent);
        }
    }

    [SubscribeLocalEvent]
    private void OnEntGotInsertedIntoContainer(Entity<SolutionAppearanceComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        ent.Comp.CachedContainer = args.Container;
        UpdateAppearance(ent);
    }

    [SubscribeLocalEvent]
    private void OnEntGotRemovedFromContainer(Entity<SolutionAppearanceComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        ent.Comp.CachedContainer = null;

        if (!IsValidSolutionContainer(args.Container.Owner, args.Container.ID))
            return;

        _appearance.SetData(args.Container.Owner, SolutionContainerVisuals.FillFraction, 0f);
    }

    [SubscribeLocalEvent]
    private void OnSolutionContainerChanged(Entity<SolutionAppearanceComponent> ent, ref SolutionChangedEvent args)
    {
        UpdateAppearance(ent, args.Solution);
    }

    [SubscribeLocalEvent]
    private void OnIsValidSolutionContainer(Entity<SolutionItemSlotAppearanceComponent> ent, ref IsValidSolutionContainerEvent args)
    {
        if (args.IsValid)
            return;

        if (ent.Comp.ContainerID == args.ContainerId)
            args.IsValid = true;
    }

    private void UpdateAppearance(Entity<SolutionAppearanceComponent> ent, SolutionComponent? solutionComp = null)
    {
        var container = ent.Comp.CachedContainer;
        if (container == null)
            return;

        if (!IsValidSolutionContainer(container.Owner, container.ID))
            return;

        if (!_entityWhitelist.CheckBoth(container.Owner, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return;

        if (solutionComp == null && !Resolve(ent.Owner, ref solutionComp))
            return;

        _solutionContainer.UpdateAppearance(container.Owner, (ent.Owner, solutionComp));
    }

    private bool IsValidSolutionContainer(EntityUid owner, string containerId)
    {
        var ev = new IsValidSolutionContainerEvent(containerId);
        RaiseLocalEvent(owner, ref ev);

        return ev.IsValid;
    }
}
