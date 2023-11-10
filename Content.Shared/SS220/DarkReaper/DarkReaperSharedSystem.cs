// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Linq;
using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.DarkReaper;

public abstract class SharedDarkReaperSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifier = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DarkReaperComponent, ComponentStartup>(OnCompInit);
        SubscribeLocalEvent<DarkReaperComponent, ComponentShutdown>(OnCompShutdown);

        // actions
        SubscribeLocalEvent<DarkReaperComponent, ReaperRoflEvent>(OnRoflAction);
        SubscribeLocalEvent<DarkReaperComponent, ReaperConsumeEvent>(OnConsumeAction);
        SubscribeLocalEvent<DarkReaperComponent, ReaperMaterializeEvent>(OnMaterializeAction);
        SubscribeLocalEvent<DarkReaperComponent, ReaperStunEvent>(OnStunAction);
        SubscribeLocalEvent<DarkReaperComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<DarkReaperComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
        SubscribeLocalEvent<DarkReaperComponent, DamageModifyEvent>(OnDamageModify);

        SubscribeLocalEvent<DarkReaperComponent, AfterMaterialize>(OnAfterMaterialize);
        SubscribeLocalEvent<DarkReaperComponent, AfterDeMaterialize>(OnAfterDeMaterialize);
        SubscribeLocalEvent<DarkReaperComponent, AfterConsumed>(OnAfterConsumed);
    }

    // Action bindings
    private void OnRoflAction(EntityUid uid, DarkReaperComponent comp, ReaperRoflEvent args)
    {
        args.Handled = true;

        DoRoflAbility(uid, comp);
    }

    private void OnConsumeAction(EntityUid uid, DarkReaperComponent comp, ReaperConsumeEvent args)
    {
        if (!comp.PhysicalForm)
            return;

        // Only consume dead
        if (!_mobState.IsDead(args.Target))
        {
            if (_net.IsClient)
                _popup.PopupEntity("Цель должна быть мертва!", uid, PopupType.MediumCaution);
            return;
        }

        if (!TryComp<HumanoidAppearanceComponent>(args.Target, out _))
        {
            if (_net.IsClient)
                _popup.PopupEntity("Цель должна быть гуманоидом!", uid, PopupType.MediumCaution);
            return;
        }

        var doafterArgs = new DoAfterArgs(
            EntityManager,
            uid,
            TimeSpan.FromSeconds(9 /* Hand-picked value to match the sound */),
            new AfterConsumed(),
            uid,
            args.Target
        )
        {
            Broadcast = false,
            BreakOnDamage = false,
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            NeedHand = false,
            BlockDuplicate = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };

        var started = _doAfter.TryStartDoAfter(doafterArgs);
        if (started)
        {
            comp.ConsoomAudio = _audio.PlayPredicted(comp.ConsumeAbilitySound, uid, uid);
        }
    }

    private void OnMaterializeAction(EntityUid uid, DarkReaperComponent comp, ReaperMaterializeEvent args)
    {
        DoMaterialize(uid, comp);
    }

    private void OnStunAction(EntityUid uid, DarkReaperComponent comp, ReaperStunEvent args)
    {
        if (!comp.PhysicalForm)
            return;

        args.Handled = true;
        DoStunAbility(uid, comp);
    }

    // Actions
    protected virtual void DoStunAbility(EntityUid uid, DarkReaperComponent comp)
    {
        _audio.PlayPredicted(comp.StunAbilitySound, uid, uid);
        comp.StunScreamStart = _timing.CurTime;
        Dirty(uid, comp);
        _appearance.SetData(uid, DarkReaperVisual.StunEffect, true);

        var entities = _lookup.GetEntitiesInRange(uid, comp.StunAbilityRadius);
        foreach (var entity in entities)
        {
            _stun.TryParalyze(entity, comp.StunDuration, true);
        }
    }

    protected virtual void DoRoflAbility(EntityUid uid, DarkReaperComponent comp)
    {
        _audio.PlayPredicted(comp.RolfAbilitySound, uid, uid);
    }

    protected void DoMaterialize(EntityUid uid, DarkReaperComponent comp)
    {
        if (!comp.PhysicalForm)
        {
            var doafterArgs = new DoAfterArgs(
                EntityManager,
                uid,
                TimeSpan.FromSeconds(1.25 /* Hand-picked value to match the sound */),
                new AfterMaterialize(),
                uid
            )
            {
                Broadcast = false,
                BreakOnDamage = false,
                BreakOnTargetMove = false,
                BreakOnUserMove = false,
                NeedHand = false,
                BlockDuplicate = true,
                CancelDuplicate = false
            };

            var started = _doAfter.TryStartDoAfter(doafterArgs);
            if (started)
            {
                _physics.SetBodyType(uid, BodyType.Static);
                _audio.PlayPredicted(comp.PortalOpenSound, uid, uid);
            }
        }
        else
        {
            var doafterArgs = new DoAfterArgs(
                EntityManager,
                uid,
                TimeSpan.FromSeconds(4.14 /* Hand-picked value to match the sound */),
                new AfterDeMaterialize(),
                uid
            )
            {
                Broadcast = false,
                BreakOnDamage = false,
                BreakOnTargetMove = false,
                BreakOnUserMove = false,
                NeedHand = false,
                BlockDuplicate = true,
                CancelDuplicate = false
            };

            var started = _doAfter.TryStartDoAfter(doafterArgs);
            if (started)
            {
                _audio.PlayPredicted(comp.PortalCloseSound, uid, uid);
            }
        }
    }

    protected virtual void OnAfterConsumed(EntityUid uid, DarkReaperComponent comp, AfterConsumed args)
    {
        args.Handled = true;

        if (comp.ConsoomAudio != null)
        {
            comp.ConsoomAudio.Stop();
            comp.ConsoomAudio = null;
        }
    }

    private void OnAfterMaterialize(EntityUid uid, DarkReaperComponent comp, AfterMaterialize args)
    {
        args.Handled = true;

        _physics.SetBodyType(uid, BodyType.KinematicController);

        if (!args.Cancelled)
        {
            ChangeForm(uid, comp, true);
            comp.MaterializedStart = _timing.CurTime;

            var cooldownStart = _timing.CurTime;
            var cooldownEnd = cooldownStart + comp.CooldownAfterMaterialize;

            _actions.SetCooldown(comp.MaterializeActionEntity, cooldownStart, cooldownEnd);

            if (_net.IsServer)
            {
                CreatePortal(uid, comp);
            }
        }
    }

    protected virtual void CreatePortal(EntityUid uid, DarkReaperComponent comp)
    {
        if (_prototype.HasIndex<EntityPrototype>(comp.PortalEffectPrototype))
        {
            var portalEntity = Spawn(comp.PortalEffectPrototype, Transform(uid).Coordinates);
            comp.ActivePortal = portalEntity;
        }
    }

    private void OnAfterDeMaterialize(EntityUid uid, DarkReaperComponent comp, AfterDeMaterialize args)
    {
        args.Handled = true;

        if (!args.Cancelled)
        {
            ChangeForm(uid, comp, false);
            _actions.StartUseDelay(comp.MaterializeActionEntity);
        }
    }

    // Update loop
    public override void Update(float delta)
    {
        base.Update(delta);

        var query = EntityQueryEnumerator<DarkReaperComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (IsPaused(uid))
                continue;

            if (_net.IsServer && _actions.TryGetActionData(comp.MaterializeActionEntity, out var materializeData, false))
            {
                var visibleEyes = materializeData.Cooldown.HasValue &&
                materializeData.Cooldown.Value.End > _timing.CurTime &&
                !comp.PhysicalForm;
                _appearance.SetData(uid, DarkReaperVisual.GhostCooldown, visibleEyes);
            }

            if (comp.StunScreamStart != null)
            {
                if (comp.StunScreamStart.Value + comp.StunGlareLength < _timing.CurTime)
                {
                    comp.StunScreamStart = null;
                    Dirty(uid, comp);
                    _appearance.SetData(uid, DarkReaperVisual.StunEffect, false);
                }
                else
                {
                    _appearance.SetData(uid, DarkReaperVisual.StunEffect, true);
                }
            }

            if (comp.MaterializedStart != null)
            {
                var maxDuration = comp.MaterializeDurations[comp.CurrentStage - 1];
                var diff = comp.MaterializedStart.Value + maxDuration - _timing.CurTime;
                if (diff.TotalSeconds < 4.14 && comp.PlayingPortalAudio == null)
                {
                    comp.PlayingPortalAudio = _audio.PlayPredicted(comp.PortalCloseSound, uid, uid);
                }
                if (diff <= TimeSpan.Zero)
                {
                    ChangeForm(uid, comp, false);
                    _actions.StartUseDelay(comp.MaterializeActionEntity);
                }
            }
            else
            {
                comp.PlayingPortalAudio = null;
            }
        }
    }

    // Crap
    protected virtual void OnCompInit(EntityUid uid, DarkReaperComponent comp, ComponentStartup args)
    {
        UpdateStageAppearance(uid, comp);
        ChangeForm(uid, comp, comp.PhysicalForm);

        _pointLight.SetEnabled(uid, comp.StunScreamStart.HasValue);

        // Make tests crash & burn if stupid things are done
        DebugTools.Assert(comp.MaxStage >= 1, "DarkReaperComponent.MaxStage must always be equal or greater than 1.");
    }

    protected virtual void OnCompShutdown(EntityUid uid, DarkReaperComponent comp, ComponentShutdown args)
    {
    }

    public virtual void ChangeForm(EntityUid uid, DarkReaperComponent comp, bool isMaterial)
    {
        comp.PhysicalForm = isMaterial;

        if (TryComp<FixturesComponent>(uid, out var fixturesComp))
        {
            if (fixturesComp.Fixtures.TryGetValue("fix1", out var fixture))
            {
                var mask = (int) (isMaterial ? CollisionGroup.MobMask : CollisionGroup.GhostImpassable);
                var layer = (int) (isMaterial ? CollisionGroup.MobLayer : CollisionGroup.None);
                _physics.SetCollisionMask(uid, "fix1", fixture, mask);
                _physics.SetCollisionLayer(uid, "fix1", fixture, layer);
            }
        }

        if (TryComp<EyeComponent>(uid, out var eye))
            _eye.SetDrawFov(uid, isMaterial, eye);
        _appearance.SetData(uid, DarkReaperVisual.PhysicalForm, isMaterial);

        if (isMaterial)
        {
            _tag.AddTag(uid, "DoorBumpOpener");
        }
        else
        {
            _tag.RemoveTag(uid, "DoorBumpOpener");
            comp.StunScreamStart = null;
            comp.MaterializedStart = null;
            _appearance.SetData(uid, DarkReaperVisual.StunEffect, false);
        }

        _actions.SetEnabled(comp.StunActionEntity, isMaterial);
        _actions.SetEnabled(comp.ConsumeActionEntity, isMaterial);

        ToggleWeapon(uid, comp, isMaterial);
        UpdateMovementSpeed(uid, comp);

        Dirty(uid, comp);
    }

    public void ChangeStage(EntityUid uid, DarkReaperComponent comp, int stage)
    {
        comp.CurrentStage = stage;
        UpdateStageAppearance(uid, comp);
    }

    public void UpdateStage(EntityUid uid, DarkReaperComponent comp)
    {
        if (!comp.ConsumedPerStage.TryGetValue(comp.CurrentStage - 1, out var nextStageReq))
        {
            return;
        }

        if (comp.Consumed >= nextStageReq)
        {
            comp.Consumed = 0;
            ChangeStage(uid, comp, comp.CurrentStage + 1);
            _audio.PlayPredicted(comp.LevelupSound, uid, uid);
        }
    }

    private void UpdateStageAppearance(EntityUid uid, DarkReaperComponent comp)
    {
        _appearance.SetData(uid, DarkReaperVisual.Stage, comp.CurrentStage);
    }

    // This cursed shit exists because we can't disable components.
    private void ToggleWeapon(EntityUid uid, DarkReaperComponent comp, bool isEnabled)
    {
        if (!_net.IsServer)
            return;

        if (!isEnabled)
        {
            if (TryComp<MeleeWeaponComponent>(uid, out var weapon))
                RemComp(uid, weapon);
        }
        else
        {
            var weapon = EnsureComp<MeleeWeaponComponent>(uid);
            weapon.Hidden = true;
            weapon.Angle = 0;
            weapon.Animation = "WeaponArcClaw";
            weapon.HitSound = comp.HitSound;
            weapon.SwingSound = comp.SwingSound;
        }
    }

    private void OnGetMeleeDamage(EntityUid uid, DarkReaperComponent comp, ref GetMeleeDamageEvent args)
    {
        if (!comp.PhysicalForm || !comp.StageMeleeDamage.TryGetValue(comp.CurrentStage - 1, out var damageSet))
        {
            damageSet = new();
        }

        args.Damage = new()
        {
            DamageDict = damageSet
        };
    }

    private void OnDamageModify(EntityUid uid, DarkReaperComponent comp, DamageModifyEvent args)
    {
        if (!comp.PhysicalForm)
        {
            args.Damage = new();
        }
        else
        {
            if (!comp.StageDamageResists.TryGetValue(comp.CurrentStage, out var resists))
                return;

            args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, resists);
        }
    }

    private void UpdateMovementSpeed(EntityUid uid, DarkReaperComponent comp)
    {
        if (!TryComp<MovementSpeedModifierComponent>(uid, out var modifComp))
            return;

        var speed = comp.PhysicalForm ? comp.MaterialMovementSpeed : comp.UnMaterialMovementSpeed;
        _speedModifier.ChangeBaseSpeed(uid, speed, speed, modifComp.Acceleration, modifComp);
    }

    private void OnMobStateChanged(EntityUid uid, DarkReaperComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        component.ConsoomAudio?.Stop();
        component.PlayingPortalAudio?.Stop();

        if (_net.IsServer)
        {
            QueueDel(component.ActivePortal);

            // play at coordinates because entity is getting deleted
            var coordinates = Transform(uid).Coordinates;
            _audio.Play(component.SoundDeath, Filter.Pvs(coordinates), coordinates, true);

            // Get everthing that was consumed out before deleting
            if (_container.TryGetContainer(uid, DarkReaperComponent.ConsumedContainerId, out var container))
            {
                _container.EmptyContainer(container);
            }

            // Make it blow up on pieces after deth
            EntProtoId[] gibPoolAsArray = component.SpawnOnDeathPool.ToArray();
            var goreAmountToSpawn = component.SpawnOnDeathAmount + component.SpawnOnDeathAdditionalPerStage * (component.CurrentStage - 1);

            var goreSpawnCoords = Transform(uid).Coordinates;
            for (int i = 0; i < goreAmountToSpawn; i++)
            {
                var protoToSpawn = gibPoolAsArray[_random.Next(gibPoolAsArray.Length)];
                var goreEntity = Spawn(protoToSpawn, goreSpawnCoords);

                _transform.SetLocalRotationNoLerp(goreEntity, Angle.FromDegrees(_random.NextDouble(0, 360)));

                var maxAxisImp = component.SpawnOnDeathImpulseStrength;
                var impulseVec = new Vector2(_random.NextFloat(-maxAxisImp, maxAxisImp), _random.NextFloat(-maxAxisImp, maxAxisImp));
                _physics.ApplyLinearImpulse(goreEntity, impulseVec);
            }

            // insallah
            QueueDel(uid);
        }
    }
}

[Serializable, NetSerializable]
public sealed partial class AfterMaterialize : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class AfterDeMaterialize : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class AfterConsumed : DoAfterEvent
{
    public override AfterConsumed Clone() => this;
}
