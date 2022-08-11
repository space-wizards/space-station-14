using Content.Server.DoAfter;
using Content.Server.MachineLinking.Components;
using Content.Server.Popups;
using Content.Shared.Interaction.Events;
using Content.Shared.MobState;
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

        SubscribeLocalEvent<DeadMansSwitchHolderComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnInit(EntityUid uid, DeadMansSwitchComponent component, ComponentInit args)
    {
        _signalSystem.EnsureTransmitterPorts(uid, component.Port);
    }

    private void OnDropped(EntityUid uid, DeadMansSwitchComponent component, DroppedEvent args)
    {
        if (args.Handled)
            return;

        _signalSystem.InvokePort(uid, component.Port);
        _popupSystem.PopupEntity(Loc.GetString("dead-mans-switch-let-go"), args.User, Filter.Entities(args.User));
        args.Handled = true;
    }

    private void OnUsedInHand(EntityUid uid, DeadMansSwitchComponent component, UseInHandEvent args)
    {
        if (component.Armed == false)
        {
            var holder = EnsureComp<DeadMansSwitchHolderComponent>(args.User);
            holder.Switches.Add(component);
        }
        else
        {
            _signalSystem.InvokePort(uid, component.Port);
            _popupSystem.PopupEntity(Loc.GetString("dead-mans-switch-let-go"), args.User, Filter.Entities(args.User));
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
                    _doAfterSystem.DoAfter(new DoAfterEventArgs(args.User, 15f)
                    {
                        BreakOnStun = true,
                        NeedHand = false,
                        UserFinishedEvent = new DeadMansSwitchDisarmCompleteEvent(component)
                    });
                }
            });
        }
    }

    private void OnMobStateChanged(EntityUid uid, DeadMansSwitchHolderComponent component, MobStateChangedEvent args)
    {
        if (args.CurrentMobState == DamageState.Critical || args.CurrentMobState == DamageState.Dead)
        {
            foreach (var sw in component.Switches)
            {
                _signalSystem.InvokePort(sw.Owner, sw.Port);
            }
        }
    }

    private sealed class DeadMansSwitchDisarmCompleteEvent : EntityEventArgs
    {
        public DeadMansSwitchComponent Switch { get; }

        public DeadMansSwitchDisarmCompleteEvent(DeadMansSwitchComponent switchComponent)
        {
            Switch = switchComponent;
        }
    }
}
