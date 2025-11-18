using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Popups;
using Content.Shared.DeadmanSwitch;
using Content.Shared.Interaction.Events;
using Content.Shared.Toggleable;
using Content.Shared.DoAfter;
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
    [Dependency] private readonly SignallerSystem _signal = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<DeadmanSwitchComponent, DroppedEvent>(OnDropped);
        SubscribeLocalEvent<DeadmanSwitchComponent, UseInHandEvent>(OnUseInHand, before: [typeof(SignallerSystem)]);
        SubscribeLocalEvent<DeadmanSwitchComponent, DeadmanSwitchDoAfterEvent>(OnDoAfter);
    }

    private void ToggleArmed(EntityUid uid, EntityUid user, DeadmanSwitchComponent component)
    {
        component.Armed = !component.Armed;
        _appearance.SetData(uid, ToggleableVisuals.Enabled, component.Armed);
        _popup.PopupEntity(Loc.GetString(component.Armed ? "deadman-on-activate" : "deadman-on-deactivate"), uid, user);
    }
    
    private void OnDropped(EntityUid uid, DeadmanSwitchComponent component, DroppedEvent args)
    {
        if (!component.Armed)
            return;
        
        if (!TryComp<SignallerComponent>(uid, out var signaller))
            return;
        
        ToggleArmed(uid, args.User, component);
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
    }
}