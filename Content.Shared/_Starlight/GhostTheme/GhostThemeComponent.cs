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
}

[Serializable, NetSerializable]
public enum GhostThemeVisualLayers : byte
{
    Base
}

[Serializable, NetSerializable]
public sealed class GhostThemeSyncEvent : EntityEventArgs
{
    public readonly NetEntity Player;
    
    public GhostThemeSyncEvent(NetEntity player)
    {
        Player = player;
    }
}