using System;
using Content.Server.Gravity.EntitySystems;
using Content.Shared.Alert;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Players;

namespace Content.Server.Alert
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

        protected override void OnRemove()
        {
            if (EntitySystem.TryGet<WeightlessSystem>(out var weightlessSystem))
            {
                weightlessSystem.RemoveAlert(this);
            }
            else
            {
                Logger.WarningS("alert", $"{nameof(WeightlessSystem)} not found");
            }

            base.OnRemove();
        }

        [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession? session = null)
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

                    if (!IsShowingAlert(msg.Type))
                    {
                        Logger.DebugS("alert", "user {0} attempted to" +
                                              " click alert {1} which is not currently showing for them",
                            player.Name, msg.Type);
                        break;
                    }

                    if (!AlertManager.TryGet(msg.Type, out var alert))
                    {
                        Logger.WarningS("alert", "unrecognized encoded alert {0}", msg.Type);
                        break;
                    }

                    alert.OnClick?.AlertClicked(new ClickAlertEventArgs(player, alert));
                    break;
                }
            }
        }
    }
}
