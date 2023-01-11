using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Administration;

public interface IGamePrototypeLoadManager
{
    public void Initialize();
    public void SendGamePrototype(string prototype);
}

// TODO REPLAYS
// Figure out a way to just directly save NetMessage objects to replays. This just uses IRobustSerializer as a crutch.

[Serializable, NetSerializable]
public sealed class ReplayPrototypeUploadMsg
{
    public string PrototypeData = default!;
}
