using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DeviceLinking.Components;
using Content.Shared.Interaction.Events;

namespace Content.Shared.DeviceLinking.Systems;

public sealed partial class SignallerSystem : EntitySystem
{
    [Dependency] private DeviceLinkSystem _link = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;

    [SubscribeLocalEvent]
    private void OnInit(Entity<SignallerComponent> ent, ref ComponentInit args)
    {
        _link.EnsureSourcePort(ent.Owner, ent.Comp.Port);
    }

    [SubscribeLocalEvent]
    private void OnUseInHand(Entity<SignallerComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.User):actor} triggered signaler {ToPrettyString(ent.Owner):tool}");
        _link.InvokePort(ent.Owner, ent.Comp.Port);
        args.Handled = true;
    }
}
