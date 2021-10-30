using Content.Shared.Popups;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Popups
{
    public static class PopupExtensions
    {
        /// <summary>
        ///     Pops up a message for every player around <see cref="source"/> to see,
        ///     except for <see cref="source"/> itself.
        /// </summary>
        /// <param name="source">The entity on which to popup the message.</param>
        /// <param name="message">The message to show.</param>
        /// <param name="playerManager">
        ///     The instance of player manager to use, will be resolved automatically
        ///     if null.
        /// </param>
        /// <param name="range">
        ///     The range in which to search for players, defaulting to one screen.
        /// </param>
        public static void PopupMessageOtherClients(this IEntity source, string message, IPlayerManager? playerManager = null, int range = 15)
        {
            playerManager ??= IoCManager.Resolve<IPlayerManager>();

            var viewers = playerManager.GetPlayersInRange(source.Transform.Coordinates, range);

            foreach (var viewer in viewers)
            {
                var viewerEntity = viewer.AttachedEntity;

                if (viewerEntity == null || source == viewerEntity || viewer.AttachedEntity == null)
                {
                    continue;
                }

                source.PopupMessage(viewer.AttachedEntity, message);
            }
        }

        /// <summary>
        ///     Pops up a message at the given entity's location for everyone,
        ///     including itself, to see.
        /// </summary>
        /// <param name="source">The entity above which to show the message.</param>
        /// <param name="message">The message to be seen.</param>
        /// <param name="playerManager">
        ///     The instance of player manager to use, will be resolved automatically
        ///     if null.
        /// </param>
        /// <param name="range">
        ///     The range in which to search for players, defaulting to one screen.
        /// </param>
        public static void PopupMessageEveryone(this IEntity source, string message, IPlayerManager? playerManager = null, int range = 15)
        {
            source.PopupMessage(message);
            source.PopupMessageOtherClients(message, playerManager, range);
        }
    }
}
