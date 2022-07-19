using Content.Client.GameTicking.Managers;
using Content.Shared.CrewManifest;

namespace Content.Client.CrewManifest;

public sealed class CrewManifestSystem : EntitySystem
{
    /// <summary>
    ///     Requests a crew manifest from the server.
    /// </summary>
    /// <param name="uid">EntityUid of the entity we're requesting the crew manifest from.</param>
    public void RequestCrewManifest(EntityUid uid)
    {
        RaiseNetworkEvent(new RequestCrewManifestMessage(uid));
    }
}
