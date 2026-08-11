using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.GameTicking;

namespace Content.Server.Connection;

/// <summary>
/// Handles Fallback Server tracking and updating
/// These servers will be offered to the player, if the server they are trying to connect is full.
/// </summary>
public sealed partial class ConnectionManager
{
    // The list of known fallback servers, and their details
    // The server URIs are used as the dictionary key
    private readonly Dictionary<string, (string name, int players, int max)> _fallbackServers = new();

    // The amount of time before a server is considered timed out for status checks.
    private static readonly TimeSpan ServerStatusTimeout = TimeSpan.FromSeconds(5); //TODO make this some central constant somewhere?

    /// <summary>
    /// Package all current fallback server data into a single string, that can be sent to the client
    /// </summary>
    private string StringifyFallbackServers()
    {
        var result = string.Empty;

        foreach (var server in _fallbackServers)
        {
            result += server.Value.name + "," + server.Key + "," + server.Value.players + "," + server.Value.max + ";";
        }

        return result;
    }

    /// <summary>
    /// Create the Fallback Server profiles, from the raw cvar value
    /// </summary>
    private void OnFallbackServersCvar(string fallbackServersRaw) //TODO:ERRANT replace ; , with final choice. Waiting on whether we want names to be definable
    {
        foreach (var serverRaw in fallbackServersRaw.Split(";", StringSplitOptions.RemoveEmptyEntries))
        {
            if (serverRaw.Split(",", StringSplitOptions.RemoveEmptyEntries).Length != 2)
            {
                _sawmill.Warning($"FallbackServers cvar is malformed - each element must contain exactly one comma character. '{serverRaw}'");
                continue;
            }
            var pos = serverRaw.IndexOf(",", StringComparison.Ordinal);
            var uri = serverRaw[(pos+1)..];
            var name = serverRaw[..pos];

            UpdateServerDetails(uri.Trim(), name.Trim());
        }
    }

    /// <summary>
    /// Updates the player data for all current fallback servers
    /// </summary>
    private void UpdateAllServers()
    {
        foreach (var server in _fallbackServers)
        {
            UpdateServerDetails(server.Key, server.Value.name);
        }
    }

    /// <summary>
    /// Adds a new server to the fallback list, or updates the details of an existing one.
    /// </summary>
    /// <param name="uri">The address of the server. This is used as their unique key for handling</param>
    /// <param name="name">The displayed name of the server. (This will be shown over the </param>
    private async void UpdateServerDetails(string uri, string name)
    {
        //TODO: More error checking for the input values?

        //TODO: Do we want to copy UriHelper from the Launcher to do these operations properly and consistently across future uses?

        if (!uri.StartsWith("ss14s://")
            && !uri.StartsWith("ss14://") )
        {
            _sawmill.Info($"Invalid address in FallbackServers cvar: {uri}");
            return;
        }

        var pos = uri.IndexOf("://", StringComparison.Ordinal);
        var slash = uri.EndsWith("/") ? string.Empty : "/";
        var statusUrl = "http" + uri[pos..] + slash + "status";

        try
        {
            // Try to get up-to-date server data
            var status = await GetServerData(statusUrl);

            if (status is null)
            {
                // We still change the dictionary because the server may have already been added to it,
                // in which case we need it to no longer show up
                // Since these servers are specifically picked by the server operator/admins,
                // we can assume that they are generally supposed to be up, and any outage should be considered temporary
                _fallbackServers[uri] = (name, 0, -1);
                return;
            }

            // We probably want to use the name provided by the cvar, rather than the server's real name
            // in case a shorter/different one was intentionally chosen for presentation reasons
            _fallbackServers[uri] = (name, status.PlayerCount, status.SoftMaxPlayerCount);
        }
        catch
        {
            _sawmill.Warning($"Error while trying to query Fallback Server '{uri}'");
        }
    }
    // TODO:ERRANT Did the hub mods respond about policy questions?

    //This is from the launcher's ServerStatusCache.cs. We can't exactly call that, so it has to be duplicated
    /// <summary>
    /// Returns server status data for the target URL
    /// </summary>
    private async Task<ServerStatus?> GetServerData(string url, CancellationToken cancel = default)
    {
        ServerStatus status;
        try
        {
            using (var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancel))
            {
                linkedToken.CancelAfter(ServerStatusTimeout);

                status = await _http.Client.GetFromJsonAsync<ServerStatus>(url, linkedToken.Token)
                         ?? throw new InvalidDataException();
            }

            cancel.ThrowIfCancellationRequested();
        }
        catch (Exception e) when (e is JsonException or HttpRequestException or InvalidDataException or IOException
                                      or SocketException)
        {
            _sawmill.Info($"A Fallback Server did not respond to the status query - '{url}'");
            return null;
        }

        return status;
    }
}

//TODO:ERRANT where should this be? In the Launcher, it was defined in ServerApi, but in this project that file is for admin API stuff
//https://docs.spacestation14.io/en/engine/http-api
public sealed record ServerStatus(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("players")] int PlayerCount,
    [property: JsonPropertyName("soft_max_players")]
    int SoftMaxPlayerCount,
    [property: JsonPropertyName("round_start_time")]
    string? RoundStartTime,
    [property: JsonPropertyName("run_level")]
    GameRunLevel? RunLevel,
    [property: JsonPropertyName("tags")] string[]? Tags);
