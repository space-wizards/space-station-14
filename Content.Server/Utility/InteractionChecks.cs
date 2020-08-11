using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Physics;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;

namespace Content.Server.Utility
{
    /// <summary>
    /// Convenient methods for checking for various conditions commonly needed
    /// for interactions.
    /// </summary>
    public static class InteractionChecks
    {

        /// <summary>
        /// Default interaction check for targeted attack interaction types.
        /// Same as <see cref="SharedInteractionSystem.InRangeUnobstructed"/>, but defaults to ignore inside blockers
        /// (making the check less restrictive).
        /// Validates that attacker is in range of the attacked entity. Additionally shows a popup if
        /// validation fails.
        /// </summary>
        public static bool InRangeUnobstructed(ITargetedInteractEventArgs eventArgs, bool ignoreInsideBlocker = true)
        {
            if (!EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(eventArgs.User.Transform.MapPosition,
                eventArgs.Target.Transform.MapPosition, ignoredEnt: eventArgs.Target, ignoreInsideBlocker: ignoreInsideBlocker))
            {
                var localizationManager = IoCManager.Resolve<ILocalizationManager>();
                eventArgs.Target.PopupMessage(eventArgs.User, localizationManager.GetString("You can't reach there!"));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Default interaction check for Drag drop interaction types.
        /// Same as <see cref="SharedInteractionSystem.InRangeUnobstructed"/>, but defaults to ignore inside blockers
        /// (making the check less restrictive) and checks reachability of both the target and the dragged / dropped object.
        /// Additionally shows a popup if validation fails.
        /// </summary>
        public static bool InRangeUnobstructed(DragDropEventArgs eventArgs, bool ignoreInsideBlocker = true)
        {
            if (!EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(eventArgs.User.Transform.MapPosition,
                eventArgs.Target.Transform.MapPosition, ignoredEnt: eventArgs.Target, ignoreInsideBlocker: ignoreInsideBlocker))
            {
                var localizationManager = IoCManager.Resolve<ILocalizationManager>();
                eventArgs.Target.PopupMessage(eventArgs.User, localizationManager.GetString("You can't reach there!"));
                return false;
            }
            if (!EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(eventArgs.User.Transform.MapPosition,
                eventArgs.Dropped.Transform.MapPosition, ignoredEnt: eventArgs.Dropped, ignoreInsideBlocker: ignoreInsideBlocker))
            {
                var localizationManager = IoCManager.Resolve<ILocalizationManager>();
                eventArgs.Dropped.PopupMessage(eventArgs.User, localizationManager.GetString("You can't reach there!"));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Default interaction check for after attack interaction types.
        /// Same as <see cref="SharedInteractionSystem.InRangeUnobstructed"/>, but defaults to ignore inside blockers
        /// (making the check less restrictive).
        /// Validates that attacker is in range of the attacked entity, if there is such an entity.
        /// If there is no attacked entity, validates that they are in range of the clicked position.
        /// Additionally shows a popup if validation fails.
        /// </summary>
        public static bool InRangeUnobstructed(AfterInteractEventArgs eventArgs, bool ignoreInsideBlocker = true)
        {
            if (eventArgs.Target != null)
            {
                if (!EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(eventArgs.User.Transform.MapPosition,
                    eventArgs.Target.Transform.MapPosition, ignoredEnt: eventArgs.Target, ignoreInsideBlocker: ignoreInsideBlocker))
                {
                    var localizationManager = IoCManager.Resolve<ILocalizationManager>();
                    eventArgs.Target.PopupMessage(eventArgs.User, localizationManager.GetString("You can't reach there!"));
                    return false;
                }
            }
            else
            {
                if (!EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(eventArgs.User.Transform.MapPosition,
                    eventArgs.ClickLocation.ToMap(IoCManager.Resolve<IMapManager>()), ignoredEnt: eventArgs.User, ignoreInsideBlocker: ignoreInsideBlocker))
                {
                    var localizationManager = IoCManager.Resolve<ILocalizationManager>();
                    eventArgs.User.PopupMessage(eventArgs.User, localizationManager.GetString("You can't reach there!"));
                    return false;
                }
            }


            return true;
        }

        /// <summary>
        /// Convenient static alternative to <see cref="SharedInteractionSystem.InRangeUnobstructed"/>, which also
        /// shows a popup message if not in range.
        /// </summary>
        public static bool InRangeUnobstructed(IEntity user, MapCoordinates otherCoords,
            float range = SharedInteractionSystem.InteractionRange,
            int collisionMask = (int) CollisionGroup.Impassable, IEntity ignoredEnt = null, bool ignoreInsideBlocker = false)
        {
            var mapManager = IoCManager.Resolve<IMapManager>();
            var interactionSystem = EntitySystem.Get<SharedInteractionSystem>();
            if (!interactionSystem.InRangeUnobstructed(user.Transform.MapPosition, otherCoords, range, collisionMask,
                ignoredEnt, ignoreInsideBlocker))
            {
                var localizationManager = IoCManager.Resolve<ILocalizationManager>();
                user.PopupMessage(user, localizationManager.GetString("You can't reach there!"));

                return false;
            }

            return true;
        }
    }
}
