using Content.Shared.ReadyManifest;

namespace Content.Client.ReadyManifest;

public sealed class ReadyManifestSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
    }

    public void RequestReadyManifest()
    {
        RaiseNetworkEvent(new RequestReadyManifestMessage());
    }
}
