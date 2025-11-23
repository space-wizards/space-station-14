using Content.Server.Administration.Logs;
using Content.Server.DeviceLinking.Components;
using Content.Shared.Database;
using Content.Shared.Interaction.Events;
using Content.Shared.Timing;
using Content.Shared.DeadmanSwitch;

namespace Content.Server.DeviceLinking.Systems;

public sealed class SignallerSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _link = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SignallerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SignallerComponent, UseInHandEvent>(OnUseInHand, after: [typeof(SharedDeadmanSwitchSystem)]);
    }

    private void OnInit(EntityUid uid, SignallerComponent component, ComponentInit args)
    {
        _link.EnsureSourcePorts(uid, component.Port);
    }

    /// <summary>
    /// Trigger a signaler device, causing it to send out its signal.
    /// </summary>
    /// <param name="ent">The signaler entity.</param>
    /// <param name="user">The user.</param>
    public void Trigger(Entity<SignallerComponent?> ent, EntityUid? user)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (user != null)
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{user} triggered signaler {ToPrettyString(ent):tool}");

        _link.InvokePort(ent, ent.Comp.Port);
    }

    private void OnUseInHand(EntityUid uid, SignallerComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        Trigger(uid, args.User);
        args.Handled = true;
    }
}
