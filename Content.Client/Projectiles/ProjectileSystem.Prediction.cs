using System.Numerics;
using Content.Shared.Damage.Components;
using Content.Shared.DeadSpace.CCCCVars;
using Content.Shared.Projectiles;
using Content.Shared.Sound.Components;
using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Components.Triggers;
using Robust.Client.GameObjects;
using Robust.Client.Physics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using TimedDespawnComponent = Robust.Shared.Spawners.TimedDespawnComponent;

namespace Content.Client.Projectiles;

public sealed partial class ProjectileSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPointLightSystem _lights = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly Dictionary<(uint Id, ushort Index), EntityUid> _predictedProjectiles = new();
    private const float PredictionLifetime = 30f;
    private const float AuthoritativeProjectileTimeout = 2f;
    private const float RejectedPredictionHitLifetime = 1f;

    public bool PredictionEnabled => _cfg.GetCVar(CCCCVars.ProjectilePredictionEnabled);

    private void InitializePrediction()
    {
        SubscribeLocalEvent<PredictedProjectileVisualComponent, UpdateIsPredictedEvent>(OnUpdateIsPredicted);
        SubscribeLocalEvent<PredictedProjectileVisualComponent, StartCollideEvent>(OnPredictedStartCollide);
        SubscribeLocalEvent<PredictedProjectileVisualComponent, EntityTerminatingEvent>(OnPredictedTerminating);
        SubscribeLocalEvent<PredictedProjectileComponent, ComponentStartup>(OnAuthoritativeStartup);
        SubscribeLocalEvent<PredictedProjectileComponent, EntityTerminatingEvent>(OnAuthoritativeTerminating);
        SubscribeLocalEvent<PhysicsUpdateBeforeSolveEvent>(OnBeforeSolve);
        SubscribeLocalEvent<PhysicsUpdateAfterSolveEvent>(OnAfterSolve);

        UpdatesBefore.Add(typeof(TransformSystem));
    }

    public void RegisterPredictedProjectile(EntityUid uid, uint predictionId, ushort projectileIndex)
    {
        if (!PredictionEnabled || predictionId == 0 || !IsClientSide(uid))
            return;

        var key = (predictionId, projectileIndex);
        if (_predictedProjectiles.TryGetValue(key, out var old) && old != uid && Exists(old))
            QueueDel(old);

        var predicted = EnsureComp<PredictedProjectileVisualComponent>(uid);
        predicted.PredictionId = predictionId;
        predicted.ProjectileIndex = projectileIndex;
        predicted.Origin = _transform.GetMapCoordinates(uid);
        predicted.CreatedAt = _timing.CurTime;
        _predictedProjectiles[key] = uid;

        // This entity is a moving visual proxy. Its lifetime and collisions are reconciled here;
        // gameplay triggers must only run on the authoritative server projectile.
        RemComp<TimedDespawnComponent>(uid);
        RemComp<TimerTriggerComponent>(uid);
        RemComp<RandomTimerTriggerComponent>(uid);
        RemComp<ActiveTimerTriggerComponent>(uid);
        RemComp<TriggerOnCollideComponent>(uid);
        RemComp<TriggerOnTimedCollideComponent>(uid);
        RemComp<ActiveTriggerOnTimedCollideComponent>(uid);

        if (HasComp<TriggerOnProximityComponent>(uid) && TryComp(uid, out PhysicsComponent? body))
            _fixtures.DestroyFixture(uid, TriggerOnProximityComponent.FixtureID, body: body);

        RemComp<TriggerOnProximityComponent>(uid);
        RemComp<DamageContactsComponent>(uid);
        RemComp<DamageOnHighSpeedImpactComponent>(uid);
        RemComp<EmitSoundOnCollideComponent>(uid);

        _physics.UpdateIsPredicted(uid);
        Transform(uid).ActivelyLerping = false;
    }

    private void OnUpdateIsPredicted(Entity<PredictedProjectileVisualComponent> ent, ref UpdateIsPredictedEvent args)
    {
        args.IsPredicted = true;
    }

    private void OnBeforeSolve(ref PhysicsUpdateBeforeSolveEvent args)
    {
        var query = EntityQueryEnumerator<PredictedProjectileVisualComponent>();
        while (query.MoveNext(out var uid, out var predicted))
        {
            if (!TerminatingOrDeleted(uid))
                predicted.CoordinatesBeforePredictionReplay = Transform(uid).Coordinates;
        }
    }

    private void OnAfterSolve(ref PhysicsUpdateAfterSolveEvent args)
    {
        if (_timing.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<PredictedProjectileVisualComponent>();
        while (query.MoveNext(out var uid, out var predicted))
        {
            if (!TerminatingOrDeleted(uid) && predicted.CoordinatesBeforePredictionReplay is { } coordinates)
                _transform.SetCoordinates(uid, coordinates);

            predicted.CoordinatesBeforePredictionReplay = null;
        }
    }

    private void OnPredictedStartCollide(Entity<PredictedProjectileVisualComponent> ent, ref StartCollideEvent args)
    {
        if (ent.Comp.HitAt != null ||
            args.OurFixtureId != SharedProjectileSystem.ProjectileFixture ||
            !args.OtherFixture.Hard)
        {
            return;
        }

        ent.Comp.HitAt = _timing.CurTime;
        var current = _transform.GetMapCoordinates(ent);
        if (current.MapId == ent.Comp.Origin.MapId)
            ent.Comp.HitDistance = (current.Position - ent.Comp.Origin.Position).Length();

        SetVisuals(ent, false);
        if (TryComp(ent, out PhysicsComponent? physics))
            _physics.SetLinearVelocity(ent, Vector2.Zero, body: physics);
    }

    private void OnPredictedTerminating(Entity<PredictedProjectileVisualComponent> ent, ref EntityTerminatingEvent args)
    {
        var key = (ent.Comp.PredictionId, ent.Comp.ProjectileIndex);
        if (_predictedProjectiles.TryGetValue(key, out var current) && current == ent.Owner)
            _predictedProjectiles.Remove(key);
    }

    private void OnAuthoritativeStartup(Entity<PredictedProjectileComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.Shooter != _players.LocalEntity || ent.Comp.PredictionId == 0)
            return;

        var key = (ent.Comp.PredictionId, ent.Comp.ProjectileIndex);
        if (!_predictedProjectiles.TryGetValue(key, out var predictedUid) ||
            !TryComp(predictedUid, out PredictedProjectileVisualComponent? predicted))
        {
            return;
        }

        predicted.AuthoritativeProjectile = ent;
        HideAuthoritative(ent);
    }

    private void OnAuthoritativeTerminating(Entity<PredictedProjectileComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.Shooter != _players.LocalEntity || ent.Comp.PredictionId == 0)
            return;

        var key = (ent.Comp.PredictionId, ent.Comp.ProjectileIndex);
        if (_predictedProjectiles.TryGetValue(key, out var predicted) && Exists(predicted))
            QueueDel(predicted);
    }

    private void HideAuthoritative(EntityUid uid)
    {
        if (HasComp<HiddenPredictedProjectileComponent>(uid))
            return;

        var hidden = EnsureComp<HiddenPredictedProjectileComponent>(uid);
        if (TryComp(uid, out SpriteComponent? sprite))
        {
            hidden.SpriteVisible = sprite.Visible;
            _sprite.SetVisible((uid, sprite), false);
        }

        if (TryComp(uid, out PointLightComponent? light))
        {
            hidden.LightEnabled = light.Enabled;
            _lights.SetEnabled(uid, false, light);
        }
    }

    private void RevealAuthoritative(EntityUid uid)
    {
        if (TerminatingOrDeleted(uid) || !TryComp(uid, out HiddenPredictedProjectileComponent? hidden))
            return;

        if (TryComp(uid, out SpriteComponent? sprite))
            _sprite.SetVisible((uid, sprite), hidden.SpriteVisible);

        if (TryComp(uid, out PointLightComponent? light))
            _lights.SetEnabled(uid, hidden.LightEnabled, light);

        RemCompDeferred<HiddenPredictedProjectileComponent>(uid);
    }

    private void SetVisuals(EntityUid uid, bool visible)
    {
        if (TryComp(uid, out SpriteComponent? sprite))
            _sprite.SetVisible((uid, sprite), visible);

        if (TryComp(uid, out PointLightComponent? light))
            _lights.SetEnabled(uid, visible, light);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<PredictedProjectileVisualComponent>();
        while (query.MoveNext(out var uid, out var predicted))
        {
            if (TerminatingOrDeleted(uid))
                continue;

            if (now - predicted.CreatedAt > TimeSpan.FromSeconds(PredictionLifetime))
            {
                if (predicted.AuthoritativeProjectile is { } staleAuthoritative && Exists(staleAuthoritative))
                    RevealAuthoritative(staleAuthoritative);

                QueueDel(uid);
                continue;
            }

            if (predicted.AuthoritativeProjectile is not { } authoritative ||
                TerminatingOrDeleted(authoritative))
            {
                if (predicted.HitAt is { } hitAt &&
                    now - hitAt > TimeSpan.FromSeconds(RejectedPredictionHitLifetime))
                {
                    QueueDel(uid);
                }
                else if (now - predicted.CreatedAt > TimeSpan.FromSeconds(AuthoritativeProjectileTimeout))
                {
                    QueueDel(uid);
                }

                continue;
            }

            if (predicted.HitAt == null || predicted.HitDistance is not { } hitDistance)
                continue;

            var authoritativePosition = _transform.GetMapCoordinates(authoritative);
            if (authoritativePosition.MapId != predicted.Origin.MapId)
                continue;

            var authoritativeDistance = (authoritativePosition.Position - predicted.Origin.Position).Length();
            if (authoritativeDistance + 0.25f < hitDistance)
                continue;

            RevealAuthoritative(authoritative);
            predicted.AuthoritativeProjectile = null;
            QueueDel(uid);
        }
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<PredictedProjectileVisualComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (!TerminatingOrDeleted(uid))
                xform.ActivelyLerping = false;
        }
    }
}
