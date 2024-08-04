using Robust.Shared.Serialization;
using Robust.Shared.Sandboxing;

namespace Content.Shared.Overlays;

public abstract class SharedDebugOverlaySystem<TPayload> : EntitySystem
    where TPayload : DebugOverlayPayload, new()
{
    [Dependency] protected readonly ISandboxHelper _sandboxHelper = default!;
}

[Serializable, NetSerializable]
public abstract class DebugOverlayPayload : EntityEventArgs
{
    public bool OverlayEnabled;
}
