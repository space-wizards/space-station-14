using Content.Shared.Interfaces;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Utility
{
    public static class NotifyExtensions
    {
        /// <summary>
        ///     Pops up a message for every player around <see cref="source"/> to see,
        ///     except for <see cref="source"/> itself.
        /// </summary>
        /// <param name="source">The entity on which to popup the message.</param>
        /// <param name="message">The message to show.</param>
        /// <param name="range">
        ///     The range in which to search for players, defaulting to one screen.
        /// </param>
        public static void PopupMessageOtherClients(this IEntity source, string message, int range = 15)
        {
            var playerManager = IoCManager.Resolve<IPlayerManager>();
            var viewers = playerManager.GetPlayersInRange(source.Transform.GridPosition, range);

            foreach (var viewer in viewers)
            {
                var viewerEntity = viewer.AttachedEntity;

                if (viewerEntity == null || source == viewerEntity)
                {
                    continue;
                }

                source.PopupMessage(viewer.AttachedEntity, message);
            }
        }
    }
}
