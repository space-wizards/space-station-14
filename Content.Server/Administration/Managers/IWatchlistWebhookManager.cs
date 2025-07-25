using Content.Server.Administration.Notes;
using Content.Server.Database;
using Content.Server.Discord;
using Content.Shared.CCVar;
using Robust.Server;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using System.Linq;

namespace Content.Server.Administration.Managers;

/// <summary>
///     This manager sends a webhook notification whenever a player with an active
///     watchlist joins the server.
/// </summary>
public interface IWatchlistWebhookManager
{
    void Initialize();
    void Update();
}
