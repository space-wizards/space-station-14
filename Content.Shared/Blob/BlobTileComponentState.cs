using Robust.Shared.Serialization;

namespace Content.Shared.Blob;

[Serializable, NetSerializable]
public sealed class BlobTileComponentState : ComponentState
{
    public Color Color;
}
