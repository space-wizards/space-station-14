using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Blob;

[NetworkedComponent]
public abstract class SharedBlobTileComponent : Component
{
    [DataField("state")]
    public BlobTileState State = BlobTileState.Green;
}

[Serializable, NetSerializable]
public enum BlobTileState : byte
{
    Dead,
    Green,
    Blue,
}
