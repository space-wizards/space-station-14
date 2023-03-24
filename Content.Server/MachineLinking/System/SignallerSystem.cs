using Content.Server.Explosion.EntitySystems;
using Content.Server.MachineLinking.Components;
using Content.Shared.Interaction.Events;
using System.Linq;

namespace Content.Server.MachineLinking.System;

public sealed class SignallerSystem : EntitySystem
{
    [Dependency] private readonly SignalLinkerSystem _signal = default!;

    private HashSet<(EntityUid, String)> _triggering = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SignallerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SignallerComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<SignallerComponent, TriggerEvent>(OnTrigger);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // do any deferred triggers for this tick
        var list = _triggering.ToList();
        _triggering.Clear();
        foreach (var (uid, port) in list)
        {
            _signal.InvokePort(uid, port);
        }
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
        // defer it to the next tick to prevent stack overflow
        _triggering.Add((uid, component.Port));
        args.Handled = true;
    }
}
