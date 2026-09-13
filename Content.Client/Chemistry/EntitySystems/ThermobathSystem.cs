using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Temperature.Components;

namespace Content.Client.Chemistry.EntitySystems;

/// <inheritdoc/>
public sealed partial class ThermobathSystem : SharedThermobathSystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    [Dependency] private EntityQuery<ThermobathComponent> _thermobathQuery;

    [SubscribeLocalEvent]
    private void OnThermoregulatorState(Entity<ThermoregulatorComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_thermobathQuery.TryComp(ent, out var thermobath))
            UpdateUi((ent, thermobath));
    }

    protected override void UpdateUi(Entity<ThermobathComponent> ent)
    {
        if (_ui.TryGetOpenUi(ent.Owner, ThermobathUiKey.Key, out var bui))
            bui.Update();
    }
}
