
namespace Content.Client.Administration.Managers;

public interface IClientUsernameBanCacheManager
{
    /// <summary>
    ///     Fired when the cache is updated
    /// </summary>
    event Action UpdatedCache;

    void Initialize();

    /// <summary>
    ///     Sends A request to the server to send new data
    /// </summary>
    void RequestUsernameBans();
}