// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Numerics;
using Content.Server.Destructible;
using Content.Server.Physics.Controllers;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DeadSpace.Blink;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Physics;
using Robust.Shared.Audio.Systems;
using Content.Shared.Tag;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Map;

namespace Content.Server.DeadSpace.Blink;

public sealed class BlinkSystem : SharedBlinkSystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly ProtoId<TagPrototype> WindowTag = "Window";

    public override void Initialize()
    {
        base.Initialize();
        UpdatesAfter.Add(typeof(MoverController));
        SubscribeNetworkEvent<BlinkRequestEvent>(OnBlinkRequest);
        SubscribeLocalEvent<DamageableComponent, DamageChangedEvent>(OnDamage);
        SubscribeLocalEvent<ActiveBlinkDashComponent, StartCollideEvent>(OnDashCollide);
    }

    private void OnDamage(Entity<DamageableComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.Origin == null || args.Origin == ent.Owner)
            return;

        var query = EntityQueryEnumerator<BlinkItemComponent>();
        while (query.MoveNext(out var item, out var blink))
        {
            if (!CanUseItem(ent.Owner, item, blink))
                continue;

            var now = _timing.CurTime;
            EnsureComp<BlinkUserComponent>(ent.Owner).LastDamaged = now;

            var lockoutEnd = now + blink.DamageLockout;
            var cooldownEnd = blink.NextUse > lockoutEnd
                ? blink.NextUse
                : lockoutEnd;
            _alerts.ShowAlert(ent.Owner,
                blink.CooldownAlert,
                cooldown: (now, cooldownEnd),
                autoRemove: false);
            return;
        }
    }

    private void OnBlinkRequest(BlinkRequestEvent msg, EntitySessionEventArgs args)
    {
        var item = GetEntity(msg.Item);
        if (args.SenderSession.AttachedEntity is not { } user ||
            !TryComp<BlinkItemComponent>(item, out var blink) ||
            !CanUseItem(user, item, blink))
            return;

        var target = GetCoordinates(msg.Target);
        var origin = _transform.GetMapCoordinates(user);
        var targetMap = _transform.ToMapCoordinates(target);
        var damage = EnsureComp<BlinkUserComponent>(user);
        var now = _timing.CurTime;

        if (now < blink.NextUse || now < damage.LastDamaged + blink.DamageLockout ||
            origin.MapId != targetMap.MapId || !TryComp<PhysicsComponent>(user, out var physics))
            return;

        var offset = targetMap.Position - origin.Position;
        if (offset.LengthSquared() < 0.001f)
            return;

        var distance = offset.Length();
        if (distance > blink.Range)
            return;

        var direction = offset.Normalized();
        var active = EnsureComp<ActiveBlinkDashComponent>(user);
        active.Direction = direction;
        active.Speed = blink.DashSpeed;
        active.Target = new MapCoordinates(origin.Position + direction * distance, origin.MapId);
        active.EndTime = now + blink.DashTimeout;
        active.StallTimeout = blink.DashStallTimeout;
        active.LastPosition = origin.Position;
        active.LastProgress = now;
        ApplyDashCollision(user, active, physics);
        _physics.SetLinearVelocity(user, direction * blink.DashSpeed, body: physics);
        _audio.PlayPvs(blink.DashSound, user);
        RaiseNetworkEvent(new BlinkDashVisualEvent(GetNetEntity(user), blink.DashTimeout));

        blink.NextUse = now + blink.Cooldown;
        Dirty(item, blink);
        _alerts.ShowAlert(user, blink.CooldownAlert, cooldown: (now, blink.NextUse), autoRemove: false);
    }

    private void OnDashCollide(Entity<ActiveBlinkDashComponent> ent, ref StartCollideEvent args)
    {
        if (!_tags.HasTag(args.OtherEntity, WindowTag))
            return;

        if (TryComp<DamageableComponent>(args.OtherEntity, out var damageable) &&
            _destructible.TryGetDestroyedAt(args.OtherEntity, out var destroyedAt))
        {
            var remainingHealth = FixedPoint2.Max(FixedPoint2.Zero, destroyedAt.Value - damageable.TotalDamage);
            var damage = new DamageSpecifier();
            damage.DamageDict["Structural"] = remainingHealth * 0.5;
            _damageable.TryChangeDamage(args.OtherEntity, damage, ignoreResistances: true, origin: ent.Owner);
        }

        EndDash(ent.Owner);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveBlinkDashComponent, PhysicsComponent>();
        while (query.MoveNext(out var uid, out var active, out var physics))
        {
            var position = _transform.GetMapCoordinates(uid);
            var remaining = active.Target.Position - position.Position;

            if (Vector2.DistanceSquared(position.Position, active.LastPosition) >= 0.0025f)
            {
                active.LastPosition = position.Position;
                active.LastProgress = _timing.CurTime;
            }

            if (_timing.CurTime >= active.EndTime ||
                _timing.CurTime >= active.LastProgress + active.StallTimeout ||
                position.MapId != active.Target.MapId ||
                remaining.LengthSquared() <= 0.04f ||
                Vector2.Dot(remaining, active.Direction) <= 0f)
            {
                EndDash(uid, physics);
                continue;
            }

            _physics.SetLinearVelocity(uid, active.Direction * active.Speed, body: physics);
        }
    }

    private void EndDash(EntityUid uid, PhysicsComponent? physics = null)
    {
        if (TryComp<ActiveBlinkDashComponent>(uid, out var active))
            RestoreCollision(uid, active, physics);

        if (Resolve(uid, ref physics, false))
            _physics.SetLinearVelocity(uid, Vector2.Zero, body: physics);

        RemCompDeferred<ActiveBlinkDashComponent>(uid);
    }

    private void ApplyDashCollision(EntityUid uid, ActiveBlinkDashComponent active, PhysicsComponent physics)
    {
        if (!TryComp<FixturesComponent>(uid, out var fixtures))
            return;

        active.OriginalCollisionMasks.Clear();
        foreach (var (id, fixture) in fixtures.Fixtures)
        {
            active.OriginalCollisionMasks[id] = fixture.CollisionMask;
            var dashMask = fixture.CollisionMask & (int) CollisionGroup.Impassable;
            _physics.SetCollisionMask(uid, id, fixture, dashMask, fixtures, physics);
        }
    }

    private void RestoreCollision(EntityUid uid, ActiveBlinkDashComponent active, PhysicsComponent? physics)
    {
        if (!Resolve(uid, ref physics, false) ||
            !TryComp<FixturesComponent>(uid, out var fixtures))
            return;

        foreach (var (id, mask) in active.OriginalCollisionMasks)
        {
            if (fixtures.Fixtures.TryGetValue(id, out var fixture))
                _physics.SetCollisionMask(uid, id, fixture, mask, fixtures, physics);
        }

        active.OriginalCollisionMasks.Clear();
    }

    private bool CanUseItem(EntityUid user, EntityUid item, BlinkItemComponent blink)
    {
        return blink.NeedHand
            ? _hands.IsHolding(user, item)
            : _inventory.TryGetContainingSlot(item, out _) && Transform(item).ParentUid == user;
    }
}

[RegisterComponent]
public sealed partial class BlinkUserComponent : Component
{
    public TimeSpan LastDamaged = TimeSpan.MinValue;
}

[RegisterComponent]
public sealed partial class ActiveBlinkDashComponent : Component
{
    public Vector2 Direction;
    public float Speed;
    public TimeSpan EndTime;
    public TimeSpan StallTimeout;
    public TimeSpan LastProgress;
    public MapCoordinates Target;
    public Vector2 LastPosition;
    public readonly Dictionary<string, int> OriginalCollisionMasks = new();
}
