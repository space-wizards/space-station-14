using Content.Shared._Impstation.Pudge;
using Robust.Shared.Prototypes;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Content.Server.Popups;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Content.Server.DoAfter;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Server.Body.Systems;
using Timer = Robust.Shared.Timing.Timer;
using Content.Shared.Actions;
using Robust.Shared.Physics.Systems;
using System.Numerics;
using Content.Shared.Physics;
using Content.Shared.Weapons.Misc;
using Content.Shared.Projectiles;
using Robust.Shared.Physics;
using Robust.Shared.Timing;
using Robust.Shared.Network;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Map;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Server._Impstation.Pudge;

public sealed partial class PudgeSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly SharedJointSystem _joints = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    private readonly SoundSpecifier _meatHookSFX = new SoundPathSpecifier("/Audio/_Impstation/Pudge/PudgeHook.ogg");
    private readonly SoundSpecifier _meatHookVOSFX = new SoundPathSpecifier("/Audio/_Impstation/Pudge/PudgeHookVO.ogg");
    private readonly SoundSpecifier _rotSFX = new SoundPathSpecifier("/Audio/_Impstation/Pudge/PudgeRotVO.ogg");
    private readonly SoundSpecifier _meatShieldSFX = new SoundPathSpecifier("/Audio/_Impstation/Pudge/PudgeMeatShieldVO.ogg");
    private readonly SoundSpecifier _deflectSFX = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");
    private readonly SoundSpecifier _dismemberSFX = new SoundPathSpecifier("/Audio/_Impstation/Pudge/PudgeDevourVO.ogg");
    private readonly SoundSpecifier _chowSFX = new SoundPathSpecifier("/Audio/_Impstation/Pudge/PudgeDismember.ogg");
    private readonly EntProtoId _meatShieldVFX = "PudgeMeatShieldVFX";
    public ProtoId<DamageGroupPrototype> ChowDamageGroup = "Brute";
    public const string GrapplingJoint = "grappling";
    public override void Initialize()
    {
        base.Initialize();
        //NO PUDGE COMPONENT!!!!!
        SubscribeLocalEvent<ActionsComponent, PudgeMeatHookEvent>(OnMeatHook);
        SubscribeLocalEvent<MeatHookComponent, ProjectileEmbedEvent>(OnMeatHookCollide);
        SubscribeLocalEvent<MeatHookComponent, JointRemovedEvent>(OnGrappleJointRemoved);
        SubscribeLocalEvent<MeatHookComponent, RemoveEmbedEvent>(OnRemoveEmbed);

        SubscribeLocalEvent<ActionsComponent, PudgeRotEvent>(OnRot);

        SubscribeLocalEvent<ActionsComponent, PudgeMeatShieldEvent>(OnMeatShield);
        SubscribeLocalEvent<MeatShieldComponent, BeforeDamageChangedEvent>(OnMeatShieldDamaged);

        SubscribeLocalEvent<ActionsComponent, PudgeDismemberEvent>(OnDismember);
        SubscribeLocalEvent<ActionsComponent, PudgeDismemberDoAfterEvent>(OnDismemberDoAfter);
    }

    #region meat's hook
    private void OnMeatHook(EntityUid uid, ActionsComponent actions, ref PudgeMeatHookEvent args)
    {
        var pudge = args.Performer;

        var xform = Transform(args.Performer);
        // Get the tile in front of the pudge
        var offsetValue = xform.LocalRotation.ToWorldVec();
        var coords = xform.Coordinates.Offset(offsetValue).SnapToGrid(EntityManager, _mapMan);

        _audio.PlayPvs(_meatHookVOSFX, uid, AudioParams.Default.WithVolume(-3f));
        _audio.PlayPvs(_meatHookSFX, uid, AudioParams.Default.WithVolume(-3f));

        var toCoords = args.Target;
        var userVelocity = _physics.GetMapLinearVelocity(pudge);

        var ent = Spawn("MeatHookPudge", coords);
        var direction = toCoords.ToMapPos(EntityManager, _transform) -
                        coords.ToMapPos(EntityManager, _transform);

        EnsureComp<MeatHookComponent>(ent, out var component);
        component.Projectile = ent;
        Dirty(ent, component);
        var visuals = EnsureComp<JointVisualsComponent>(ent);
        visuals.Sprite = component.RopeSprite;
        visuals.OffsetA = new Vector2(0f, 0.5f);
        visuals.Target = GetNetEntity(uid);
        Dirty(ent, visuals);

        TryComp<AppearanceComponent>(uid, out var appearance);
        _appearance.SetData(uid, SharedTetherGunSystem.TetherVisualsStatus.Key, false, appearance);
        Dirty(ent, component);

        _gun.ShootProjectile(ent, direction, userVelocity, pudge, pudge);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MeatHookComponent>();

        while (query.MoveNext(out var uid, out var grappling))
        {
            if (!grappling.Reeling)
            {
                if (Timing.IsFirstTimePredicted)
                {
                    //Just in case.
                    grappling.Stream = _audio.Stop(grappling.Stream);
                }
                continue;
            }
            var pudgeQuery = EntityQueryEnumerator<JointComponent>();

            while (pudgeQuery.MoveNext(out var pudge, out var jointComp))
            {
                if (!jointComp.GetJoints.TryGetValue(GrapplingJoint, out var joint) ||
                    joint is not DistanceJoint distance)
                {
                    SetReeling(uid, grappling, false);
                    continue;
                }

                // TODO: This should be on engine.
                distance.MaxLength = MathF.Max(distance.MinLength, distance.MaxLength - grappling.ReelRate * frameTime);
                distance.Length = MathF.Min(distance.MaxLength, distance.Length);

                _physics.WakeBody(joint.BodyAUid);
                _physics.WakeBody(joint.BodyBUid);

                if (jointComp.Relay != null)
                {
                    _physics.WakeBody(jointComp.Relay.Value);
                }

                Dirty(pudge, jointComp);

                if (distance.MaxLength.Equals(distance.MinLength))
                {
                    SetReeling(uid, grappling, false);
                }
            }
        }
    }

    private void SetReeling(EntityUid uid, MeatHookComponent component, bool value)
    {
        if (component.Reeling == value)
            return;

        component.Reeling = value;
        Dirty(uid, component);
    }

    private void OnGrappleJointRemoved(EntityUid uid, MeatHookComponent component, JointRemovedEvent args)
    {
        if (_netManager.IsServer)
            QueueDel(uid);
    }

    private void OnMeatHookCollide(EntityUid uid, MeatHookComponent component, ref ProjectileEmbedEvent args)
    {
        if (!Timing.IsFirstTimePredicted)
            return;
        //joint between the embedded and the weapon
        var jointComp = EnsureComp<JointComponent>(args.Weapon);
        var joint = _joints.CreateDistanceJoint(args.Weapon, args.Embedded, anchorA: new Vector2(0f, 0.5f), id: GrapplingJoint);
        joint.MaxLength = joint.Length + 0.2f;
        joint.Stiffness = 1f;
        joint.MinLength = 2.5f;
        // Setting velocity directly for mob movement fucks this so need to make them aware of it.
        // joint.Breakpoint = 4000f;
        Dirty(args.Weapon, jointComp);

        SetReeling(uid, component, true);
    }

    private void OnRemoveEmbed(EntityUid uid, MeatHookComponent component, RemoveEmbedEvent args)
    {
        if (TryComp<EmbeddableProjectileComponent>(uid, out var projectile))
        {
            if (projectile.EmbeddedIntoUid != null)
            {
                _joints.ClearJoints(projectile.EmbeddedIntoUid.Value);
            }
        }
    }
    # endregion

    #region other stuff
    private void OnRot(EntityUid uid, ActionsComponent component, ref PudgeRotEvent args)
    {
        _popup.PopupEntity(Loc.GetString("pudge-rot-popup"), args.Performer, args.Performer);
        _audio.PlayPvs(_rotSFX, uid, AudioParams.Default.WithVolume(-3f));

        var tileMix = _atmos.GetTileMixture(args.Performer, excite: true);
        tileMix?.AdjustMoles(Gas.Ammonia, 300);
        //MAYBE ADD A FOAM CLOUD OR PUDDLE SPILL OR SOMETHING
    }

    private void OnMeatShield(EntityUid uid, ActionsComponent component, ref PudgeMeatShieldEvent args)
    {
        if (HasComp<MeatShieldComponent>(uid))
        {
            _popup.PopupEntity(Loc.GetString("pudge-ability-active"), uid, uid);
            return;
        }
        _audio.PlayPvs(_meatShieldSFX, uid, AudioParams.Default.WithVolume(-3f));
        EnsureComp<MeatShieldComponent>(uid, out var shield);
        Timer.Spawn(TimeSpan.FromSeconds(5.8), () => RemComp(uid, shield));
        Spawn(_meatShieldVFX, Transform(uid).Coordinates);
        args.Handled = true;
    }

    private void OnMeatShieldDamaged(Entity<MeatShieldComponent> uid, ref BeforeDamageChangedEvent args)
    {
        args.Cancelled = true;
        _audio.PlayPvs(_deflectSFX, uid, AudioParams.Default.WithVolume(-3f));
    }
    #endregion

    #region dismember
    private void OnDismember(EntityUid uid, ActionsComponent component, ref PudgeDismemberEvent args)
    {
        var target = args.Target;

        var popupSelf = Loc.GetString("pudge-dismember-start-self", ("target", Identity.Entity(target, EntityManager)));
        var popupTarget = Loc.GetString("pudge-dismember-start-target");
        var popupOthers = Loc.GetString("pudge-dismember-start-others", ("user", Identity.Entity(target, EntityManager)), ("target", Identity.Entity(target, EntityManager)));

        _popup.PopupEntity(popupSelf, uid, uid);
        _popup.PopupEntity(popupTarget, target, target, PopupType.MediumCaution);
        _popup.PopupEntity(popupOthers, uid, Filter.Pvs(uid).RemovePlayersByAttachedEntity([uid, target]), true, PopupType.MediumCaution);

        _audio.PlayPvs(_dismemberSFX, uid, AudioParams.Default.WithVolume(-3f));

        TryDismember(uid, target);
    }
    private void TryDismember(EntityUid uid, EntityUid? target)
    {
        var dargs = new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(0.75), new PudgeDismemberDoAfterEvent(), uid, target)
        {
            DistanceThreshold = 1f,
            BreakOnDamage = false,
            BreakOnHandChange = false,
            BreakOnMove = false,
            BreakOnWeightlessMove = false,
            AttemptFrequency = AttemptFrequency.StartAndEnd
        };
        _doAfter.TryStartDoAfter(dargs);
    }
    private void OnDismemberDoAfter(EntityUid uid, ActionsComponent component, ref PudgeDismemberDoAfterEvent args)
    {
        var target = args.Args.Target;
        if (args.Cancelled || args.Handled || target == null || _mobState.IsDead(target.Value))
            return;

        _audio.PlayPvs(_chowSFX, uid, AudioParams.Default.WithVolume(-3f));

        var dmg = new DamageSpecifier(_proto.Index(ChowDamageGroup), 11);
        _damageable.TryChangeDamage(target, dmg, false, true);

        var ichorInjection = new Solution("Ichor", 10f);
        _bloodstreamSystem.TryAddToChemicals(uid, ichorInjection);

        TryDismember(uid, target);
    }
}
#endregion
