
namespace Content.Client.Administration.Managers;

public interface IClientUsernameBanCacheManager
{
    /// <summary>
    ///     Fired when the cache is updated
    /// </summary>
    event Action<List<UsernameCacheLine>>? UpdatedCache;

    /// <summary>
    /// derived field used to provide the total username ban list
    /// </summary>
    IReadOnlyList<UsernameCacheLine> BanList { get; }

    /// <summary>
    /// starts the manager by registering messages and subscribing to client administration changes
    /// </summary>
    void Initialize();

    /// <summary>
    ///     Sends A request to the server to send new data
    /// </summary>
    void RequestUsernameBans();
}

public readonly record struct UsernameCacheLine(string Expression, int Id, bool ExtendToBan, bool Regex);
