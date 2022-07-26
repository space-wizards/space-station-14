using Robust.Server.Player;

namespace Content.Server.Ghost.Roles;

public interface IGhostRoleRequester
{
    /// <summary>
    ///     Invoked when a request is complete. The request could have succeeded or failed.
    /// </summary>
    /// <param name="sessions">Null if the request failed. Contains the sessions selected if the request succeeded.</param>
    public void OnRequestComplete(IEnumerable<IPlayerSession>? sessions);
}
