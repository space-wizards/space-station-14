#nullable enable
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Conveyor;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameObjects;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Players;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Recycling;
using Content.Shared.Interfaces;
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

namespace Content.Server.GameObjects.Components.Recycling
{
    // TODO: Add sound and safe beep
    [RegisterComponent]
    public class RecyclerComponent : Component, IStartCollide, ISuicideAct
    {
        public override string Name => "Recycler";

        public List<IEntity> Intersecting { get; set; } = new();

        /// <summary>
        ///     Whether or not sentient beings will be recycled
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("safe")]
        private bool _safe = true;

        /// <summary>
        ///     The percentage of material that will be recovered
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("efficiency")]
        private float _efficiency = 0.25f;

        private bool Powered =>
            !Owner.TryGetComponent(out PowerReceiverComponent? receiver) ||
            receiver.Powered;

        private void Bloodstain()
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(RecyclerVisuals.Bloody, true);
            }
        }

        private void Clean()
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(RecyclerVisuals.Bloody, false);
            }
        }

        private bool CanGib(IEntity entity)
        {
            // We suppose this entity has a Recyclable component.
            return entity.HasComponent<IBody>() && !_safe && Powered;
        }

        private void Recycle(IEntity entity)
        {
            if (!Intersecting.Contains(entity))
            {
                Intersecting.Add(entity);
            }

            // TODO: Prevent collision with recycled items

            // Can only recycle things that are recyclable... And also check the safety of the thing to recycle.
            if (!entity.TryGetComponent(out RecyclableComponent? recyclable) || !recyclable.Safe && _safe) return;

            // Mobs are a special case!
            if (CanGib(entity))
            {
                entity.GetComponent<IBody>().Gib(true);
                Bloodstain();
                return;
            }

            recyclable.Recycle(_efficiency);
        }

        public bool CanRun()
        {
            if (Owner.TryGetComponent(out PowerReceiverComponent? receiver) &&
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

        void IStartCollide.CollideWith(Fixture ourFixture, Fixture otherFixture, in Manifold manifold)
        {
            Recycle(otherFixture.Body.Owner);
        }

        SuicideKind ISuicideAct.Suicide(IEntity victim, IChatManager chat)
        {
            var mind = victim.PlayerSession()?.ContentData()?.Mind;

            if (mind != null)
            {
                IoCManager.Resolve<IGameTicker>().OnGhostAttempt(mind, false);
                mind.OwnedEntity?.PopupMessage(Loc.GetString("You recycle yourself!"));
            }

            victim.PopupMessageOtherClients(Loc.GetString("{0:theName} tries to recycle {0:themself}!", victim));

            if (victim.TryGetComponent<IBody>(out var body))
            {
                body.Gib(true);
            }

            Bloodstain();

            return SuicideKind.Bloodloss;
        }
    }
}
