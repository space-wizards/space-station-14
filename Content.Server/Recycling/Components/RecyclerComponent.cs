using System.Collections.Generic;
using Content.Server.Act;
using Content.Server.Chat.Managers;
using Content.Server.Conveyor;
using Content.Server.GameTicking;
using Content.Server.Items;
using Content.Server.Notification;
using Content.Server.Players;
using Content.Server.Power.Components;
using Content.Shared.Body.Components;
using Content.Shared.Notification;
using Content.Shared.Notification.Managers;
using Content.Shared.Recycling;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Recycling.Components
{
    // TODO: Add sound and safe beep
    [RegisterComponent]
    public class RecyclerComponent : Component, ISuicideAct
    {
        public override string Name => "Recycler";

        public List<IEntity> Intersecting { get; set; } = new();

        /// <summary>
        ///     Whether or not sentient beings will be recycled
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("safe")]
        internal bool Safe = true;

        /// <summary>
        ///     The percentage of material that will be recovered
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("efficiency")]
        internal float Efficiency = 0.25f;

        internal bool Powered =>
            !Owner.TryGetComponent(out ApcPowerReceiverComponent? receiver) ||
            receiver.Powered;

        private void Clean()
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(RecyclerVisuals.Bloody, false);
            }
        }

        public bool CanRun()
        {
            if (Owner.TryGetComponent(out ApcPowerReceiverComponent? receiver) &&
                !receiver.Powered)
            {
                return false;
            }

            if (Owner.HasComponent<ItemComponent>())
            {
                return false;
            }

            return true;
        }

        public bool CanMove(IEntity entity)
        {
            if (entity == Owner)
            {
                return false;
            }

            if (!entity.TryGetComponent(out IPhysBody? physics) ||
                physics.BodyType == BodyType.Static)
            {
                return false;
            }

            if (entity.HasComponent<ConveyorComponent>())
            {
                return false;
            }

            if (entity.HasComponent<IMapGridComponent>())
            {
                return false;
            }

            if (entity.IsInContainer())
            {
                return false;
            }

            return true;
        }

        SuicideKind ISuicideAct.Suicide(IEntity victim, IChatManager chat)
        {
            var mind = victim.PlayerSession()?.ContentData()?.Mind;

            if (mind != null)
            {
                EntitySystem.Get<GameTicker>().OnGhostAttempt(mind, false);
                mind.OwnedEntity?.PopupMessage(Loc.GetString("recycler-component-suicide-message"));
            }

            victim.PopupMessageOtherClients(Loc.GetString("recycler-component-suicide-message-others", ("victim",victim)));

            if (victim.TryGetComponent<SharedBodyComponent>(out var body))
            {
                body.Gib(true);
            }

            EntitySystem.Get<RecyclerSystem>().Bloodstain(this);

            return SuicideKind.Bloodloss;
        }
    }
}
