using Content.Shared.Physics;
using Content.Shared.Throwing;
using Content.Shared.Timing;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;
using Content.Shared.Wieldable;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using System.Numerics;

namespace Content.Shared.RepulseAttract;

public sealed class RepulseAttractSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ThrowingSystem _throw = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedTransformSystem _xForm = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RepulseAttractComponent, AttemptMeleeEvent>(OnMeleeAttempt, after: [typeof(SharedWieldableSystem)]);
    }
    private void OnMeleeAttempt(Entity<RepulseAttractComponent> ent, ref AttemptMeleeEvent args)
    {
        if (args.Cancelled)
            return;

        if (_delay.IsDelayed(ent.Owner))
            return;

        TryRepulseAttract(ent, args.User);
    }

    public bool TryRepulseAttract(Entity<RepulseAttractComponent> ent, EntityUid user)
    {
        var position = _xForm.GetMapCoordinates(ent.Owner);
        return TryRepulseAttract(ent.Comp.Attract, position, ent.Comp.Speed, ent.Comp.Range, ent.Comp.Whitelist, ent.Comp.Blacklist);
    }

    public bool TryRepulseAttract(bool attract, MapCoordinates position, float speed, float range, EntityWhitelist? whitelist = null, EntityWhitelist? blacklist = null)
    {
        var entsInRange = _lookup.GetEntitiesInRange(position, range, flags: LookupFlags.Dynamic | LookupFlags.Sundries);
        var epicenter = position.Position;
        var bodyQuery = GetEntityQuery<PhysicsComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();

        foreach (var target in entsInRange)
        {
            if (!bodyQuery.TryGetComponent(target, out var physics)
                || physics.BodyType == BodyType.Static
                || physics.CollisionLayer == (int)CollisionGroup.GhostImpassable) // don't affect ghosts
                continue;

            if (_whitelist.IsWhitelistFail(whitelist, target) || _whitelist.IsBlacklistPass(blacklist, target))
                continue;

            var targetXForm = xformQuery.GetComponent(target);

            var targetPos = _xForm.GetMapCoordinates(target, targetXForm).Position;

            // vector from epicenter to target entity
            var direction = targetPos - epicenter;

            if (direction == Vector2.Zero)
                continue;

            // attract: throw all items directly to to the epicenter
            // repulse: throw them up to the maximum range
            var throwDirection = attract ? -direction : direction.Normalized() * (range - direction.Length());

            _throw.TryThrow(target, throwDirection, speed);
        }

        return true;
    }
}
