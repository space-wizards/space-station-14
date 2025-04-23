using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Starlight.GhostTheme;

[RegisterComponent, NetworkedComponent]
public sealed partial class GhostThemeComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)] // No admeme
    [DataField]
    public string SelectedGhostTheme = "None";
    
    [ViewVariables(VVAccess.ReadOnly)] // No admeme
    [DataField]
    public Color GhostThemeColor = Color.White;
}

[Serializable, NetSerializable]
public enum GhostThemeVisualLayers : byte
{
    Base,
    Color
}