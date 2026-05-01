using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared.SolutionAppearanceRelay;

public sealed class SolutionAppearanceRelaySystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolutionAppearanceRelayComponent, SolutionChangedEvent>(OnSolutionContainerChanged);
        SubscribeLocalEvent<SolutionAppearanceRelayComponent, EntGotInsertedIntoContainerMessage>(OnEntGotInsertedIntoContainer);
        SubscribeLocalEvent<SolutionAppearanceRelayComponent, EntGotRemovedFromContainerMessage>(OnEntGotRemovedFromContainer);
    }

    private void OnSolutionContainerChanged(Entity<SolutionAppearanceRelayComponent> ent, ref SolutionChangedEvent args)
    {
        UpdateAppearance(ent);
    }

    private void OnEntGotInsertedIntoContainer(Entity<SolutionAppearanceRelayComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        UpdateAppearance(ent);
    }

    private void OnEntGotRemovedFromContainer(Entity<SolutionAppearanceRelayComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (!_entityWhitelist.CheckBoth(args.Container.Owner, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return;

        _appearance.SetData(args.Container.Owner, SolutionContainerVisuals.FillFraction, 0);
        _appearance.SetData(args.Container.Owner, SolutionAppearanceRelayedVisuals.HasRelay, false);
    }

    private void UpdateAppearance(Entity<SolutionAppearanceRelayComponent> ent)
    {
        if (!_container.TryGetContainingContainer((ent, null, null), out var container))
            return;

        if (!_entityWhitelist.CheckBoth(container.Owner, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return;

        if (!TryComp<SolutionComponent>(ent.Owner, out var solutionComp))
            return;

        if (!_solutionContainer.TryGetSolution(ent.Owner, solutionComp.Id, out var solutionEntity, out _))
            return;

        _solutionContainer.UpdateAppearance(container.Owner, (solutionEntity.Value.Owner, solutionEntity.Value.Comp));
        _appearance.SetData(container.Owner, SolutionAppearanceRelayedVisuals.HasRelay, true);
    }
}
