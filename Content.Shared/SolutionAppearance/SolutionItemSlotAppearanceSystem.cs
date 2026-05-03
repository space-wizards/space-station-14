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
public sealed class SolutionItemSlotAppearanceSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolutionAppearanceComponent, SolutionChangedEvent>(OnSolutionContainerChanged);
        SubscribeLocalEvent<SolutionAppearanceComponent, EntGotInsertedIntoContainerMessage>(OnEntGotInsertedIntoContainer);
        SubscribeLocalEvent<SolutionAppearanceComponent, EntGotRemovedFromContainerMessage>(OnEntGotRemovedFromContainer);
    }

    private void OnSolutionContainerChanged(Entity<SolutionAppearanceComponent> ent, ref SolutionChangedEvent args)
    {
        UpdateAppearance(ent);
    }

    private void OnEntGotInsertedIntoContainer(Entity<SolutionAppearanceComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        UpdateAppearance(ent);
    }

    private void OnEntGotRemovedFromContainer(Entity<SolutionAppearanceComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (!_entityWhitelist.CheckBoth(args.Container.Owner, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return;

        if (!IsValidSolutionContainer(args.Container.Owner, args.Container.ID))
            return;

        _appearance.SetData(args.Container.Owner, SolutionContainerVisuals.FillFraction, 0);
        _appearance.SetData(args.Container.Owner, SolutionAppearanceRelayedVisuals.HasRelay, false);
    }

    private void UpdateAppearance(Entity<SolutionAppearanceComponent> ent)
    {
        if (!_container.TryGetContainingContainer((ent, null, null), out var container))
            return;

        if (!IsValidSolutionContainer(container.Owner, container.ID))
            return;

        if (!_entityWhitelist.CheckBoth(container.Owner, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return;

        if (!TryComp<SolutionComponent>(ent.Owner, out var solutionComp))
            return;

        if (!_solutionContainer.TryGetSolution(ent.Owner, solutionComp.Id, out var solutionEntity, out _))
            return;

        _solutionContainer.UpdateAppearance(container.Owner, (solutionEntity.Value.Owner, solutionEntity.Value.Comp));
        _appearance.SetData(container.Owner, SolutionAppearanceRelayedVisuals.HasRelay, true);

        return;
    }

    private bool IsValidSolutionContainer(EntityUid owner, string containerId)
    {
        if (!TryComp<SolutionItemSlotAppearanceComponent>(owner, out var appearance))
            return false;

        return appearance.ContainerID == containerId;
    }
}
