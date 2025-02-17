// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics;
using Content.Shared.DeadSpace.Sith.Components;
using Content.Shared.Stunnable;
using Content.Shared.DeadSpace.Sith;
using Content.Shared.StatusEffect;
using Content.Shared.Physics;
using System.Numerics;
using Content.Server.Singularity.Components;
using Content.Shared.Atmos.Components;
using Content.Shared.Ghost;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Content.Shared.Verbs;
using Robust.Shared.Utility;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Robust.Shared.Spawners;
using Content.Shared.Alert;
using Content.Shared.Item;

namespace Content.Server.DeadSpace.Sith;

public sealed class SithForceAbilitySystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    
    public const float MinGravPulseRange = 0.00001f;
    public const float MinRange = 0.01f;
    public const float MaxStrenghtPush = 15f;
    public const float MaxStrenghtPull = 1f;
    public const float OneItemStrenghtMultiply = 10f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SithForceAbilityComponent, SithForceEvent>(OnSithForce);
        SubscribeLocalEvent<SithForceAbilityComponent, SithForceOneEvent>(OnSithForceOne);
        SubscribeLocalEvent<SithForceAbilityComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SithForceAbilityComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<SithForceAbilityComponent, GetVerbsEvent<Verb>>(OnSetVerbs);
        SubscribeLocalEvent<SithForceAbilityComponent, ChangeForcePowerAlertEvent>(OnChangeForcePowerAlert);
    }

    public override void Update(float frameTime)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        var curTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<SithForceAbilityComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var component, out var xform))
        {
            if (component.NextPulseTime <= curTime && component.IsActiveAbility)
                Force(uid, component, xform);
        }
    }

    private void OnChangeForcePowerAlert(Entity<SithForceAbilityComponent> ent, ref ChangeForcePowerAlertEvent args)
    {
        SetForce(ent.Owner, ent.Comp);

        args.Handled = true;
    }

    private void OnSetVerbs(EntityUid uid, SithForceAbilityComponent component, GetVerbsEvent<Verb> args)
    {
        if (uid != args.User)
            return;

        if (component.IsActiveAbility)
            return;

        if (component.BaseRadialAcceleration > 0)
        {
            args.Verbs.Add(new Verb()
            {
                Text = Loc.GetString("Поменять поток силы: отталкивание"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/dot.svg.192dpi.png")),
                Act = () => SetForce(uid, component),
                Impact = LogImpact.High
            });
        }
        else
        {
            args.Verbs.Add(new Verb()
            {
                Text = Loc.GetString("Поменять поток силы: притяжение"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/dot.svg.192dpi.png")),
                Act = () => SetForce(uid, component),
                Impact = LogImpact.High
            });
        }
    }
    private void OnComponentInit(EntityUid uid, SithForceAbilityComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.ActionSithForceEntity, component.ActionSithForce, uid);
        _actionsSystem.AddAction(uid, ref component.ActionSithForceOneEntity, component.ActionSithForceOnce, uid);

        if (MaxStrenghtPush < component.StrenghtPush)
        {
            component.Strenght = MaxStrenghtPush;
        }
        else
        {
            component.Strenght = component.StrenghtPush;
        }

        UpdateForceAlert(uid, component);
    }

    private void OnComponentShutdown(EntityUid uid, SithForceAbilityComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.ActionSithForceEntity);
        _actionsSystem.RemoveAction(uid, component.ActionSithForceOneEntity);
    }
    private void OnSithForceOne(EntityUid uid, SithForceAbilityComponent component, SithForceOneEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;

        if (component.IsActiveAbility)
            return;

        if (!TryComp<PhysicsComponent>(target, out var physics)
                || physics.BodyType == BodyType.Static)
        {
            return;
        }

        if (!CanGravPulseAffect(target))
            return;

        args.Handled = true;

        var fromCoordinates = Transform(target).Coordinates;
        var toCoordinates = Transform(uid).Coordinates;

        var fromMap = fromCoordinates.ToMapPos(EntityManager, _transform);
        var toMap = toCoordinates.ToMapPos(EntityManager, _transform);

        var shotDirection = (toMap - fromMap).Normalized();

        var scaling = component.StrenghtPushPullOne * physics.Mass;
        var impulseVector = shotDirection;

        if (HasComp<ItemComponent>(target))
            scaling *= OneItemStrenghtMultiply;

        _stun.TryParalyze(target, TimeSpan.FromSeconds(component.StunDuration), true);

        if (component.BaseRadialAcceleration < 0)
        {
            _physics.ApplyLinearImpulse(target, -impulseVector * scaling, body: physics);
        }
        else
        {
            _physics.ApplyLinearImpulse(target, impulseVector * scaling, body: physics);
        }

        var forceSound = component.SoundPush;

        if (component.BaseRadialAcceleration > 0)
        {
            forceSound = component.SoundPull;
        }

        if (forceSound == null)
            return;

        _audio.PlayPvs(forceSound, uid);
    }

    private void OnSithForce(EntityUid uid, SithForceAbilityComponent component, SithForceEvent args)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        if (args.Handled)
            return;

        if (!TryComp<TransformComponent>(uid, out var xform))
            return;

        if (component.IsActiveAbility)
            return;

        args.Handled = true;

        component.IsActiveAbility = true;

        var forceEnt = component.PushForcePower;
        var forceSound = component.SoundPush;

        if (component.BaseRadialAcceleration > 0)
        {
            forceEnt = component.PullForcePower;
            forceSound = component.SoundPull;
        }

        var forcePowerEnt = Spawn(forceEnt, Transform(uid).Coordinates);

        if (TryComp<TimedDespawnComponent>(forcePowerEnt, out var timedDespawnComp))
        {
            TimeSpan durationEffect = TimeSpan.FromSeconds(timedDespawnComp.Lifetime * component.NumberOfPulses);
            _statusEffect.TryAddStatusEffect<StunnedComponent>(uid, "Stun", durationEffect, true);
        }

        Force(uid, component, xform);

        if (forceSound == null)
            return;

        _audio.PlayPvs(forceSound, uid);
    }

    private void Force(EntityUid uid, SithForceAbilityComponent component, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref xform))
            return;

        var entityPos = xform.Coordinates;
        var minRange2 = MathF.Max(MinRange * MinRange, MinGravPulseRange);
        var mapPos = _transform.ToMapCoordinates(entityPos);
        var epicenter = mapPos.Position;
        var maxRange = component.Range;

        var baseMatrixDeltaV = new Matrix3x2(component.BaseRadialAcceleration, -component.BaseTangentialAcceleration, component.BaseTangentialAcceleration, component.BaseRadialAcceleration, 0.0f, 0.0f);

        var bodyQuery = GetEntityQuery<PhysicsComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();

        foreach (var entity in _lookup.GetEntitiesInRange(mapPos.MapId, epicenter, maxRange, flags: LookupFlags.Dynamic | LookupFlags.Sundries))
        {
            if (entity == uid)
                continue;

            if (!_interaction.InRangeUnobstructed(uid, entity, 0f, CollisionGroup.Opaque))
                continue;

            if (!bodyQuery.TryGetComponent(entity, out var physics)
                || physics.BodyType == BodyType.Static)
            {
                continue;
            }

            if (TryComp<MovedByPressureComponent>(entity, out var movedPressure) && !movedPressure.Enabled)
                continue;

            if (!CanGravPulseAffect(entity))
                continue;

            _stun.TryParalyze(entity, TimeSpan.FromSeconds(component.StunDuration), true);

            var displacement = epicenter - _transform.GetWorldPosition(entity, xformQuery);
            var distance2 = displacement.LengthSquared();
            if (distance2 < minRange2)
                continue;

            var scaling = component.Strenght * physics.Mass;
            _physics.ApplyLinearImpulse(entity, Vector2.TransformNormal(displacement, baseMatrixDeltaV) * scaling, body: physics);
        }

        component.NextPulseTime = _gameTiming.CurTime + TimeSpan.FromSeconds(component.NextPulseDuration);
        component.Pulse++;
        if (component.NumberOfPulses <= component.Pulse)
        {
            component.IsActiveAbility = false;
            component.Pulse = 0;
        }
    }
    private bool CanGravPulseAffect(EntityUid entity)
    {
        return !(
            EntityManager.HasComponent<GhostComponent>(entity) ||
            EntityManager.HasComponent<MapGridComponent>(entity) ||
            EntityManager.HasComponent<MapComponent>(entity) ||
            EntityManager.HasComponent<GravityWellComponent>(entity)
        );
    }
    public void SetForce(EntityUid uid, SithForceAbilityComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.BaseRadialAcceleration *= -1;

        if (component.BaseRadialAcceleration < 0)
        {
            if (MaxStrenghtPush < component.StrenghtPush)
            {
                component.Strenght = MaxStrenghtPush;
            }
            else
            {
                component.Strenght = component.StrenghtPush;
            }
        }
        else
        {
            if (MaxStrenghtPull < component.StrenghtPull)
            {
                component.Strenght = MaxStrenghtPull;
            }
            else
            {
                component.Strenght = component.StrenghtPull;
            }
        }

        UpdateForceAlert(uid, component);
    }
    public void UpdateForceAlert(EntityUid uid, SithForceAbilityComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        short severity = 1;

        if (component.BaseRadialAcceleration <= 0) severity = 1;
        if (component.BaseRadialAcceleration > 0) severity = 2;

        _alerts.ClearAlert(uid, component.ForcePowerAlert);
        _alerts.ShowAlert(uid, component.ForcePowerAlert, severity);
    }
}
