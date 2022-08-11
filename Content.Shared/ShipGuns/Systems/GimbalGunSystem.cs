using Content.Shared.ShipGuns.Components;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.ShipGuns.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class GimbalGunSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeAllEvent<GunnerCursorPositionEvent>(OnGunnerCursorPositionEvent);
    }

    private void OnGunnerCursorPositionEvent(GunnerCursorPositionEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity == null)
            return;

        if (!EntityManager.TryGetComponent<GunnerComponent>(args.SenderSession.AttachedEntity.Value,
                out var component))
            return;

        var console = component.Console;
        if (console == null)
            return;

        if (console.SubscribedGunner != null &&
            console.SubscribedGunner.Owner != args.SenderSession.AttachedEntity.Value)
            return;

        if (console.Target == null)
            // TODO: Make a new target entity if one doesn't exist
            return;

        var targetCoords = EntityCoordinates.FromMap(console.Target.Owner, Transform(console.Target.Owner).MapPosition);
    }

    // ReSharper disable once SuggestBaseTypeForParameter
    private void RotateGimbalGunTowardsPosition(GimbalGunWeaponComponent weapon, EntityCoordinates targetCoords)
    {
        var transform = Transform(weapon.Owner);
        var gimbalCoords = EntityCoordinates.FromMap(weapon.Owner, transform.MapPosition);

        // TODO: Make turrets turn properly instead of snapping
        var vectorRotation = gimbalCoords.Position - targetCoords.Position;
        var rotation = vectorRotation.ToWorldAngle();
        transform.WorldRotation = rotation;
    }

    [NetSerializable, Serializable]
    public sealed class GunnerCursorPositionEvent : EntityEventArgs
    {
        public EntityCoordinates Coordinates;
    }
}
