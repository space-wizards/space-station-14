using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Blob;

[NetworkedComponent]
public abstract class SharedBlobbernautComponent : Component
{
    [DataField("color")]
    public Color Color = Color.White;
}
