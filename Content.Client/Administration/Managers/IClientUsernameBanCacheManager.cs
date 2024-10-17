
namespace Content.Client.Administration.Managers;

public interface IClientUsernameBanCacheManager
{
    /// <summary>
    ///     Fired when the cache is updated
    /// </summary>
    event Action<List<(int, string, bool, bool)>>? UpdatedCache;

    IReadOnlyList<(int, string, bool, bool)> BanList { get; }

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