using Content.Shared.Maps;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Physics;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Shared.Trigger.Systems;

public sealed class ScramOnTriggerSystem : XOnTriggerSystem<ScramOnTriggerComponent>
{
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly TurfSystem _turfSystem = default!;

    protected override void OnTrigger(Entity<ScramOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        // We need stop the user from being pulled so they don't just get "attached" with whoever is pulling them.
        // This can for example happen when the user is cuffed and being pulled.
        if (TryComp<PullableComponent>(target, out var pull) && _pulling.IsPulled(target, pull))
            _pulling.TryStopPull(ent, pull);

        // Check if the user is pulling anything, and drop it if so.
        if (TryComp<PullerComponent>(target, out var puller) && TryComp<PullableComponent>(puller.Pulling, out var pullable))
            _pulling.TryStopPull(puller.Pulling.Value, pullable);

        _audio.PlayPredicted(ent.Comp.TeleportSound, ent, args.User);

        // Can't predict picking random grids and the target location might be out of PVS range.
        if (_net.IsClient)
            return;

        var targetCoords = SelectRandomTileInRange(target, ent.Comp.TeleportRadius);

        if (targetCoords != null)
        {
            _transform.SetCoordinates(target, targetCoords.Value);
            args.Handled = true;
        }
    }
    /// <summary>
    /// Method to find a random empty tile within a certain radius. Will not select off-grid tiles. Returns
    /// null if no tile is found within a certain number of tries.
    /// </summary>
    /// <remarks> Trends towards the outer radius. Compensates for small grids. </remarks>
    private EntityCoordinates? SelectRandomTileInRange(EntityUid uid, float radius, int tries = 40, PhysicsComponent? physicsComponent = null)
    {
        var userCoords = Transform(uid).Coordinates;
        EntityCoordinates? targetCoords = null;

        if (!Resolve(uid, ref physicsComponent))
            return targetCoords;


        for (var i = 0; i < tries; i++)
        {
            // distance = r * sq(x) * i
            // r = the radius of the search area.
            // sq(x) = the square root of [0 - 1]. Gives a number trending to the
            // upper range of [0, 1] so that you tend to teleport further.
            // i = A percentage based on the current try count, which results in each
            // subsequent try landing closer and closer towards the entity.
            // Beneficial for smaller maps, especially when the radius is large.
            var distance = radius * MathF.Sqrt(_random.NextFloat()) * (1 - (float)i / tries);

            // We then offset the user coords from a random angle * distance
            var tempTargetCoords = userCoords.Offset(_random.NextAngle().ToVec() * distance);

            if (!_turfSystem.TryGetTileRef(tempTargetCoords, out var tileRef)
                || _turfSystem.IsSpace(tileRef.Value)
                || _turfSystem.IsTileBlocked(tileRef.Value, (CollisionGroup)physicsComponent.CollisionMask))
                continue;

            targetCoords = tempTargetCoords;
            break;
        }

        return targetCoords;
    }
}
