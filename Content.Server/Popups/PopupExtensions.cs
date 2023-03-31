using Content.Shared.Popups;
using Robust.Server.Player;
using Robust.Shared.Player;

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
        [Obsolete("Use PopupSystem.PopupEntity instead")]
        public static void PopupMessageOtherClients(this EntityUid source, string message)
        {
            var viewers = Filter.Empty()
                .AddPlayersByPvs(source)
                .Recipients;

            foreach (var viewer in viewers)
            {
                if (viewer.AttachedEntity is not {Valid: true} viewerEntity || source == viewerEntity || viewer.AttachedEntity == null)
                {
                    continue;
                }

                source.PopupMessage(viewerEntity, message);
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
        [Obsolete("Use PopupSystem.PopupEntity instead")]
        public static void PopupMessageEveryone(this EntityUid source, string message, IPlayerManager? playerManager = null, int range = 15)
        {
            source.PopupMessage(message);
            source.PopupMessageOtherClients(message);
        }
    }
}
