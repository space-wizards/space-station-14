using Content.Server.Administration.Logs;
using Content.Server.DeviceLinking.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Interaction.Events;
using Content.Shared.Timing;

namespace Content.Server.DeviceLinking.Systems;

[InjectDependencies]
public sealed partial class SignallerSystem : EntitySystem
{
    [Dependency] private DeviceLinkSystem _link = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SignallerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SignallerComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<SignallerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnInit(EntityUid uid, SignallerComponent component, ComponentInit args)
    {
        _link.EnsureSourcePorts(uid, component.Port);
    }

    private void OnUseInHand(EntityUid uid, SignallerComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.User):actor} triggered signaler {ToPrettyString(uid):tool}");
        _link.InvokePort(uid, component.Port);
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

        _link.InvokePort(uid, component.Port);
        args.Handled = true;
    }
}
