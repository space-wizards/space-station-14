using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceLinking.Systems;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Popups;
using Content.Shared.DeadmanSwitch;
using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.Interaction.Events;
using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.Toggleable;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;

namespace Content.Server.DeadmanSwitch;

/// <summary>
/// System for deadman's switch behavior.
/// Handles OnUseInHand event, preventing the signaller from being triggered the normal way.
/// Instead, using it in hand arms / disarms it, and it will then trigger if dropped while armed.
/// </summary>

public sealed class DeadmanSwitchSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SignallerSystem _signal = default!;
    [Dependency] private readonly WirelessNetworkSystem _wirelessNetwork = default!;
    [Dependency] private readonly SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    
    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<DeadmanSwitchComponent, DroppedEvent>(OnDropped);
        SubscribeLocalEvent<DeadmanSwitchComponent, UseInHandEvent>(OnUseInHand, before: [typeof(SignallerSystem)]);
        SubscribeLocalEvent<DeadmanSwitchComponent, DeadmanSwitchDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<DeadmanSwitchComponent, ExaminedEvent>(OnExamined);
    }

    private void ToggleArmed(EntityUid uid, EntityUid user, DeadmanSwitchComponent component)
    {
        component.Armed = !component.Armed;
        _appearance.SetData(uid, ToggleableVisuals.Enabled, component.Armed);
        _audio.PlayPvs(component.SwitchSound, uid);
    }
    
    private void OnDropped(EntityUid uid, DeadmanSwitchComponent component, DroppedEvent args)
    {
        if (!component.Armed)
            return;
        
        if (!TryComp<SignallerComponent>(uid, out var signaller))
            return;
        
        ToggleArmed(uid, args.User, component);
        _popup.PopupEntity(Loc.GetString("deadman-on-trigger", ("name", uid)), uid, args.User);

        // Immediately trigger linked timers on networked entities within InstantTriggerRange
        var linkedDevices = _deviceLink.GetLinkedSinks(uid, signaller.Port);
        if (linkedDevices.Count == 0)
            return;

        var switchXform = Transform(uid);
        var switchMapID = switchXform.MapID;
        var switchPos = _transformSystem.GetWorldPosition(switchXform);
        
        foreach (var linkedUid in linkedDevices)
        {
            if (!_wirelessNetwork.CheckRange(linkedUid, switchMapID, switchPos, component.InstantTriggerRange))
                continue;
            
            if (!TryComp<TimerTriggerComponent>(linkedUid, out var timerTrigger))
                continue;
            
            if (!TryComp<TriggerOnSignalComponent>(linkedUid, out var signalTrigger))
                continue;
            
            if (signalTrigger.KeyOut == null || !timerTrigger.KeysIn.Contains(signalTrigger.KeyOut))
                continue;

            // Manually call the trigger that would fire when the timer completes
            _trigger.Trigger(linkedUid, user: args.User, timerTrigger.KeyOut);
            // Block the _signal.Trigger event from starting a new countdown
            timerTrigger.Disabled = true;
            
            _adminLogger.Add(LogType.Trigger,
                $"{ToPrettyString(args.User):user} instant-triggered {ToPrettyString(linkedUid):target} with {ToPrettyString(uid):device}");
        }

        // Activate signaller the normal way
        _signal.Trigger(uid, args.User, signaller);
    }
    
    private void OnUseInHand(EntityUid uid, DeadmanSwitchComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, component.ArmDelay, new DeadmanSwitchDoAfterEvent(), uid, target: uid);
        _doAfter.TryStartDoAfter(doAfterArgs);
        
        args.Handled = true;
    }

    private void OnDoAfter(EntityUid uid, DeadmanSwitchComponent component, DeadmanSwitchDoAfterEvent args)
    {
    if (args.Cancelled)
        return;
    
    ToggleArmed(uid, args.User, component);
    _popup.PopupEntity(Loc.GetString(component.Armed ? "deadman-on-activate" : "deadman-on-deactivate", ("name", uid)), uid, args.User);
    }

    private void OnExamined(EntityUid uid, DeadmanSwitchComponent component, ExaminedEvent args)
    {
        if (component.Armed)
        {
            args.PushMarkup(Loc.GetString("deadman-examine-armed"));
        }
        else
        {
            args.PushMarkup(Loc.GetString("deadman-examine-disarmed"));
        }
}
}