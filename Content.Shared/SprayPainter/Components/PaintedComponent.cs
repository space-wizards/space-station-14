using Robust.Shared.GameStates;

namespace Content.Shared.SprayPainter.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PaintedComponent : Component
{
    [DataField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan RemoveTime = TimeSpan.FromMinutes(15);
}
