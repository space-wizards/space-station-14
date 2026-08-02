// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Popups;
using Content.Shared.Mindshield.Components;
using Content.Shared.DeadSpace.Demons.Shadowling;
using Content.Shared.Humanoid;
using Robust.Shared.Audio.Systems;

namespace Content.Server.DeadSpace.Demons.Shadowling;

public sealed class ShadowlingMindShieldBreakSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedSubdermalImplantSystem _implants = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShadowlingMindShieldBreakComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ShadowlingMindShieldBreakComponent, ShadowlingMindShieldBreakEvent>(OnMindShieldBreakAction);
        SubscribeLocalEvent<ShadowlingMindShieldBreakComponent, ShadowlingMindShieldBreakDoAfterEvent>(OnDoAfter);
    }

    private void OnComponentInit(EntityUid uid, ShadowlingMindShieldBreakComponent component, ComponentInit args)
    {
        _actions.AddAction(uid, ref component.ActionMindShieldBreakEntity, component.ActionMindShieldBreak);
    }

    private void OnMindShieldBreakAction(EntityUid uid, ShadowlingMindShieldBreakComponent component, ShadowlingMindShieldBreakEvent args)
    {
        if (args.Handled) return;

        var target = args.Target;

        if (!HasComp<HumanoidAppearanceComponent>(target))
            return;

        if (HasComp<ShadowlingComponent>(target) ||
            HasComp<ShadowlingRevealComponent>(target) ||
            HasComp<ShadowlingSlaveComponent>(target))
            return;

        if (!HasComp<MindShieldComponent>(target))
        {
            _popup.PopupEntity("Разум цели не защищён имплантом!", uid, uid, PopupType.Medium);
            return;
        }

        args.Handled = true;
        _popup.PopupEntity("Вы начинаете разрушать имплант защиты разума...", uid, uid, PopupType.Medium);
        var doAfterArgs = new DoAfterArgs(EntityManager, uid, component.Duration, new ShadowlingMindShieldBreakDoAfterEvent(), uid, target: target)
        {
            BreakOnMove = true,
            NeedHand = false,
            BreakOnDamage = true,
            DistanceThreshold = 2f
        };
        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnDoAfter(EntityUid uid, ShadowlingMindShieldBreakComponent component, ShadowlingMindShieldBreakDoAfterEvent args)
    {
        var target = args.Args.Target ?? args.Target;
        if (args.Cancelled || target == null) return;

        var targetUid = target.Value;

        if (!HasComp<MindShieldComponent>(targetUid))
        {
            _popup.PopupEntity("Разум цели больше не защищён имплантом!", uid, uid, PopupType.Medium);
            return;
        }

        if (TryComp<ImplantedComponent>(targetUid, out var implanted))
        {
            foreach (var implant in implanted.ImplantContainer.ContainedEntities)
            {
                if (!HasComp<MindShieldImplantComponent>(implant))
                    continue;

                _implants.ForceRemove((targetUid, implanted), implant);
                break;
            }
        }

        RemComp<MindShieldComponent>(targetUid);
        _audio.PlayPvs(component.BreakSound, uid);
        _popup.PopupEntity("Вы разрушили имплант защиты разума!", uid, uid, PopupType.Large);
        _popup.PopupEntity("Ваш имплант защиты разума разрушен!", targetUid, targetUid, PopupType.LargeCaution);
    }
}
