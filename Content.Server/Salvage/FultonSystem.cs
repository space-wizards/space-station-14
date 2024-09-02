using System.Numerics;
using Content.Shared.Salvage.Fulton;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Salvage;

/// <summary>
/// Transports attached entities to the linked beacon after a timer has elapsed.
/// </summary>
public sealed class FultonSystem : SharedFultonSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FultonedComponent, ComponentStartup>(OnFultonedStartup);
        SubscribeLocalEvent<FultonedComponent, ComponentShutdown>(OnFultonedShutdown);
    }

    private void OnFultonedShutdown(EntityUid uid, FultonedComponent component, ComponentShutdown args)
    {
        Del(component.Effect);
        component.Effect = EntityUid.Invalid;
    }

    private void OnFultonedStartup(EntityUid uid, FultonedComponent component, ComponentStartup args)
    {
        if (Exists(component.Effect))
            return;

        component.Effect = Spawn(EffectProto, new EntityCoordinates(uid, EffectOffset));
        Dirty(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FultonedComponent>();
        var curTime = Timing.CurTime;

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NextFulton > curTime)
                continue;

            Fulton(uid, comp);
        }
    }

    private void Fulton(EntityUid uid, FultonedComponent component)
    {
        if (!Deleted(component.Beacon) &&
            TryComp(component.Beacon, out TransformComponent? beaconXform) &&
            !Container.IsEntityOrParentInContainer(component.Beacon.Value, xform: beaconXform) &&
            CanFulton(uid))
        {
            var xform = Transform(uid);
            var metadata = MetaData(uid);
            var oldCoords = xform.Coordinates;
            var offset = _random.NextVector2(1.5f);
            var localPos = Vector2.Transform(
                    TransformSystem.GetWorldPosition(beaconXform),
                    TransformSystem.GetInvWorldMatrix(beaconXform.ParentUid)) + offset;

            TransformSystem.SetCoordinates(uid, new EntityCoordinates(beaconXform.ParentUid, localPos));

            RaiseNetworkEvent(new FultonAnimationMessage()
            {
                Entity = GetNetEntity(uid, metadata),
                Coordinates = GetNetCoordinates(oldCoords),
            });
        }

        Audio.PlayPvs(component.Sound, uid);
        RemCompDeferred<FultonedComponent>(uid);
    }
}
