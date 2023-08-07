using Robust.Shared.GameStates;

namespace Content.Shared.Blob;

[NetworkedComponent]
public abstract class SharedBlobTileComponent : Component
{
    [DataField("color")]
    public Color Color = Color.White;
}
