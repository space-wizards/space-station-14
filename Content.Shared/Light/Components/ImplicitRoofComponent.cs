using Robust.Shared.GameStates;

namespace Content.Shared.Light.Components;

/// <summary>
/// Assumes the entire attached grid is rooved. This component will get removed if the grid has RoofComponent.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ImplicitRoofComponent : Component
{
    [DataField, AutoNetworkedField]
    public Color Color = Color.Black;
}
