using System.Numerics;
using System.Linq;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DeadSpace.CCCCVars;
using Content.Shared.Effects;
using Content.Shared.Projectiles;
using Content.Shared.Sound.Components;
using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Client.GameObjects;
using Robust.Client.Physics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
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
    [Dependency] private readonly RequireProjectileTargetSystem _requireProjectileTarget = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private readonly Dictionary<(uint Id, ushort Index), EntityUid> _predictedProjectiles = new();
    private readonly Dictionary<(uint Id, ushort Index), TimeSpan> _reconciledProjectiles = new();
    private const float PredictionLifetime = 30f;
    private const float AuthoritativeProjectileTimeout = 2f;
    private const float RejectedPredictionHitLifetime = 1f;
    private const float DamagePitchVariation = 0.05f;

    public bool PredictionEnabled => _cfg.GetCVar(CCCCVars.ProjectilePredictionEnabled);

    private void InitializePrediction()
    {
        SubscribeLocalEvent<PredictedProjectileVisualComponent, UpdateIsPredictedEvent>(OnUpdateIsPredicted);
        SubscribeLocalEvent<PredictedProjectileVisualComponent, StartCollideEvent>(OnPredictedStartCollide);
        SubscribeLocalEvent<PredictedProjectileVisualComponent, EntityTerminatingEvent>(OnPredictedTerminating);
        SubscribeLocalEvent<PredictedProjectileComponent, ComponentStartup>(OnAuthoritativeStartup);
        SubscribeLocalEvent<PredictedProjectileComponent, EntityTerminatingEvent>(OnAuthoritativeTerminating);
        SubscribeNetworkEvent<PredictedProjectileReconcileEvent>(OnReconcilePredictedProjectile);
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
            !args.OtherFixture.Hard ||
            !CanPredictHit(ent, args.OtherEntity))
        {
            return;
        }

        if (!_timing.IsFirstTimePredicted)
        {
            ent.Comp.PendingCollision ??= args.OtherEntity;
            return;
        }

        ProcessPredictedCollision(ent, args.OtherEntity);
    }

    private void ProcessPredictedCollision(Entity<PredictedProjectileVisualComponent> ent, EntityUid target)
    {
        if (ent.Comp.HitAt != null || !CanPredictHit(ent, target))
            return;

        if (!TryComp(ent, out ProjectileComponent? projectile))
            return;

        SendPredictedHit(ent, target);

        var hit = new ProjectileHitEvent(projectile.Damage, target, projectile.Shooter);
        RaiseLocalEvent(ent, ref hit);

        ReportPredictedHit(ent, new HashSet<EntityUid> { target });
    }

    private void SendPredictedHit(Entity<PredictedProjectileVisualComponent> ent, EntityUid target)
    {
        RaiseNetworkEvent(new PredictedProjectileHitEvent(
            ent.Comp.PredictionId,
            ent.Comp.ProjectileIndex,
            new HashSet<(NetEntity, MapCoordinates)>
            {
                (GetNetEntity(target), _transform.GetMapCoordinates(target)),
            }));
    }

    private void ReportPredictedHit(Entity<PredictedProjectileVisualComponent> ent, HashSet<EntityUid> targets)
    {
        if (ent.Comp.HitAt != null || targets.Count == 0)
            return;

        ent.Comp.HitAt = _timing.CurTime;
        var current = _transform.GetMapCoordinates(ent);
        if (current.MapId == ent.Comp.Origin.MapId)
            ent.Comp.HitDistance = (current.Position - ent.Comp.Origin.Position).Length();

        PlayPredictedImpact(ent);

        if (TryComp(ent, out ProjectileComponent? projectile))
        {
            PlayPredictedImpactSound(projectile, targets.First());
            projectile.ProjectileSpent = true;
        }

        if (projectile != null && projectile.Damage.AnyPositive())
            _color.RaiseEffect(Color.Red, new List<EntityUid> { targets.First() }, Filter.Local());

        SetVisuals(ent, false);
        if (TryComp(ent, out PhysicsComponent? physics))
            _physics.SetLinearVelocity(ent, Vector2.Zero, body: physics);
    }

    private void PlayPredictedImpactSound(ProjectileComponent projectile, EntityUid target)
    {
        SoundSpecifier? sound = projectile.SoundHit;
        if (!projectile.ForceSound &&
            projectile.Damage.AnyPositive() &&
            TryComp(target, out RangedDamageSoundComponent? rangedSound))
        {
            var type = Content.Shared.Weapons.Melee.SharedMeleeWeaponSystem.GetHighestDamageSound(
                projectile.Damage,
                _prototypes);

            if (type != null && rangedSound.SoundTypes?.TryGetValue(type, out var typedSound) == true)
                sound = typedSound;
            else if (type != null && rangedSound.SoundGroups?.TryGetValue(type, out var groupedSound) == true)
                sound = groupedSound;
        }

        if (sound != null)
            _audio.PlayPredicted(sound, target, _players.LocalEntity,
                AudioParams.Default.WithVariation(DamagePitchVariation));
    }

    private bool CanPredictHit(EntityUid projectile, EntityUid target)
    {
        if (IsClientSide(target))
            return false;

        if (!TryComp(target, out RequireProjectileTargetComponent? requireTarget) ||
            !_requireProjectileTarget.RequiresExplicitTargetForPrediction((target, requireTarget)))
        {
            return true;
        }

        return TryComp(projectile, out TargetedProjectileComponent? targeted) && targeted.Target == target;
    }

    private void OnPredictedTerminating(Entity<PredictedProjectileVisualComponent> ent, ref EntityTerminatingEvent args)
    {
        var key = (ent.Comp.PredictionId, ent.Comp.ProjectileIndex);
        if (_predictedProjectiles.TryGetValue(key, out var current) && current == ent.Owner)
            _predictedProjectiles.Remove(key);
    }

    private void OnAuthoritativeStartup(Entity<PredictedProjectileComponent> ent, ref ComponentStartup args)
    {
        if (!PredictionEnabled || ent.Comp.Shooter != _players.LocalEntity || ent.Comp.PredictionId == 0)
            return;

        var key = (ent.Comp.PredictionId, ent.Comp.ProjectileIndex);
        if (_reconciledProjectiles.Remove(key))
        {
            if (_predictedProjectiles.TryGetValue(key, out var reconciledPredicted) && Exists(reconciledPredicted))
                QueueDel(reconciledPredicted);
            return;
        }

        // The shooter already has a local projectile for this shot. Always hide the server copy,
        // even if the local copy collided or despawned before this component reached the client.
        HideAuthoritative(ent);

        if (!_predictedProjectiles.TryGetValue(key, out var predictedUid) ||
            !TryComp(predictedUid, out PredictedProjectileVisualComponent? predicted))
        {
            return;
        }

        predicted.AuthoritativeProjectile = ent;
    }

    private void OnReconcilePredictedProjectile(PredictedProjectileReconcileEvent ev)
    {
        var key = (ev.PredictionId, ev.ProjectileIndex);
        _reconciledProjectiles[key] = _timing.CurTime;
        if (_predictedProjectiles.TryGetValue(key, out var predicted) && Exists(predicted))
            QueueDel(predicted);

        var query = EntityQueryEnumerator<PredictedProjectileComponent>();
        while (query.MoveNext(out var uid, out var authoritative))
        {
            if (authoritative.Shooter != _players.LocalEntity ||
                authoritative.PredictionId != ev.PredictionId ||
                authoritative.ProjectileIndex != ev.ProjectileIndex)
            {
                continue;
            }

            RevealAuthoritative(uid);
            _reconciledProjectiles.Remove(key);
            break;
        }
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

        if (_timing.IsFirstTimePredicted)
        {
            var contactQuery = EntityQueryEnumerator<PredictedProjectileVisualComponent, FixturesComponent>();
            while (contactQuery.MoveNext(out var uid, out var predicted, out var fixtures))
            {
                if (predicted.HitAt != null)
                    continue;

                var pending = predicted.PendingCollision;
                predicted.PendingCollision = null;
                if (pending is { } pendingTarget &&
                    !TerminatingOrDeleted(pendingTarget) &&
                    CanPredictHit(uid, pendingTarget))
                {
                    ProcessPredictedCollision((uid, predicted), pendingTarget);
                    continue;
                }

                if (TryGetPredictedContact(uid, fixtures, out var target))
                    ProcessPredictedCollision((uid, predicted), target);
            }
        }

        var now = _timing.CurTime;
        foreach (var reconciled in _reconciledProjectiles.ToArray())
        {
            if (now - reconciled.Value > TimeSpan.FromSeconds(PredictionLifetime))
                _reconciledProjectiles.Remove(reconciled.Key);
        }

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

            predicted.AuthoritativeProjectile = null;
            QueueDel(uid);
        }
    }

    private bool TryGetPredictedContact(
        EntityUid projectile,
        FixturesComponent fixtures,
        out EntityUid target)
    {
        if (!fixtures.Fixtures.TryGetValue(SharedProjectileSystem.ProjectileFixture, out var projectileFixture))
        {
            target = default;
            return false;
        }

        foreach (var contact in projectileFixture.Contacts.Values)
        {
            if (contact.Deleting || !contact.Enabled || !contact.IsTouching)
                continue;

            var projectileIsA = contact.EntityA == projectile;
            var targetFixture = projectileIsA ? contact.FixtureB : contact.FixtureA;
            var candidate = projectileIsA ? contact.EntityB : contact.EntityA;

            if (targetFixture?.Hard != true ||
                !CanPredictHit(projectile, candidate))
            {
                continue;
            }

            target = candidate;
            return true;
        }

        target = default;
        return false;
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
