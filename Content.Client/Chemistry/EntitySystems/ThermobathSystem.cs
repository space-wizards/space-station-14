using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Temperature.Components;
using Robust.Shared.Containers;

namespace Content.Client.Chemistry.EntitySystems;

/// <inheritdoc/>
public sealed partial class ThermobathSystem : SharedThermobathSystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    [SubscribeLocalEvent]
    private void OnThermoregulatorState(Entity<ThermoregulatorComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (TryComp<ThermobathComponent>(ent, out var thermobath))
            UpdateUi((ent, thermobath));
    }

    [SubscribeLocalEvent]
    private void OnSolutionState(Entity<SolutionComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<ContainedSolutionComponent>(ent, out var contained) ||
            !_container.TryGetContainingContainer(contained.Container, out var container) ||
            container.ID != ThermobathComponent.BeakerSlotId ||
            !TryComp<ThermobathComponent>(container.Owner, out var thermobath))
            return;

        UpdateUi((container.Owner, thermobath));
    }

    protected override void UpdateUi(Entity<ThermobathComponent> ent)
    {
        if (_ui.TryGetOpenUi(ent.Owner, ThermobathUiKey.Key, out var bui))
            bui.Update();
    }
}
