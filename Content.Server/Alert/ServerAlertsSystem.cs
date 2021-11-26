using Content.Server.Gravity.EntitySystems;
using Content.Shared.Alert;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Log;

namespace Content.Server.Alert;

[UsedImplicitly]
internal class ServerAlertsSystem : SharedAlertsSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AlertsComponent, ComponentStartup>(HandleComponentStartup);
        SubscribeLocalEvent<AlertsComponent, ComponentRemove>(HandleComponentRemove);

        SubscribeLocalEvent<AlertsComponent, ComponentGetState>(ClientAlertsGetState);
        SubscribeNetworkEvent<ClickAlertEvent>(HandleClickAlert);
    }

    private static void ClientAlertsGetState(EntityUid uid, AlertsComponent component, ref ComponentGetState args)
    {
        args.State = new AlertsComponentState(component.Alerts);
    }

    private void HandleClickAlert(ClickAlertEvent msg, EntitySessionEventArgs args)
    {
        var player = args.SenderSession.AttachedEntity;
        if (player is null || !EntityManager.TryGetComponent<AlertsComponent>(player, out var alertComp)) return;

        if (!IsShowingAlert(player.Value, msg.Type))
        {
            Logger.DebugS("alert", "user {0} attempted to" +
                                   " click alert {1} which is not currently showing for them",
                EntityManager.GetComponent<MetaDataComponent>(player.Value).EntityName, msg.Type);
            return;
        }

        if (!TryGet(msg.Type, out var alert))
        {
            Logger.WarningS("alert", "unrecognized encoded alert {0}", msg.Type);
            return;
        }

        alert.OnClick?.AlertClicked(new ClickAlertEventArgs(player.Value, alert));
    }

    private static void HandleComponentStartup(EntityUid uid, AlertsComponent component, ComponentStartup args)
    {
        if (TryGet<WeightlessSystem>(out var weightlessSystem))
            weightlessSystem.AddAlert(component);
        else
            Logger.WarningS("alert", "weightlesssystem not found");
    }

    private static void HandleComponentRemove(EntityUid uid, AlertsComponent component, ComponentRemove args)
    {
        if (TryGet<WeightlessSystem>(out var weightlessSystem))
            weightlessSystem.RemoveAlert(component);
        else
            Logger.WarningS("alert", $"{nameof(WeightlessSystem)} not found");
    }
}
