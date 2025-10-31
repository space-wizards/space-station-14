using Content.Client.Atmos.Components;
using Content.Client.Atmos.UI;
using Content.Client.Items;
using Content.Shared.Atmos.Components;

namespace Content.Client.Atmos.EntitySystems;

/// <summary>
/// Wires up item status logic for <see cref="TankPressureItemStatusComponent"/>.
/// </summary>
/// <seealso cref="TankPressureStatusControl"/>
public sealed class TankPressureItemStatusSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        Subs.ItemStatus<TankPressureItemStatusComponent>(
            entity => new TankPressureStatusControl(entity, EntityManager));
        SubscribeNetworkEvent<GasTankPressureChangedEvent>(OnTankPressureChanged);
    }

    private void OnTankPressureChanged(GasTankPressureChangedEvent ev)
    {
        var uid = GetEntity(ev.Tank);
        if (TryComp(uid, out GasTankComponent? tank))
            tank.InternalPressure = ev.Pressure;
    }
}
