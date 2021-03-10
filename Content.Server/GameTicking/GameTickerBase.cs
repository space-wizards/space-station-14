using Content.Server.Players;
using Content.Shared.GameTicking;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

#nullable enable

namespace Content.Server.GameTicking
{
    /// <summary>
    ///     Handles some low-level GameTicker behavior such as setting up clients when they connect.
    ///     Does not contain lobby/round handling mechanisms.
    /// </summary>
    public abstract class GameTickerBase : SharedGameTicker
    {
        [Dependency] protected readonly IPlayerManager PlayerManager = default!;

        public virtual void Initialize()
        {
            PlayerManager.PlayerStatusChanged += PlayerStatusChanged;
        }

        protected virtual void PlayerStatusChanged(object? sender, SessionStatusEventArgs args)
        {
            var session = args.Session;

            if (args.NewStatus == SessionStatus.Connected)
            {
                // Always make sure the client has player data. Mind gets assigned on spawn.
                if (session.Data.ContentDataUncast == null)
                    session.Data.ContentDataUncast = new PlayerData(session.UserId);

                // timer time must be > tick length
                Timer.Spawn(0, args.Session.JoinGame);
            }
        }
    }
}
