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
            var url = serverRaw[(pos+1)..].Trim();
            var name = serverRaw[..pos].Trim();

            //TODO:ERRANT Under construction
            var players =10;
            var max = 75;
            ///////////////////////////////////

            UpdateServerDetails(url, name, players, max);
        }

        _fallbackString = PackageFallbacks();
    }

    /// <summary>
    /// Updates the details of a server. Does nothing if the server is not already
    /// </summary>
    /// <param name="url"></param>
    /// <param name="name"></param>
    /// <param name="players"></param>
    /// <param name="max"></param>
    private void UpdateServerDetails(string url, string name, int players, int max) // Is there a point for this to be a class
    {
        //TODO: various error checking for the values being acceptable

        if (!_fallbackServers.ContainsKey(url))
        {
            _fallbackServers.Add(url, (name, players, max));
            return;
        }

        //TODO:ERRANT Under construction
        players = _random.Next(60, 90);
        ///////////////////////////////////

        _fallbackServers[url] = (name, players, max);
    }

    //TODO:ERRANT Add a way to get serverinfo, and call UpdateServerDetails when it changes

    private string PackageFallbacks() // Is there a point for this to be a class
    {
        var result = string.Empty;

        foreach (var server in _fallbackServers)
        {
            result += server.Value.name + "," + server.Key + "," + server.Value.players + "," + server.Value.max + ";";
        }

        return result;
    }
}
