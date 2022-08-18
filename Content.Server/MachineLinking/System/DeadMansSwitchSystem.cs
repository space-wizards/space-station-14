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
/// Entity system for dead man's switches.
/// </summary>
public sealed class DeadMansSwitchSystem : EntitySystem
{
    [Dependency] private readonly SignalLinkerSystem _signalSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DeadMansSwitchComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<DeadMansSwitchComponent, DroppedEvent>(OnDropped);
        SubscribeLocalEvent<DeadMansSwitchComponent, UseInHandEvent>(OnUsedInHand);
        SubscribeLocalEvent<DeadMansSwitchComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<DeadMansSwitchComponent, DeadMansSwitchDisarmCompleteEvent>(OnSuccessfulDisarm);

        SubscribeLocalEvent<DeadMansSwitchHolderComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        foreach (var holder in EntityQuery<DeadMansSwitchHolderComponent>())
        {
            if (holder.Switches.Count == 0)
            {
                RemComp<DeadMansSwitchHolderComponent>(holder.Owner);
            }
        }
    }

    private void OnInit(EntityUid uid, DeadMansSwitchComponent component, ComponentInit args)
    {
        _signalSystem.EnsureTransmitterPorts(uid, component.Port);
    }

    private void OnDropped(EntityUid uid, DeadMansSwitchComponent component, DroppedEvent args)
    {
        if (args.Handled)
            return;

        if (!component.Armed)
            return;

        _signalSystem.InvokePort(uid, component.Port);
        component.Armed = false;
        if(EntityManager.TryGetComponent<DeadMansSwitchHolderComponent>(args.User, out var holderComp))
            holderComp.Switches.Remove(component);
        _popupSystem.PopupEntity(Loc.GetString("dead-mans-switch-let-go"), args.User, Filter.Entities(args.User), PopupType.MediumCaution);
        args.Handled = true;
    }

    private void OnUsedInHand(EntityUid uid, DeadMansSwitchComponent component, UseInHandEvent args)
    {
        if (component.Armed == false)
        {
            var holder = EnsureComp<DeadMansSwitchHolderComponent>(args.User);
            holder.Switches.Add(component);
            component.Armed = true;
            _popupSystem.PopupEntity(Loc.GetString("dead-mans-switch-armed"), args.User, Filter.Entities(args.User));
        }
        else
        {
            _signalSystem.InvokePort(uid, component.Port);
            component.Armed = false;
            _popupSystem.PopupEntity(Loc.GetString("dead-mans-switch-let-go"), args.User, Filter.Entities(args.User), PopupType.MediumCaution);
        }
    }

    private void OnGetVerbs(EntityUid uid, DeadMansSwitchComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (component.Armed)
        {
            args.Verbs.Add(new AlternativeVerb()
            {
                Text = Loc.GetString("verb-dead-mans-switch-disarm"),
                Act = () =>
                {
                    _doAfterSystem.DoAfter(new DoAfterEventArgs(args.User, 15f, target: uid)
                    {
                        BreakOnStun = true,
                        NeedHand = true,
                        TargetFinishedEvent = new DeadMansSwitchDisarmCompleteEvent(args.User)
                    });
                }
            });
        }
    }

    private void OnMobStateChanged(EntityUid uid, DeadMansSwitchHolderComponent component, MobStateChangedEvent args)
    {
        if (args.CurrentMobState == DamageState.Critical || args.CurrentMobState == DamageState.Dead)
        {
            foreach (var switchComponent in component.Switches)
            {
                _signalSystem.InvokePort(switchComponent.Owner, switchComponent.Port);
            }
        }
    }

    private void OnSuccessfulDisarm(EntityUid uid, DeadMansSwitchComponent component, DeadMansSwitchDisarmCompleteEvent args)
    {
        component.Armed = false;
        _popupSystem.PopupEntity(Loc.GetString("dead-mans-switch-disarmed"), args.Disarmer, Filter.Entities(args.Disarmer));
    }

    private sealed class DeadMansSwitchDisarmCompleteEvent : EntityEventArgs
    {
        public EntityUid Disarmer { get; }

        public DeadMansSwitchDisarmCompleteEvent(EntityUid disarmer)
        {
            Disarmer = disarmer;
        }
    }
}
