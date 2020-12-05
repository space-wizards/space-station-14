using System;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Alert;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Log;
using Robust.Shared.Players;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedAlertsComponent))]
    public sealed class ServerAlertsComponent : SharedAlertsComponent
    {

        protected override void Startup()
        {
            base.Startup();

            if (EntitySystem.TryGet<WeightlessSystem>(out var weightlessSystem))
            {
                weightlessSystem.AddAlert(this);
            }
            else
            {
                Logger.WarningS("alert", "weightlesssystem not found");
            }
        }

        public override void OnRemove()
        {
            if (EntitySystem.TryGet<WeightlessSystem>(out var weightlessSystem))
            {
                weightlessSystem.RemoveAlert(this);
            }
            else
            {
                Logger.WarningS("alert", "weightlesssystem not found");
            }

            base.OnRemove();
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            switch (message)
            {
                case ClickAlertMessage msg:
                {
                    var player = session.AttachedEntity;

                    if (player != Owner)
                    {
                        break;
                    }

                    if (!IsShowingAlert(msg.AlertType))
                    {
                        Logger.DebugS("alert", "user {0} attempted to" +
                                              " click alert {1} which is not currently showing for them",
                            player.Name, msg.AlertType);
                        break;
                    }

                    if (AlertManager.TryGet(msg.AlertType, out var alert))
                    {
                        alert.OnClick.AlertClicked(new ClickAlertEventArgs(player, alert));
                    }
                    else
                    {
                        Logger.WarningS("alert", "unrecognized encoded alert {0}", msg.AlertType);
                    }

                    break;
                }
            }
        }
    }
}
