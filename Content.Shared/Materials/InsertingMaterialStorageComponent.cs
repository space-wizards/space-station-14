using Robust.Shared.GameStates;

namespace Content.Shared.Materials;

[RegisterComponent, NetworkedComponent]
public sealed class InsertingMaterialStorageComponent : Component
{
    /// <summary>
    /// The time when insertion ends.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan EndTime;

    [ViewVariables]
    public Color? MaterialColor;
}
