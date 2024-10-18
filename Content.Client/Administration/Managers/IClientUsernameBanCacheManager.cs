
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

    void Initialize();

    /// <summary>
    ///     Sends A request to the server to send new data
    /// </summary>
    void RequestUsernameBans();

    /// <summary>
    ///     Sends a request for the full username ban info
    /// </summary>
    /// <param name="id"></param>
    public void RequestFullUsernameBan(int id);
}

public readonly record struct UsernameCacheLine(string Expression, int Id, bool ExtendToBan, bool Regex);
