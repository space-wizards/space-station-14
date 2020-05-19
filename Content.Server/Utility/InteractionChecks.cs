using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Utility
{
    /// <summary>
    /// Convenient methods for checking for various conditions commonly needed
    /// for interactions.
    /// </summary>
    public static class InteractionChecks
    {

        /// <summary>
        /// Same as <see cref="SharedInteractionSystem.InRangeUnobstructed"/>, validating
        /// that attacker is in range of the attacked entity. Additionally shows a popup if
        /// validation fails.
        /// </summary>
        public static bool InRangeUnobstructed(ITargetedAttackEventArgs eventArgs, bool insideBlockerValid = false)
        {
            if (!EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(eventArgs.User.Transform.MapPosition,
                eventArgs.Attacked.Transform.WorldPosition, ignoredEnt: eventArgs.Attacked, insideBlockerValid: insideBlockerValid))
            {
                var localizationManager = IoCManager.Resolve<ILocalizationManager>();
                eventArgs.Attacked.PopupMessage(eventArgs.User, localizationManager.GetString("You can't reach there!"));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Same as <see cref="SharedInteractionSystem.InRangeUnobstructed"/>, validating
        /// that attacker is in range of the attacked entity, if there is such an entity. If there is no attacked entity, validates
        /// that they are in range of the clicked position. Additionally shows a popup if
        /// validation fails.
        /// </summary>
        public static bool InRangeUnobstructed(AfterAttackEventArgs eventArgs, bool insideBlockerValid = false)
        {
            if (eventArgs.Attacked != null)
            {
                if (!EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(eventArgs.User.Transform.MapPosition,
                    eventArgs.Attacked.Transform.WorldPosition, ignoredEnt: eventArgs.Attacked, insideBlockerValid: insideBlockerValid))
                {
                    var localizationManager = IoCManager.Resolve<ILocalizationManager>();
                    eventArgs.Attacked.PopupMessage(eventArgs.User, localizationManager.GetString("You can't reach there!"));
                    return false;
                }
            }
            else
            {
                var mapManager = IoCManager.Resolve<IMapManager>();
                if (!EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(eventArgs.User.Transform.MapPosition,
                    eventArgs.ClickLocation.ToMapPos(mapManager), ignoredEnt: eventArgs.User, insideBlockerValid: insideBlockerValid))
                {
                    var localizationManager = IoCManager.Resolve<ILocalizationManager>();
                    eventArgs.User.PopupMessage(eventArgs.User, localizationManager.GetString("You can't reach there!"));
                    return false;
                }
            }


            return true;
        }


    }
}
