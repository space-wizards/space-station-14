// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mindshield.Components;
using Content.Server.Mind;
using Content.Shared.DeadSpace.Demons.Shadowling;
using Content.Shared.Stunnable;
using Content.Server.Roles;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Robust.Shared.Audio;
using Content.Server.Antag;
using Content.Shared.Radio.Components;
using Content.Server.DeadSpace.Components.NightVision;
using Content.Server.DeadSpace.Races;
using Content.Server.Chat.Systems;

namespace Content.Server.DeadSpace.Demons.Shadowling;

public sealed class ShadowlingRecruitSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    private const string ShadowlingChannel = "Shadowling";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShadowlingRecruitComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ShadowlingRecruitComponent, ShadowlingRecruitEvent>(OnRecruitAction);
        SubscribeLocalEvent<ShadowlingRecruitComponent, ShadowlingRecruitDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<ShadowlingRecruitComponent, MobStateChangedEvent>(OnMasterStateChanged);
        SubscribeLocalEvent<ShadowlingRecruitComponent, ComponentShutdown>(OnMasterShutdown);
        SubscribeLocalEvent<ShadowlingSlaveComponent, MobStateChangedEvent>(OnSlaveStateChanged);
        SubscribeLocalEvent<MindShieldComponent, ComponentInit>(OnMindShieldImplanted);
        SubscribeLocalEvent<ShadowlingSlaveComponent, ComponentShutdown>(OnSlaveRemoved);
        SubscribeLocalEvent<ShadowlingSlaveComponent, ComponentStartup>(OnSlaveStartup);
        SubscribeLocalEvent<ShadowlingScreechComponent, ComponentStartup>(OnAbilityStartup);
        SubscribeLocalEvent<ShadowlingFreezingVeinsComponent, ComponentStartup>(OnAbilityStartup);
        SubscribeLocalEvent<ShadowlingBlackMedComponent, ComponentStartup>(OnAbilityStartup);
        SubscribeLocalEvent<ShadowlingAscendanceComponent, ComponentStartup>(OnAbilityStartup);
        SubscribeLocalEvent<ShadowlingMindShieldBreakComponent, ComponentStartup>(OnAbilityStartup);
        SubscribeLocalEvent<ShadowlingRecruitComponent, ComponentStartup>(OnMasterStartup);
    }

    private void OnAbilityStartup(EntityUid uid, IComponent component, ComponentStartup args)
    {
        if (TryComp<ShadowlingRecruitComponent>(uid, out var recruit))
            UpdateSlaveCount(uid, recruit);
    }

    private void OnComponentInit(EntityUid uid, ShadowlingRecruitComponent component, ComponentInit args)
    {
        _actions.AddAction(uid, ref component.ActionRecruitEntity, component.ActionRecruit);
        UpdateSlaveCount(uid, component);
    }

    private void OnMasterStartup(EntityUid uid, ShadowlingRecruitComponent component, ComponentStartup args)
    {
        GiveShadowlingRadio(uid);
    }

    private void OnSlaveStartup(EntityUid uid, ShadowlingSlaveComponent component, ComponentStartup args)
    {
        UpdateAllRecruiters();
        GiveShadowlingRadio(uid);

        if (HasComp<FelinidComponent>(uid))
            return;

        if (!TryComp<NightVisionComponent>(uid, out _))
        {
            var nightVision = EnsureComp<NightVisionComponent>(uid);
            nightVision.Color = new Color(0xDC, 0x14, 0x3C, 0x11);
            nightVision.Animation = true;
            nightVision.IsNightVision = false;
        }
    }

    private void OnMasterStateChanged(EntityUid uid, ShadowlingRecruitComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead) return;
        ReleaseAllSlaves(uid);
    }

    private void OnMasterShutdown(EntityUid uid, ShadowlingRecruitComponent component, ComponentShutdown args)
    {
        ReleaseAllSlaves(uid);
    }

    private void ReleaseAllSlaves(EntityUid uid)
    {
        var query = EntityQueryEnumerator<ShadowlingSlaveComponent>();
        while (query.MoveNext(out var sUid, out var slave))
        {
            if (slave.Master != uid) continue;

            if (!TerminatingOrDeleted(sUid))
            {
                _stun.TryUpdateParalyzeDuration(sUid, TimeSpan.FromSeconds(10));
                _popup.PopupEntity("Связь с хозяином разорвана, тьма отступает!", sUid, sUid, PopupType.LargeCaution);
            }

            if (_mind.TryGetMind(sUid, out var mindId, out _))
                _role.MindRemoveRole(mindId, "MindRoleShadowlingSlave");

            if (!HasComp<FelinidComponent>(sUid) && !TerminatingOrDeleted(sUid))
                RemCompDeferred<NightVisionComponent>(sUid);

            RemCompDeferred<ShadowlingSlaveComponent>(sUid);
        }
    }

    private void OnSlaveRemoved(EntityUid uid, ShadowlingSlaveComponent component, ComponentShutdown args)
    {
        UpdateAllRecruiters();
        RemoveShadowlingRadio(uid);

        if (!HasComp<FelinidComponent>(uid) && !TerminatingOrDeleted(uid))
            RemCompDeferred<NightVisionComponent>(uid);
    }

    private void GiveShadowlingRadio(EntityUid uid)
    {
        var transmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(uid);
        if (!transmitter.Channels.Contains(ShadowlingChannel))
            transmitter.Channels.Add(ShadowlingChannel);

        EnsureComp<IntrinsicRadioReceiverComponent>(uid);

        var active = EnsureComp<ActiveRadioComponent>(uid);
        if (!active.Channels.Contains(ShadowlingChannel))
        {
            active.Channels.Add(ShadowlingChannel);
            Dirty(uid, active);
        }
    }

    private void RemoveShadowlingRadio(EntityUid uid)
    {
        if (TryComp<IntrinsicRadioTransmitterComponent>(uid, out var transmitter))
        {
            transmitter.Channels.Remove(ShadowlingChannel);
            if (transmitter.Channels.Count == 0) RemCompDeferred<IntrinsicRadioTransmitterComponent>(uid);
        }

        if (TryComp<ActiveRadioComponent>(uid, out var active))
        {
            active.Channels.Remove(ShadowlingChannel);
            if (active.Channels.Count == 0) RemCompDeferred<ActiveRadioComponent>(uid);
            else Dirty(uid, active);
        }

        RemCompDeferred<IntrinsicRadioReceiverComponent>(uid);
    }

    private void OnSlaveStateChanged(EntityUid uid, ShadowlingSlaveComponent component, MobStateChangedEvent args)
    {
        UpdateAllRecruiters();
    }

    private void OnRecruitAction(EntityUid uid, ShadowlingRecruitComponent component, ShadowlingRecruitEvent args)
    {
        if (args.Handled) return;
        var target = args.Target;
        var meta = MetaData(target);

        if (HasComp<ShadowlingRecruitComponent>(target) || HasComp<ShadowlingRevealComponent>(target))
        {
            _popup.PopupEntity("Вы не можете поработить другого тенеморфа!", uid, uid, PopupType.Medium);
            return;
        }
        if (meta.EntityPrototype?.ID == component.ImmunePrototypeId)
        {
            _popup.PopupEntity(Loc.GetString("Его воля сопротивляется!"), uid, uid, PopupType.Medium);
            return;
        }
        if (HasComp<ShadowlingSlaveComponent>(target))
        {
            _popup.PopupEntity("Разум этой цели уже принадлежит тьме!", uid, uid, PopupType.Medium);
            return;
        }
        if (HasComp<MindShieldComponent>(target))
        {
            _popup.PopupEntity("Разум цели защищён имплантом!", uid, uid, PopupType.Medium);
            return;
        }
        if (_mobState.IsDead(target) || _mobState.IsCritical(target))
        {
            _popup.PopupEntity("Цель должна быть в сознании!", uid, uid, PopupType.Medium);
            return;
        }
        if (!HasComp<HumanoidAppearanceComponent>(target))
        {
            return;
        }
        if (!_mind.TryGetMind(target, out _, out _))
        {
            _popup.PopupEntity("Это существо не обладает разумом!", uid, uid);
            return;
        }

        args.Handled = true;
        _popup.PopupEntity("Вы шепчете ужасающие истины в разум жертвы...", uid, uid, PopupType.Medium);
        var doAfterArgs = new DoAfterArgs(EntityManager, uid, component.Duration, new ShadowlingRecruitDoAfterEvent(), uid, target: target)
        {
            BreakOnMove = true,
            NeedHand = false,
            BreakOnDamage = true,
            DistanceThreshold = 2f
        };
        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnDoAfter(EntityUid uid, ShadowlingRecruitComponent component, ShadowlingRecruitDoAfterEvent args)
    {
        var target = args.Args.Target ?? args.Target;
        if (args.Cancelled || target == null) return;

        var targetUid = target.Value;

        if (HasComp<ShadowlingRecruitComponent>(targetUid) || HasComp<ShadowlingRevealComponent>(targetUid))
        {
            _popup.PopupEntity("Вы не можете поработить другого тенеморфа!", uid, uid, PopupType.Medium);
            return;
        }

        var meta = MetaData(targetUid);
        if (meta.EntityPrototype?.ID == component.ImmunePrototypeId)
        {
            _popup.PopupEntity(Loc.GetString("Его воля сопротивляется!"), uid, uid, PopupType.Medium);
            return;
        }

        if (HasComp<ShadowlingSlaveComponent>(targetUid))
        {
            _popup.PopupEntity("Разум этой цели уже принадлежит тьме!", uid, uid, PopupType.Medium);
            return;
        }

        if (HasComp<MindShieldComponent>(targetUid))
        {
            _popup.PopupEntity("Разум цели защищён имплантом!", uid, uid, PopupType.Medium);
            return;
        }

        if (_mobState.IsDead(targetUid) || _mobState.IsCritical(targetUid))
        {
            _popup.PopupEntity("Цель должна быть в сознании!", uid, uid, PopupType.Medium);
            return;
        }

        if (!HasComp<HumanoidAppearanceComponent>(targetUid))
        {
            return;
        }

        if (!_mind.TryGetMind(targetUid, out _, out _))
        {
            _popup.PopupEntity("Это существо не обладает разумом!", uid, uid);
            return;
        }

        var slave = EnsureComp<ShadowlingSlaveComponent>(targetUid);
        slave.Master = uid;
        component.TotalRecruited++;

        if (_mind.TryGetMind(uid, out var masterMindId, out var masterMind) &&
            _role.MindHasRole<ShadowlingRoleComponent>(masterMindId, out var shadowlingRole))
        {
            shadowlingRole.Value.Comp2.TotalRecruited++;
        }

        if (_mind.TryGetMind(targetUid, out var mindId, out var mind))
        {
            _role.MindAddRole(mindId, "MindRoleShadowlingSlave", mind);
            var sound = new SoundCollectionSpecifier("ShadowlingRecruitBriefing");
            _antag.SendBriefing(targetUid, Loc.GetString("roles-antag-shadowlingslave-objective"), Color.Red, sound);
        }

        UpdateSlaveCount(uid, component);
    }

    private void OnMindShieldImplanted(EntityUid uid, MindShieldComponent comp, ComponentInit args)
    {
        if (HasComp<ShadowlingSlaveComponent>(uid))
        {
            _stun.TryUpdateParalyzeDuration(uid, TimeSpan.FromSeconds(10));
            if (_mind.TryGetMind(uid, out var mindId, out _))
                _role.MindRemoveRole(mindId, "MindRoleShadowlingSlave");

            if (!HasComp<FelinidComponent>(uid) && !TerminatingOrDeleted(uid))
                RemComp<NightVisionComponent>(uid);

            RemComp<ShadowlingSlaveComponent>(uid);
            UpdateAllRecruiters();
        }
    }

    public void UpdateAllRecruiters()
    {
        var query = EntityQueryEnumerator<ShadowlingRecruitComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            UpdateSlaveCount(uid, comp);
        }
    }

    public void UpdateSlaveCount(EntityUid uid, ShadowlingRecruitComponent component)
    {
        var count = 0;
        var query = EntityQueryEnumerator<ShadowlingSlaveComponent>();
        while (query.MoveNext(out var sUid, out var slave))
        {
            if (slave.Master == uid && _mobState.IsAlive(sUid)) count++;
        }
        component.CurrentSlaves = count;

        var ruleQuery = EntityQueryEnumerator<ShadowlingRuleComponent>();
        var alertThreshold = 15;
        while (ruleQuery.MoveNext(out var ruleComp))
        {
            alertThreshold = ruleComp.AlertThreshold;
        }

        if (count >= alertThreshold)
        {
            var alertRuleQuery = EntityQueryEnumerator<ShadowlingRuleComponent>();
            while (alertRuleQuery.MoveNext(out var alertRuleComp))
            {
                if (!alertRuleComp.AlertAnnounced)
                {
                    alertRuleComp.AlertAnnounced = true;
                    var message = Loc.GetString("shadowling-alert-announcement");
                    var sender = Loc.GetString("shadowling-alert-sender");
                    _chat.DispatchGlobalAnnouncement(message, sender,
                        colorOverride: Color.FromHex("#aa0000"),
                        announcementSound: new SoundCollectionSpecifier("ShadowlingAnnouncement"));
                }
                break;
            }
        }

        bool isAscended = HasComp<ShadowlingAnnihilationComponent>(uid);

        if (TryComp<ShadowlingAscendanceComponent>(uid, out var asc))
        {
            if (count >= asc.RequiredSlaves && !isAscended)
            {
                if (asc.ActionAscendanceEntity == null)
                    _actions.AddAction(uid, ref asc.ActionAscendanceEntity, asc.ActionAscendance);
            }
            else if (asc.ActionAscendanceEntity != null)
            {
                _actions.RemoveAction(uid, asc.ActionAscendanceEntity);
                asc.ActionAscendanceEntity = null;
            }
        }

        if (TryComp<ShadowlingScreechComponent>(uid, out var screech))
        {
            if (isAscended || count >= screech.RequiredSlaves)
            {
                if (screech.ActionScreechEntity == null)
                    _actions.AddAction(uid, ref screech.ActionScreechEntity, screech.ActionScreech);
            }
            else if (screech.ActionScreechEntity != null)
            {
                _actions.RemoveAction(uid, screech.ActionScreechEntity);
                screech.ActionScreechEntity = null;
            }
        }

        if (TryComp<ShadowlingFreezingVeinsComponent>(uid, out var veins))
        {
            if (!isAscended && count >= veins.RequiredSlaves)
            {
                if (veins.ActionFreezingVeinsEntity == null)
                    _actions.AddAction(uid, ref veins.ActionFreezingVeinsEntity, veins.ActionFreezingVeins);
            }
            else if (veins.ActionFreezingVeinsEntity != null)
            {
                _actions.RemoveAction(uid, veins.ActionFreezingVeinsEntity);
                veins.ActionFreezingVeinsEntity = null;
            }
        }

        if (TryComp<ShadowlingBlackMedComponent>(uid, out var med))
        {
            if (isAscended || count >= med.RequiredSlaves)
            {
                if (med.ActionBlackMedEntity == null)
                    _actions.AddAction(uid, ref med.ActionBlackMedEntity, med.ActionBlackMed);
            }
            else if (med.ActionBlackMedEntity != null)
            {
                _actions.RemoveAction(uid, med.ActionBlackMedEntity);
                med.ActionBlackMedEntity = null;
            }
        }

        if (TryComp<ShadowlingMindShieldBreakComponent>(uid, out var mindShieldBreak))
        {
            if (isAscended || count >= mindShieldBreak.RequiredSlaves)
            {
                if (mindShieldBreak.ActionMindShieldBreakEntity == null)
                    _actions.AddAction(uid, ref mindShieldBreak.ActionMindShieldBreakEntity, mindShieldBreak.ActionMindShieldBreak);
            }
            else if (mindShieldBreak.ActionMindShieldBreakEntity != null)
            {
                _actions.RemoveAction(uid, mindShieldBreak.ActionMindShieldBreakEntity);
                mindShieldBreak.ActionMindShieldBreakEntity = null;
            }
        }
    }
}