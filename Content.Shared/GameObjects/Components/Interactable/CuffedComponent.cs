using System;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Containers;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Timers;

namespace Content.Shared.GameObjects.Components.Interactable
{
    [RegisterComponent]
    public class CuffedComponent : Component, IActionBlocker
    {
#pragma warning disable 649
        [Dependency] private readonly ISharedNotifyManager _notifyManager;
#pragma warning restore 649
        public override string Name => "Cuffed";

        public string HandcuffId = "Handcuffs";

        public float uncuffTime = 10.0f;
        public float breakoutTime = 60.0f;

        private IContainer cuffContainer;

        private float interactRange;

        #region ActionBlockers
        bool IActionBlocker.CanInteract() => false;

        bool IActionBlocker.CanUse() => false;

        bool IActionBlocker.CanPickup() => false;

        bool IActionBlocker.CanDrop() => false;

        bool IActionBlocker.CanAttack() => false;

        bool IActionBlocker.CanEquip() => false;

        bool IActionBlocker.CanUnequip() => false;

        #endregion

        public CuffedComponent()
        {
            //IoCManager.InjectDependencies(this);
            interactRange = SharedInteractionSystem.InteractionRange / 2;
        }

        public void TryUncuff(IEntity user, bool isOwner, bool force = false)
        {
            if (!force)
            {
                if (!isOwner && !EntitySystem.Get<SharedInteractionSystem>()
                        .InRangeUnobstructed(user.Transform.MapPosition,
                            Owner.Transform.MapPosition, interactRange, ignoredEnt: Owner))
                {
                    _notifyManager.PopupMessage(user, user,
                        "You are too far away to remove the cuffs.");
                    return;
                }

                if (isOwner)
                {
                    Timer.Spawn(TimeSpan.FromSeconds(breakoutTime), () => Uncuff(user, true));
                }
                else
                {
                    Timer.Spawn(TimeSpan.FromSeconds(uncuffTime), () => Uncuff(user, false));
                }

                return;
            }

            Uncuff(user, isOwner);
        }

        public void Uncuff(IEntity user, bool isOwner)
        {
            if (!isOwner && !EntitySystem.Get<SharedInteractionSystem>()
                .InRangeUnobstructed(user.Transform.MapPosition,
                    Owner.Transform.MapPosition, interactRange, ignoredEnt: Owner))
            {
                _notifyManager.PopupMessage(user, user,
                    "You are too far away to remove the cuffs.");
                return;
            }

            Owner.EntityManager.SpawnEntity(HandcuffId, Owner.Transform.GridPosition);
            Owner.RemoveComponent<CuffedComponent>();

        }


        /// <summary>
        ///     Allows the uncuffing of a cuffed person. Used by other people and by the player themself to break out of cuffs.
        /// </summary>
        [Verb]
        private sealed class UncuffVerb : Verb<CuffedComponent>
        {
            protected override void GetData(IEntity user, CuffedComponent component, VerbData data)
            {
                if (user != component.Owner && !ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Uncuff");
            }

            protected override void Activate(IEntity user, CuffedComponent component)
            {
                bool isOwner = user == component.Owner;
                component.TryUncuff(user, isOwner);
            }
        }
    }
}
