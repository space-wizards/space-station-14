using Robust.Shared.GameStates;

namespace Content.Shared.Light.Components;

/// <summary>
/// Counts the tile this entity on as being rooved.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class IsRoofComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;
}
