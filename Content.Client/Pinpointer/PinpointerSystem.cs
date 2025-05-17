using Content.Shared.Pinpointer;
using Content.Shared.Storage.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Containers;

namespace Content.Client.Pinpointer;

public sealed class PinpointerSystem : SharedPinpointerSystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // we want to show pinpointers arrow direction relative
        // to players eye rotation (like it was in SS13)

        // because eye can change it rotation anytime
        // we need to update this arrow in a update loop
        var query = EntityQueryEnumerator<PinpointerComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var pinpointer, out var sprite))
        {
            if (!pinpointer.HasTarget)
                continue;
            var eye = _eyeManager.CurrentEye;
            var angle = pinpointer.ArrowAngle + eye.Rotation;

            // Pinpointer may be flipped in a backpack etc, adjust for it
            if (_container.TryGetContainingContainer((uid, null, null), out var container))
            {
                _storage.TryGetStorageLocation(uid, out var _, out var _, out var itemStorageLocation);
                if (itemStorageLocation.Direction == Direction.East || itemStorageLocation.Direction == Direction.West)
                {
                    angle += Angle.FromDegrees(180);
                }
            }

            switch (pinpointer.DistanceToTarget)
            {
                case Distance.Close:
                case Distance.Medium:
                case Distance.Far:
                    sprite.LayerSetRotation(PinpointerLayers.Screen, angle);
                    break;
                default:
                    sprite.LayerSetRotation(PinpointerLayers.Screen, Angle.Zero);
                    break;
            }
        }
    }
}
