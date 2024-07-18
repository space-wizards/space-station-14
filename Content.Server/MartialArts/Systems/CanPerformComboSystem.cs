using Content.Server.Administration.Logs;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Tag;
using Content.Shared.MartialArts;
using Robust.Shared.Timing;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.StatusEffect;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage;
using Content.Shared.Stunnable;
using Content.Shared.Bed.Sleep;
using Content.Shared.MartialArts.Systems;
using System.Numerics;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Movement.Pulling.Components;
using Content.Server.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Components;
using Robust.Shared.Random;
using Content.Server.Hands.Systems;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Server.MartialArts;

public sealed class ComboSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly GrabThrownSystem _grabThrowing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly HandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CanPerformComboComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<CanPerformComboComponent, ComboAttackPerformedEvent>(OnAttackPerformed);

        // Granting martial arts
        SubscribeLocalEvent<GrantCQCComponent, UseInHandEvent>(OnGrantCQCUse);
        SubscribeLocalEvent<GrantCQCComponent, ExaminedEvent>(OnGrantCQCExamine);

        // Here comes CQC!
        SubscribeLocalEvent<CQCKnowledgeComponent, MapInitEvent>(OnCQCMapInit);
        SubscribeLocalEvent<CQCKnowledgeComponent, ComponentShutdown>(OnCQCShutdown);
        SubscribeLocalEvent<CQCKnowledgeComponent, CheckGrabOverridesEvent>(CheckGrabStageOverride);
        SubscribeLocalEvent<CQCKnowledgeComponent, ComboAttackPerformedEvent>(OnCQCAttackPerformed);

        SubscribeLocalEvent<CanPerformComboComponent, CQCSlamPerformedEvent>(OnCQCSlam);
        SubscribeLocalEvent<CanPerformComboComponent, CQCKickPerformedEvent>(OnCQCKick);
        SubscribeLocalEvent<CanPerformComboComponent, CQCRestrainPerformedEvent>(OnCQCRestrain);
        SubscribeLocalEvent<CanPerformComboComponent, CQCPressurePerformedEvent>(OnCQCPressure);
        SubscribeLocalEvent<CanPerformComboComponent, CQCConsecutivePerformedEvent>(OnCQCConsecutive);
    }
    private void OnMapInit(EntityUid uid, CanPerformComboComponent component, MapInitEvent args)
    {
        foreach (var item in component.RoundstartCombos)
        {
            component.AllowedCombos.Add(_proto.Index<ComboPrototype>(item));
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CanPerformComboComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime >= comp.ResetTime && comp.LastAttacks.Count > 0)
                comp.LastAttacks.Clear();
        }
    }

    private void OnAttackPerformed(EntityUid uid, CanPerformComboComponent component, ComboAttackPerformedEvent args)
    {
        if (!HasComp<MobStateComponent>(args.Target))
            return;

        if (component.CurrentTarget != null && args.Target != component.CurrentTarget.Value)
        {
            component.LastAttacks.Clear();
        }
        if (args.Weapon != uid)
        {
            component.LastAttacks.Clear();
            return;
        }
        component.CurrentTarget = args.Target;
        component.ResetTime = _timing.CurTime + TimeSpan.FromSeconds(4);
        component.LastAttacks.Add(args.Type);
        CheckCombo(uid, args.Target, component);
    }

    private void CheckCombo(EntityUid uid, EntityUid target, CanPerformComboComponent comp)
    {
        var success = false;

        foreach (var proto in comp.AllowedCombos)
        {
            if (success)
                break;

            var sum = comp.LastAttacks.Count - proto.AttackTypes.Count;
            if (sum < 0)
                continue;

            var list = comp.LastAttacks.GetRange(sum, proto.AttackTypes.Count).AsEnumerable();
            var attackList = proto.AttackTypes.AsEnumerable();
            
            if (list.SequenceEqual(attackList) && proto.ResultEvent != null)
            {
                var ev = proto.ResultEvent;
                RaiseLocalEvent(uid, ev);
                comp.LastAttacks.Clear();
            }
        }
    }

    private void CheckGrabStageOverride<T>(EntityUid uid, T component, CheckGrabOverridesEvent args) where T : GrabStagesOverrideComponent
    {
        if (args.Stage == GrabStage.Soft)
            args.Stage = component.StartingStage;
    }

    #region Granting
    private void OnGrantCQCUse(EntityUid uid, GrantCQCComponent comp, UseInHandEvent args)
    {
        if (comp.Used)
        {
            _popupSystem.PopupEntity(Loc.GetString("cqc-fail-used", ("manual", Identity.Entity(uid, EntityManager))), args.User, args.User);
            return;
        }

        if (HasComp<CanPerformComboComponent>(args.User))
        {
            if (!TryComp<CQCKnowledgeComponent>(args.User, out var cqc))
            {
                _popupSystem.PopupEntity(Loc.GetString("cqc-success-knowanother"), args.User, args.User);
                return;
            }
            else if (cqc.Blocked)
            {
                _popupSystem.PopupEntity(Loc.GetString("cqc-success-unblocked"), args.User, args.User);
                cqc.Blocked = false;
                comp.Used = true;
                return;
            }
            else
            {
                _popupSystem.PopupEntity(Loc.GetString("cqc-fail-already"), args.User, args.User);
                return;
            }
        }
        else
        {
            _popupSystem.PopupEntity(Loc.GetString("cqc-success-learned"), args.User, args.User);
            var cqc = EnsureComp<CQCKnowledgeComponent>(args.User);
            cqc.Blocked = false;
            comp.Used = true;
        }
    }

    private void OnGrantCQCExamine(EntityUid uid, GrantCQCComponent comp, ExaminedEvent args)
    {
        if (comp.Used)
            args.PushMarkup(Loc.GetString("cqc-manual-used", ("manual", Identity.Entity(uid, EntityManager))));
    }
    #endregion

    #region CQC
    private void OnCQCMapInit(EntityUid uid, CQCKnowledgeComponent component, MapInitEvent args)
    {
        var combo = EnsureComp<CanPerformComboComponent>(uid);
        foreach (var item in component.RoundstartCombos)
        {
            combo.AllowedCombos.Add(_proto.Index<ComboPrototype>(item));
        }
    }

    private void OnCQCShutdown(EntityUid uid, CQCKnowledgeComponent component, ComponentShutdown args)
    {
        var combo = EnsureComp<CanPerformComboComponent>(uid);
        foreach (var item in component.RoundstartCombos)
        {
            combo.AllowedCombos.Remove(_proto.Index<ComboPrototype>(item));
        }
    }

    private void OnCQCAttackPerformed(EntityUid uid, CQCKnowledgeComponent component, ComboAttackPerformedEvent args)
    {
        if (!CheckCanUseCQC(uid))
            return;

        if (args.Type == ComboAttackType.Disarm)
        {
            if (_random.Prob(0.5f))
            {
                var item = _hands.GetActiveItem(args.Target);

                if (item != null)
                {
                    if (!HasComp<MeleeWeaponComponent>(item.Value) && !HasComp<GunComponent>(item.Value))
                        return;
                    _hands.TryDrop(args.Target, item.Value);
                    _hands.TryPickupAnyHand(uid, item.Value);
                    _stamina.TakeStaminaDamage(args.Target, 10f);
                }
            }
        }
    }
    private void OnCQCSlam(EntityUid uid, CanPerformComboComponent component, CQCSlamPerformedEvent args)
    {
        if (component.CurrentTarget == null)
            return;

        if (!CheckCanUseCQC(uid))
            return;

        var target = component.CurrentTarget.Value;

        if (TryComp<RequireProjectileTargetComponent>(target, out var downed) && downed.Active)
            return;

        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Blunt", 10);
        _damageable.TryChangeDamage(target, damage, origin: uid);
        _stun.TryParalyze(target, TimeSpan.FromSeconds(12), true);
        if (TryComp<PullableComponent>(target, out var pullable))
            _pulling.TryStopPull(target, pullable, uid, true);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit3.ogg"), target);
    }

    private void OnCQCKick(EntityUid uid, CanPerformComboComponent component, CQCKickPerformedEvent args)
    {
        if (component.CurrentTarget == null)
            return;

        if (!CheckCanUseCQC(uid))
            return;

        var target = component.CurrentTarget.Value;

        if (!TryComp<RequireProjectileTargetComponent>(target, out var downed) || !downed.Active)
            return;

        if (TryComp<StaminaComponent>(target, out var stamina) && stamina.Critical)
        {
            _status.TryAddStatusEffect<ForcedSleepingComponent>(target, "ForcedSleep", TimeSpan.FromSeconds(10), true);
        }

        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Blunt", 10);
        _damageable.TryChangeDamage(target, damage, origin: uid);
        _stamina.TakeStaminaDamage(target, 55f, source: uid);

        var mapPos = _transform.GetMapCoordinates(uid).Position;
        var hitPos = _transform.GetMapCoordinates(target).Position;
        Vector2 dir = hitPos - mapPos;
        dir *= 1f / dir.Length();
        if (TryComp<PullableComponent>(target, out var pullable))
            _pulling.TryStopPull(target, pullable, uid, true);
        _grabThrowing.Throw(target, dir);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit2.ogg"), target);
    }

    private void OnCQCRestrain(EntityUid uid, CanPerformComboComponent component, CQCRestrainPerformedEvent args)
    {
        if (component.CurrentTarget == null)
            return;

        if (!CheckCanUseCQC(uid))
            return;

        var target = component.CurrentTarget.Value;

        _stun.TryParalyze(target, TimeSpan.FromSeconds(10), true);
        _stamina.TakeStaminaDamage(target, 30f, source: uid);
    }

    private void OnCQCPressure(EntityUid uid, CanPerformComboComponent component, CQCPressurePerformedEvent args)
    {
        if (component.CurrentTarget == null)
            return;

        if (!CheckCanUseCQC(uid))
            return;

        var target = component.CurrentTarget.Value;

        _stamina.TakeStaminaDamage(target, 65f, source: uid);
    }

    private void OnCQCConsecutive(EntityUid uid, CanPerformComboComponent component, CQCConsecutivePerformedEvent args)
    {
        if (component.CurrentTarget == null)
            return;

        if (!CheckCanUseCQC(uid))
            return;

        var target = component.CurrentTarget.Value;

        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Blunt", 25);
        _damageable.TryChangeDamage(target, damage, origin: uid);
        _stamina.TakeStaminaDamage(target, 55f, source: uid);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit1.ogg"), target);
    }

    private bool CheckCanUseCQC(EntityUid uid)
    {
        if (TryComp<CQCKnowledgeComponent>(uid, out var cqc) && !cqc.Blocked)
            return true;

        foreach (var ent in _lookup.GetEntitiesInRange(uid, 8f))
        {
            if (TryPrototype(ent, out var proto) && proto.ID == "DefaultStationBeaconKitchen")
                return true;
        }
        return false;
    }
    #endregion
}
