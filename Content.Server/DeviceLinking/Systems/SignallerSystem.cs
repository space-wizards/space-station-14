using Content.Server.Administration.Logs;
using Content.Server.DeviceLinking.Components;
using Content.Shared.Database;
using Content.Shared.Interaction.Events;
using Content.Shared.Timing;

namespace Content.Server.DeviceLinking.Systems;

public sealed class SignallerSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _link = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SignallerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SignallerComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnInit(EntityUid uid, SignallerComponent component, ComponentInit args)
    {
        _link.EnsureSourcePorts(uid, component.Port);
    }
    
    public void Trigger(EntityUid uid, EntityUid user, SignallerComponent component)
    {
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user):actor} triggered signaler {ToPrettyString(uid):tool}");
        _link.InvokePort(uid, component.Port);
    }

    private void OnUseInHand(EntityUid uid, SignallerComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        Trigger(uid, args.User, component);
        args.Handled = true;
    }
}
