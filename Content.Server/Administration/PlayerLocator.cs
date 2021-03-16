using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;

namespace Content.Server.Administration
{
    /// <summary>
    ///     Utilities for finding user IDs that extend to more than the server database.
    /// </summary>
    /// <remarks>
    ///     Methods in this class will check connected clients, server database
    ///     AND the authentication server for lookups, in that order.
    /// </remarks>
    public interface IPlayerLocator
    {
        /// <summary>
        ///     Look up a user ID by name globally.
        /// </summary>
        /// <returns>Null if the player does not exist.</returns>
        Task<NetUserId?> LookupIdByNameAsync(string playerName, CancellationToken cancel = default);

        /// <summary>
        ///     If passed a GUID, runs <see cref="DoesPlayerExistAsync"/> and only returns it if the account exists.
        ///     If passed a player name, returns <see cref="LookupIdByNameAsync"/>.
        /// </summary>
        Task<NetUserId?> LookupIdByNameOrIdAsync(string playerName, CancellationToken cancel = default);

        /// <summary>
        ///     Checks whether the specified user ID is an existing account, globally.
        /// </summary>
        /// <returns>True if the player account exists, false otherwise</returns>
        Task<bool> DoesPlayerExistAsync(NetUserId userId, CancellationToken cancel = default);
    }

    internal sealed class PlayerLocator : IPlayerLocator
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IServerDbManager _db = default!;

        public async Task<NetUserId?> LookupIdByNameAsync(string playerName, CancellationToken cancel = default)
        {
            // Check people currently on the server, the easiest case.
            if (_playerManager.TryGetSessionByUsername(playerName, out var session))
                return session.UserId;

            // Check database for past players.
            var record = await _db.GetPlayerRecordByUserName(playerName, cancel);
            if (record != null)
                return record.UserId;

            // If all else fails, ask the auth server.
            var client = new HttpClient();
            var authServer = _configurationManager.GetCVar(CVars.AuthServer);
            var resp = await client.GetAsync($"{authServer}api/query/name?name={WebUtility.UrlEncode(playerName)}",
                cancel);

            if (resp.StatusCode == HttpStatusCode.NotFound)
                return null;

            if (!resp.IsSuccessStatusCode)
            {
                Logger.ErrorS("PlayerLocate", "Auth server returned bad response {StatusCode}!", resp.StatusCode);
                return null;
            }

            var responseData = await resp.Content.ReadFromJsonAsync<UserDataResponse>(cancellationToken: cancel);

            if (responseData == null)
            {
                Logger.ErrorS("PlayerLocate", "Auth server returned null response!");
                return null;
            }

            return new NetUserId(responseData.UserId);
        }

        public async Task<bool> DoesPlayerExistAsync(NetUserId userId, CancellationToken cancel = default)
        {
            // Check people currently on the server, the easiest case.
            if (_playerManager.ValidSessionId(userId))
                return true;

            // Check database for past players.
            var record = await _db.GetPlayerRecordByUserId(userId, cancel);
            if (record != null)
                return true;

            // If all else fails, ask the auth server.
            var client = new HttpClient();
            var authServer = _configurationManager.GetCVar(CVars.AuthServer);
            var requestUri = $"{authServer}api/query/userid?userid={WebUtility.UrlEncode(userId.UserId.ToString())}";
            var resp = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, requestUri), cancel);

            if (resp.StatusCode == HttpStatusCode.NotFound)
                return false;

            if (!resp.IsSuccessStatusCode)
            {
                Logger.ErrorS("PlayerLocate", "Auth server returned bad response {StatusCode}!", resp.StatusCode);
                return false;
            }

            return true;
        }

        public async Task<NetUserId?> LookupIdByNameOrIdAsync(string playerName, CancellationToken cancel = default)
        {
            if (Guid.TryParse(playerName, out var guid))
            {
                var userId = new NetUserId(guid);

                return await DoesPlayerExistAsync(userId, cancel) ? userId : null;
            }

            return await LookupIdByNameAsync(playerName, cancel);
        }

        [UsedImplicitly]
        private sealed record UserDataResponse(string UserName, Guid UserId)
        {
        }
    }
}
