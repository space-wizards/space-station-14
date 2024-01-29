using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared.Placeable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(PlaceableSurfaceSystem))]
public sealed partial class PlaceableSurfaceComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IsPlaceable { get; set; } = true;

    [DataField, AutoNetworkedField]
    public bool PlaceCentered { get; set; }

    [DataField, AutoNetworkedField]
    public Vector2 PositionOffset { get; set; }
}
