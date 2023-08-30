using System.Numerics;
using Content.Shared.Salvage.Fulton;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Salvage;

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
        if (!Deleted(component.Beacon))
        {
            var xform = Transform(uid);
            var oldCoords = xform.Coordinates;
            var offset = _random.NextVector2(1.5f);
            TransformSystem.SetCoordinates(uid, new EntityCoordinates(component.Beacon.Value, offset));

            RaiseNetworkEvent(new FultonAnimationMessage()
            {
                Entity = uid,
                Coordinates = oldCoords,
            });
        }

        Audio.PlayPvs(component.Sound, uid);
        RemCompDeferred<FultonedComponent>(uid);
    }
}
