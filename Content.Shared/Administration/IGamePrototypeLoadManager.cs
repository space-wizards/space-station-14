using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Administration;

public interface IGamePrototypeLoadManager
{
    public void Initialize();
    public void SendGamePrototype(string prototype);
}

[Serializable, NetSerializable]
public sealed class ReplayPrototypeUploadMsg
{
    public string PrototypeData = default!;
}
