using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Robust.Shared.GameObjects.Systems;
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
        /// that attacker is in range of the target. Additionally shows a popup if
        /// validation fails.
        /// </summary>
        public static bool InRangeUnobstructed(ITargetedAttack eventArgs, bool insideBlockerValid = false)
        {
            if (!EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(eventArgs.User.Transform.MapPosition,
                eventArgs.Target.Transform.WorldPosition, ignoredEnt: eventArgs.Target, insideBlockerValid: insideBlockerValid))
            {
                var localizationManager = IoCManager.Resolve<ILocalizationManager>();
                eventArgs.Target.PopupMessage(eventArgs.User, localizationManager.GetString("You can't reach there!"));
                return false;
            }

            return true;
        }


    }
}
