using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Doors.Systems;

/// <summary>
/// Handles client-side door behaviour for alarmed doors.
/// </summary>
public sealed partial class DoorSystem
{
    [Dependency] private readonly PointLightSystem _pointLight = default!;

    private void InitializeClientDoorAlarm()
    {
        SubscribeLocalEvent<FirelockComponent, MapInitEvent>(SetAppearance);
        SubscribeLocalEvent<FirelockComponent, ComponentStartup>(SetAppearance);
        SubscribeLocalEvent<FirelockComponent, DoorAlarmChangedEvent>(SetAppearance);
    }

    private void SetAppearance<T>(Entity<FirelockComponent> doorAlarm, ref T _) where T : EntityEventArgs
    {
        UpdateAppearance(doorAlarm);
    }

    protected override void OnDoorStateChanged(Entity<FirelockComponent> doorAlarm, ref DoorStateChangedEvent args)
    {
        base.OnDoorStateChanged(doorAlarm, ref args);

        UpdateAppearance(doorAlarm);
    }

    private void UpdateAppearance(Entity<FirelockComponent> doorAlarm)
    {
        if (TryComp<SpriteComponent>(doorAlarm, out var sprite))
            sprite.LayerSetVisible(DoorVisualLayers.BaseUnlit, doorAlarm.Comp.IsTriggered);

        _appearance.SetData(doorAlarm, DoorVisuals.ClosedLights, doorAlarm.Comp.IsTriggered);
        _pointLight.SetEnabled(doorAlarm, doorAlarm.Comp.IsTriggered);
    }
}
