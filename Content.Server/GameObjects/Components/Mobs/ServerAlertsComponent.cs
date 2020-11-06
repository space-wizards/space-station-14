using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.GameObjects.Components.Buckle;
using Content.Server.GameObjects.Components.Movement;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Pulling;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Alert;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Players;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedAlertsComponent))]
    public sealed class ServerAlertsComponent : SharedAlertsComponent
    {

        protected override void Startup()
        {
            base.Startup();

            EntitySystem.Get<WeightlessSystem>().AddStatus(this);
        }

        public override void OnRemove()
        {
            EntitySystem.Get<WeightlessSystem>().RemoveStatus(this);

            base.OnRemove();
        }

        public override ComponentState GetComponentState()
        {
            return new AlertsComponentState(Alerts);
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

                    // TODO: Implement clicking other status effects in the HUD
                    switch (msg.Effect)
                    {
                        case AlertSlot.Buckled:
                            if (!player.TryGetComponent(out BuckleComponent buckle))
                                break;

                            buckle.TryUnbuckle(player);
                            break;
                        case AlertSlot.Piloting:
                            if (!player.TryGetComponent(out ShuttleControllerComponent controller))
                                break;

                            controller.RemoveController();
                            break;
                        case AlertSlot.Pulling:
                            EntitySystem
                                .Get<SharedPullingSystem>()
                                .GetPulled(player)?
                                .GetComponentOrNull<SharedPullableComponent>()?
                                .TryStopPull();

                            break;
                        case AlertSlot.Fire:
                            if (!player.TryGetComponent(out FlammableComponent flammable))
                                break;

                            flammable.Resist();
                            break;
                        default:
                            player.PopupMessage(msg.Effect.ToString());
                            break;
                    }

                    break;
                }
            }
        }
    }

}
