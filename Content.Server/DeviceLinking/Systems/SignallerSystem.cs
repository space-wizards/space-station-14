using Content.Server.Administration.Logs;
using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceLinking.Events;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Database;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Robust.Shared.Map;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Robust.Shared.Toolshed.Commands.Math;

namespace Content.Server.DeviceLinking.Systems;

public sealed class SignallerSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _link = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SignallerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SignallerComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<SignallerComponent, TriggerEvent>(OnTrigger);
        SubscribeLocalEvent<SignallerComponent, SignalFailedEvent>(OnSignalFailed);
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
        if (!TryComp(uid, out UseDelayComponent? useDelay)
            // if on cooldown, do nothing
            // and set cooldown to prevent clocks
            || !_useDelay.TryResetDelay((uid, useDelay), true))
            return;

        _link.InvokePort(uid, component.Port);
        args.Handled = true;
    }

    private void OnSignalFailed(Entity<SignallerComponent> ent, ref SignalFailedEvent args) // called by WirelessNetworkSystem
    {
        if (args.failed) // makes a popup if the signaller is out of range
        {
            _popup.PopupEntity(Loc.GetString("signaller-interact-out-of-range-text"), ent.Owner, _transform.GetParentUid(ent.Owner), PopupType.SmallCaution);
        }
        else // diff popup if it's in-range
        {
            _popup.PopupEntity(Loc.GetString("signaller-interact-in-range-text"), ent.Owner, _transform.GetParentUid(ent.Owner));
        }
    }
}
