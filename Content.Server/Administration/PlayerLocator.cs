using System.Collections.Immutable;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Connection;
using Content.Server.Database;
using Content.Shared.Database;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Administration
{
    /// <summary>
    /// Contains data resolved via <see cref="IPlayerLocator"/>.
    /// </summary>
    /// <param name="UserId">The ID of the located user.</param>
    /// <param name="LastAddress">The last known IP address that the user connected with.</param>
    /// <param name="LastHWId">
    /// The last known HWID that the user connected with.
    /// This should be used for placing new records involving HWIDs, such as bans.
    /// For looking up data based on HWID, use combined <see cref="LastLegacyHWId"/> and <see cref="LastModernHWIds"/>.
    /// </param>
    /// <param name="Username">The last known username for the user connected with.</param>
    /// <param name="LastLegacyHWId">
    /// The last known legacy HWID value this user connected with. Only use for old lookups!
    /// </param>
    /// <param name="LastModernHWIds">
    /// The set of last known modern HWIDs the user connected with.
    /// </param>
    public sealed record LocatedPlayerData(
        NetUserId UserId,
        IPAddress? LastAddress,
        ImmutableTypedHwid? LastHWId,
        string Username,
        ImmutableArray<byte>? LastLegacyHWId,
        ImmutableArray<ImmutableArray<byte>> LastModernHWIds);

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
        Task<LocatedPlayerData?> LookupIdByNameAsync(string playerName, CancellationToken cancel = default);

        /// <summary>
        ///     If passed a GUID, looks up the ID and tries to find HWId for it.
        ///     If passed a player name, returns <see cref="LookupIdByNameAsync"/>.
        /// </summary>
        Task<LocatedPlayerData?> LookupIdByNameOrIdAsync(string playerName, CancellationToken cancel = default);

        /// <summary>
        ///     Look up a user by <see cref="NetUserId"/> globally.
        /// </summary>
        /// <returns>Null if the player does not exist.</returns>
        Task<LocatedPlayerData?> LookupIdAsync(NetUserId userId, CancellationToken cancel = default);
    }

    internal sealed class PlayerLocator : IPlayerLocator, IDisposable, IPostInjectInit
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IServerDbManager _db = default!;
        [Dependency] private readonly ILogManager _logManager = default!;

        private readonly HttpClient _httpClient = new();
        private ISawmill _sawmill = default!;

        public PlayerLocator()
        {
            if (typeof(PlayerLocator).Assembly.GetName().Version is { } version)
            {
                _httpClient.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue("SpaceStation14", version.ToString()));
            }
        }

        public async Task<LocatedPlayerData?> LookupIdByNameAsync(string playerName, CancellationToken cancel = default)
        {
            // Check people currently on the server, the easiest case.
            if (_playerManager.TryGetSessionByUsername(playerName, out var session))
                return ReturnForSession(session);

            // Check database for past players.
            var record = await _db.GetPlayerRecordByUserName(playerName, cancel);
            if (record != null)
                return ReturnForPlayerRecord(record);

            // If all else fails, ask the auth server.
            var authServer = _configurationManager.GetCVar(CVars.AuthServer);
            var requestUri = $"{authServer}api/query/name?name={WebUtility.UrlEncode(playerName)}";
            using var resp = await _httpClient.GetAsync(requestUri, cancel);

            return await HandleAuthServerResponse(resp, cancel);
        }

        public async Task<LocatedPlayerData?> LookupIdAsync(NetUserId userId, CancellationToken cancel = default)
        {
            // Check people currently on the server, the easiest case.
            if (_playerManager.TryGetSessionById(userId, out var session))
                return ReturnForSession(session);

            // Check database for past players.
            var record = await _db.GetPlayerRecordByUserId(userId, cancel);
            if (record != null)
                return ReturnForPlayerRecord(record);

            // If all else fails, ask the auth server.
            var authServer = _configurationManager.GetCVar(CVars.AuthServer);
            var requestUri = $"{authServer}api/query/userid?userid={WebUtility.UrlEncode(userId.UserId.ToString())}";
            using var resp = await _httpClient.GetAsync(requestUri, cancel);

            return await HandleAuthServerResponse(resp, cancel);
        }

        private async Task<LocatedPlayerData?> HandleAuthServerResponse(HttpResponseMessage resp, CancellationToken cancel)
        {
            if (resp.StatusCode == HttpStatusCode.NotFound)
                return null;

            if (!resp.IsSuccessStatusCode)
            {
                _sawmill.Error("Auth server returned bad response {StatusCode}!", resp.StatusCode);
                return null;
            }

            var responseData = await resp.Content.ReadFromJsonAsync<UserDataResponse>(cancellationToken: cancel);
            if (responseData == null)
            {
                _sawmill.Error("Auth server returned null response!");
                return null;
            }

            return new LocatedPlayerData(new NetUserId(responseData.UserId), null, null, responseData.UserName, null, []);
        }

        private static LocatedPlayerData ReturnForSession(ICommonSession session)
        {
            var userId = session.UserId;
            var address = session.Channel.RemoteEndPoint.Address;
            var hwId = session.Channel.UserData.GetModernHwid();
            return new LocatedPlayerData(
                userId,
                address,
                hwId,
                session.Name,
                session.Channel.UserData.HWId,
                session.Channel.UserData.ModernHWIds);
        }

        private static LocatedPlayerData ReturnForPlayerRecord(PlayerRecord record)
        {
            var hwid = record.HWId;

            return new LocatedPlayerData(
                record.UserId,
                record.LastSeenAddress,
                hwid,
                record.LastSeenUserName,
                hwid is { Type: HwidType.Legacy } ? hwid.Hwid : null,
                hwid is { Type: HwidType.Modern } ? [hwid.Hwid] : []);
        }

        public async Task<LocatedPlayerData?> LookupIdByNameOrIdAsync(string playerName, CancellationToken cancel = default)
        {
            if (Guid.TryParse(playerName, out var guid))
            {
                var userId = new NetUserId(guid);

                return await LookupIdAsync(userId, cancel);
            }

            return await LookupIdByNameAsync(playerName, cancel);
        }

        [UsedImplicitly]
        private sealed record UserDataResponse(string UserName, Guid UserId)
        {
        }

        void IDisposable.Dispose()
        {
            _httpClient.Dispose();
        }

        void IPostInjectInit.PostInject()
        {
            _sawmill = _logManager.GetSawmill("PlayerLocate");
        }
    }
}
