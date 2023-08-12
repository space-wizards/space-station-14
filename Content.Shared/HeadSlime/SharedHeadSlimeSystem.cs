using Content.Shared.Movement.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Robust.Shared.Containers;
using Content.Shared.Actions;
using Content.Shared.Popups;
using Robust.Shared.Serialization;
using Content.Shared.Mobs.Systems;
using Content.Shared.Humanoid;

namespace Content.Shared.HeadSlime;

public abstract class SharedHeadSlimeSystem : EntitySystem
{
    /// <inheritdoc/>
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeadSlimeComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);

        SubscribeLocalEvent<HeadSlimeComponent, HeadSlimeInfectEvent>(OnHeadSlimeInfectAction);
        SubscribeLocalEvent<HeadSlimeComponent, HeadSlimeInjectEvent>(OnHeadSlimeInjectAction);
    }

    private void OnRefreshSpeed(EntityUid uid, HeadSlimeComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if(!component.HeadSlimeQueen)
        {
            var mod = component.HeadSlimeMovementSpeedDebuff;
            args.ModifySpeed(mod, mod);
        }
    }

    private void OnHeadSlimeInfectAction(EntityUid uid, HeadSlimeComponent component, HeadSlimeInfectEvent args)
    {       
        if (args.Handled)
            return;

        args.Handled = true;
        var target = args.Target;

        if (HasComp<HeadSlimeComponent>(target))
            return;

        if (!HasComp<HumanoidAppearanceComponent>(target))
        {
            _popupSystem.PopupEntity(Loc.GetString("Head-Slime-action-popup-message-fail-target-animal"), uid, uid);
            return;
        }

        if (TryComp(target, out MobStateComponent? targetState))
        {
            if (targetState.CurrentState == MobState.Dead)
            {
                _popupSystem.PopupEntity(Loc.GetString("Head-Slime-action-popup-message-fail-target-dead"), uid, uid);
                return;
            }
            
            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(uid, TimeSpan.FromSeconds(component.InfectTime), new HeadSlimeInfectDoAfterEvent(), uid, target: target, used: uid)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
            });
        }
    }    
    
    private void OnHeadSlimeInjectAction(EntityUid uid, HeadSlimeComponent component, HeadSlimeInjectEvent args)
    {
        if (args.Handled)
            return;


        args.Handled = true;
        var target = args.Target;

        if (HasComp<HeadSlimeComponent>(target))
            return;

        if (TryComp(target, out MobStateComponent? targetState))
        {
            if (targetState.CurrentState == MobState.Dead)
            {
                _popupSystem.PopupEntity(Loc.GetString("Head-Slime-action-popup-message-fail-target-dead"), uid, uid);
                return;
            }

            _popupSystem.PopupEntity(Loc.GetString("hypospray-component-feel-prick-message"), target, target);

            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(uid, TimeSpan.FromSeconds(component.InjectTime), new HeadSlimeInjectDoAfterEvent(), uid, target: target, used: uid)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
            });
        }
    }
}

[Serializable, NetSerializable]
public sealed class HeadSlimeInfectDoAfterEvent : SimpleDoAfterEvent { }
[Serializable, NetSerializable]
public sealed class HeadSlimeInjectDoAfterEvent : SimpleDoAfterEvent { }