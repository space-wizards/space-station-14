using Content.Server.GameTicking;
using Content.Server.Players;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Mind
{
    internal static class MindHelpers
    {
        internal static void SendToGhost(this IEntity entity, bool canReturn=false)
        {
            var mind = entity.PlayerSession()?.ContentData()?.Mind;

            if (mind == null) return;

            EntitySystem.Get<GameTicker>().OnGhostAttempt(mind, canReturn);
        }
    }
}
