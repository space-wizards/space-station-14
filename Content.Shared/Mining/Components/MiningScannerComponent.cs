using Robust.Shared.GameStates;

namespace Content.Shared.Mining.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(MiningScannerSystem))]
public sealed partial class MiningScannerComponent : Component
{
    [DataField]
    public float Range = 5;
}
