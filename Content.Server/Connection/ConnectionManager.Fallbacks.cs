namespace Content.Server.Connection;

/// <summary>
/// Handles Fallback Server tracking and updating
/// These servers will be offered to the player, if the server they are trying to connect is full.
/// </summary>
public sealed partial class ConnectionManager
{
    private Dictionary<string, (string name, int players, int max)> _fallbackServers = new();
    private string _fallbackString = string.Empty;

    /// <summary>
    /// Initial creation of the Fallback Server profiles, from the raw cvar value
    /// </summary>
    private void ReadFallbackServers(string fallbackServersRaw)
    {
        foreach (var serverRaw in fallbackServersRaw.Split(";", StringSplitOptions.RemoveEmptyEntries))
        {
            if (serverRaw.Split(",", StringSplitOptions.RemoveEmptyEntries).Length != 2)
            {
                _sawmill.Warning($"FallbackServers cvar is malformed - must contain exactly one comma. '{serverRaw}'");
                continue;
            }
            var pos = serverRaw.IndexOf(",", StringComparison.Ordinal);
            var url = serverRaw[(pos+1)..];
            var name = serverRaw[..pos];

            //TODO:ERRANT Under construction
            var players =10;
            var max = 75;
            ///////////////////////////////////

            UpdateServerDetails(url, name, players, max);
        }
    }

    /// <summary>
    /// Adds a new server to the fallback list, or updates the details of an existing one.
    /// </summary>
    /// <param name="url">The address of the server. This is used as their unique key for handling</param>
    /// <param name="name">The displayed name of the server</param>
    /// <param name="players">Current players on the server</param>
    /// <param name="max">Maximum players on the server</param>
    private void UpdateServerDetails(string url, string name, int players, int max)
    {
        //TODO: Error checking for the input values?

        //TODO:ERRANT Under construction
        players = _random.Next(60, 90);
        ///////////////////////////////////

        _fallbackServers[url.Trim()] = (name.Trim(), players, max);

        // Package all current fallback server data into a single string, that can be sent to the client
        var result = string.Empty;

        foreach (var server in _fallbackServers)
        {
            result += server.Value.name + "," + server.Key + "," + server.Value.players + "," + server.Value.max + ";";
        }

        _fallbackString = result;
    }

    //TODO:ERRANT Add a way to get serverinfo, and call UpdateServerDetails when it changes

}
