using Content.Server.DoAfter;
using Content.Server.MachineLinking.Components;
using Content.Server.Popups;
using Content.Shared.Interaction.Events;
using Content.Shared.MobState;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Server.MachineLinking.System;

/// <summary>
/// Entity system for release signallers, aka dead man's switches.
/// </summary>
public sealed class ReleaseSignallerSystem : EntitySystem
{
    [Dependency] private readonly SignalLinkerSystem _signalSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ReleaseSignallerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ReleaseSignallerComponent, DroppedEvent>(OnDropped);
        SubscribeLocalEvent<ReleaseSignallerComponent, UseInHandEvent>(OnUsedInHand);
        SubscribeLocalEvent<ReleaseSignallerComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<ReleaseSignallerComponent, ReleaseSignallerDisarmCompleteEvent>(OnSuccessfulDisarm);

        SubscribeLocalEvent<ReleaseSignallerHolderComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        foreach (var holder in EntityQuery<ReleaseSignallerHolderComponent>())
        {
            if (holder.Switches.Count == 0)
            {
                RemComp<ReleaseSignallerHolderComponent>(holder.Owner);
            }
        }
    }

    private void OnInit(EntityUid uid, ReleaseSignallerComponent component, ComponentInit args)
    {
        _signalSystem.EnsureTransmitterPorts(uid, component.Port);
    }

    private void OnDropped(EntityUid uid, ReleaseSignallerComponent component, DroppedEvent args)
    {
        if (args.Handled)
            return;

        if (!component.Armed)
            return;

        _signalSystem.InvokePort(uid, component.Port);
        component.Armed = false;
        if(EntityManager.TryGetComponent<ReleaseSignallerHolderComponent>(args.User, out var holderComp))
            holderComp.Switches.Remove(component);
        _popupSystem.PopupEntity(Loc.GetString("release-signaller-release-self", ("device", uid)), args.User, Filter.Entities(args.User), PopupType.MediumCaution);
        _popupSystem.PopupEntity(Loc.GetString("release-signaller-release-other", ("device", uid), ("person", args.User)), args.User, Filter.PvsExcept(args.User), PopupType.MediumCaution);
        args.Handled = true;
    }

    private void OnUsedInHand(EntityUid uid, ReleaseSignallerComponent component, UseInHandEvent args)
    {
        if (component.Armed == false)
        {
            var holder = EnsureComp<ReleaseSignallerHolderComponent>(args.User);
            holder.Switches.Add(component);
            component.Armed = true;
            _popupSystem.PopupEntity(Loc.GetString("release-signaller-armed", ("device", uid)), args.User, Filter.Entities(args.User));
        }
        else
        {
            _signalSystem.InvokePort(uid, component.Port);
            component.Armed = false;
            _popupSystem.PopupEntity(Loc.GetString("release-signaller-release-self", ("device", uid)), args.User, Filter.Entities(args.User), PopupType.MediumCaution);
            _popupSystem.PopupEntity(Loc.GetString("release-signaller-release-other", ("device", uid), ("person", args.User)), args.User, Filter.PvsExcept(args.User), PopupType.MediumCaution);
        }
    }

    private void OnGetVerbs(EntityUid uid, ReleaseSignallerComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (component.Armed)
        {
            args.Verbs.Add(new AlternativeVerb()
            {
                Text = Loc.GetString("verb-release-signaller-disarm"),
                Act = () =>
                {
                    _doAfterSystem.DoAfter(new DoAfterEventArgs(args.User, 15f, target: uid)
                    {
                        BreakOnStun = true,
                        NeedHand = true,
                        TargetFinishedEvent = new ReleaseSignallerDisarmCompleteEvent(args.User)
                    });
                }
            });
        }
    }

    private void OnMobStateChanged(EntityUid uid, ReleaseSignallerHolderComponent component, MobStateChangedEvent args)
    {
        if (args.CurrentMobState == DamageState.Critical || args.CurrentMobState == DamageState.Dead)
        {
            foreach (var switchComponent in component.Switches)
            {
                _signalSystem.InvokePort(switchComponent.Owner, switchComponent.Port);
            }
        }
    }

    private void OnSuccessfulDisarm(EntityUid uid, ReleaseSignallerComponent component, ReleaseSignallerDisarmCompleteEvent args)
    {
        component.Armed = false;
        _popupSystem.PopupEntity(Loc.GetString("release-signaller-disarmed"), args.Disarmer, Filter.Entities(args.Disarmer));
    }

    private sealed class ReleaseSignallerDisarmCompleteEvent : EntityEventArgs
    {
        public EntityUid Disarmer { get; }

        public ReleaseSignallerDisarmCompleteEvent(EntityUid disarmer)
        {
            Disarmer = disarmer;
        }
    }
}
