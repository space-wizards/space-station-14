using Content.Server.Explosion.EntitySystems;
using Content.Server.MachineLinking.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Timing;

namespace Content.Server.MachineLinking.System;

public sealed class SignallerSystem : EntitySystem
{
    [Dependency] private readonly SignalLinkerSystem _signal = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SignallerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SignallerComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<SignallerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnInit(EntityUid uid, SignallerComponent component, ComponentInit args)
    {
        _signal.EnsureTransmitterPorts(uid, component.Port);
    }

    private void OnUseInHand(EntityUid uid, SignallerComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;
        _signal.InvokePort(uid, component.Port);
        args.Handled = true;
    }

    private void OnTrigger(EntityUid uid, SignallerComponent component, TriggerEvent args)
    {
        // if on cooldown, do nothing
        var hasUseDelay = TryComp<UseDelayComponent>(uid, out var useDelay);
        if (hasUseDelay && _useDelay.ActiveDelay(uid, useDelay))
            return;

        // set cooldown to prevent clocks
        if (hasUseDelay)
            _useDelay.BeginDelay(uid, useDelay);

        _signal.InvokePort(uid, component.Port);
        args.Handled = true;
    }
}
