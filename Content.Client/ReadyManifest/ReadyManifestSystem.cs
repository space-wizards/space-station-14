using Content.Shared.ReadyManifest;

namespace Content.Client.ReadyManifest;

public sealed class ReadyManifestSystem : EntitySystem
{
    private HashSet<string> _departments = new();

    public IReadOnlySet<string> Departments => _departments;

    public override void Initialize()
    {
        base.Initialize();
    }

    public void RequestReadyManifest()
    {
        RaiseNetworkEvent(new RequestReadyManifestMessage());
    }
}
