
namespace Content.Client.Administration.Managers;

public interface IClientUsernameBanCacheManager
{
    /// <summary>
    ///     Fired when the cache is updated
    /// </summary>
    event Action<List<(int, string, string, bool)>>? UpdatedCache;

    IReadOnlyList<(int, string, string, bool)> BanList { get; }

    void Initialize();

    /// <summary>
    ///     Sends A request to the server to send new data
    /// </summary>
    void RequestUsernameBans();
}