using System.Numerics;
using Content.Shared.Construction.Components;
using Content.Shared.Weapons.Melee.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Weapons.Melee;

/// <summary>
/// This handles <see cref="MeleeThrowOnHitComponent"/>
/// </summary>
public sealed class MeleeThrowOnHitSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MeleeThrowOnHitComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<MeleeThrownComponent, ComponentStartup>(OnThrownStartup);
        SubscribeLocalEvent<MeleeThrownComponent, ComponentShutdown>(OnThrownShutdown);
        SubscribeLocalEvent<MeleeThrownComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnMeleeHit(Entity<MeleeThrowOnHitComponent> ent, ref MeleeHitEvent args)
    {
        var (_, comp) = ent;
        if (!args.IsHit)
            return;

        var mapPos = _transform.GetMapCoordinates(args.User).Position;
        foreach (var hit in args.HitEntities)
        {
            var hitPos = _transform.GetMapCoordinates(hit).Position;
            var angle = args.Direction ?? hitPos - mapPos;
            if (angle == Vector2.Zero)
                continue;

            if (!CanThrowOnHit(ent, hit))
                continue;

            if (comp.UnanchorOnHit && HasComp<AnchorableComponent>(hit))
            {
                _transform.Unanchor(hit, Transform(hit));
            }

            RemComp<MeleeThrownComponent>(hit);
            var ev = new MeleeThrowOnHitStartEvent(args.User, ent);
            RaiseLocalEvent(hit, ref ev);
            var thrownComp = new MeleeThrownComponent
            {
                Velocity = angle.Normalized() * comp.Speed,
                Lifetime = comp.Lifetime,
                MinLifetime = comp.MinLifetime
            };
            AddComp(hit, thrownComp);
        }
    }

    private void OnThrownStartup(Entity<MeleeThrownComponent> ent, ref ComponentStartup args)
    {
        var (_, comp) = ent;

        if (!TryComp<PhysicsComponent>(ent, out var body) ||
            (body.BodyType & (BodyType.Dynamic | BodyType.KinematicController)) == 0x0)
            return;

        comp.ThrownEndTime = _timing.CurTime + TimeSpan.FromSeconds(comp.Lifetime);
        comp.MinLifetimeTime = _timing.CurTime + TimeSpan.FromSeconds(comp.MinLifetime);
        _physics.SetBodyStatus(body, BodyStatus.InAir);
        _physics.SetLinearVelocity(ent, Vector2.Zero, body: body);
        _physics.ApplyLinearImpulse(ent, comp.Velocity * body.Mass, body: body);
        Dirty(ent, ent.Comp);
    }

    private void OnThrownShutdown(Entity<MeleeThrownComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<PhysicsComponent>(ent, out var body))
            _physics.SetBodyStatus(body, BodyStatus.OnGround);
        var ev = new MeleeThrowOnHitEndEvent();
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnStartCollide(Entity<MeleeThrownComponent> ent, ref StartCollideEvent args)
    {
        var (_, comp) = ent;
        if (!args.OtherFixture.Hard || !args.OtherBody.CanCollide || !args.OurFixture.Hard || !args.OurBody.CanCollide)
            return;

        if (_timing.CurTime < comp.MinLifetimeTime)
            return;

        RemCompDeferred(ent, ent.Comp);
    }

    public bool CanThrowOnHit(Entity<MeleeThrowOnHitComponent> ent, EntityUid target)
    {
        var (uid, comp) = ent;

        var ev = new AttemptMeleeThrowOnHitEvent(target);
        RaiseLocalEvent(uid, ref ev);

        if (ev.Handled)
            return !ev.Cancelled;

        return comp.Enabled;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MeleeThrownComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime > comp.ThrownEndTime)
                RemCompDeferred(uid, comp);
        }
    }
}
