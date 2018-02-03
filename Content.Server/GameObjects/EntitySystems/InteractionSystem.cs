using Content.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects.System;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.GameObjects.EntitySystems
{
    /// <summary>
    /// This interface gives components behavior when being clicked on or "attacked" by a user with an object in their hand
    /// </summary>
    public interface IAttackby
    {
        bool Attackby(IEntity user, IEntity attackwith);
    }

    /// <summary>
    /// This interface gives components behavior when being clicked on or "attacked" by a user with an empty hand
    /// </summary>
    public interface IAttackHand
    {
        bool Attackhand(IEntity user);
    }

    public interface IUse
    {
        bool UseEntity(IEntity user);
    }

    /// <summary>
    /// Governs interactions during clicking on entities
    /// </summary>
    public class InteractionSystem : EntitySystem
    {
        private const float INTERACTION_RANGE = 2;
        private const float INTERACTION_RANGE_SQUARED = INTERACTION_RANGE * INTERACTION_RANGE;

        public void AddEvent(IClickableComponent click)
        {
            click.OnClick += UserInteraction;
        }

        public void RemoveEvent(IClickableComponent click)
        {
            click.OnClick -= UserInteraction;
        }

        public static void UserInteraction(object sender, ClickEventArgs e)
        {
            if (e.ClickType != SS14.Shared.GameObjects.Clicktype.Left)
                return;

            IEntity user = e.User;
            IEntity attacked = e.Source;

            if (!user.TryGetComponent<IServerTransformComponent>(out var userTransform))
            {
                return;
            }

            var distance = (userTransform.WorldPosition - attacked.GetComponent<IServerTransformComponent>().WorldPosition).LengthSquared;
            if (distance > INTERACTION_RANGE_SQUARED)
            {
                return;
            }

            if (!user.TryGetComponent<IHandsComponent>(out var hands))
            {
                return;
            }

            var item = hands.GetHand(hands.ActiveIndex)?.Owner;

            if (item != null && attacked != item)
            {
                Interaction(user, item, attacked);
            }
            else if(attacked == item)
            {
                UseInteraction(user, item);
            }
            else
            {
                Interaction(user, attacked);
            }
        }

        /// <summary>
        /// Uses a weapon/object on an entity
        /// </summary>
        /// <param name="user"></param>
        /// <param name="weapon"></param>
        /// <param name="attacked"></param>
        public static void Interaction(IEntity user, IEntity weapon, IEntity attacked)
        {
            List<IAttackby> interactables = attacked.GetComponents<IAttackby>().ToList();

            for(var i = 0; i < interactables.Count; i++)
            {
                if (interactables[i].Attackby(user, weapon)) //If an attackby returns a status completion we finish our attack
                {
                    return;
                }
            }

            //Else check damage component to see if we damage if not attackby, and if so can we attack object
        }

        /// <summary>
        /// Uses an empty hand on an entity
        /// </summary>
        /// <param name="user"></param>
        /// <param name="attacked"></param>
        public static void Interaction(IEntity user, IEntity attacked)
        {
            List<IAttackHand> interactables = attacked.GetComponents<IAttackHand>().ToList();

            for (var i = 0; i < interactables.Count; i++)
            {
                if (interactables[i].Attackhand(user)) //If an attackby returns a status completion we finish our attack
                {
                    return;
                }
            }

            //Else check damage component to see if we damage if not attackby, and if so can we attack object
        }

        /// <summary>
        /// Activates/Uses an object in control/possession of a user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="attacked"></param>
        public static void UseInteraction(IEntity user, IEntity used)
        {
            List<IUse> usables = used.GetComponents<IUse>().ToList();

            for (var i = 0; i < usables.Count; i++)
            {
                if (usables[i].UseEntity(user)) //If an attackby returns a status completion we finish our attack
                {
                    return;
                }
            }
        }
    }
}
