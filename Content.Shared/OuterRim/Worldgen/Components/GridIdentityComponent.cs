using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.OuterRim.Worldgen.Components;

/// <summary>
/// This is used for grid visuals in radars.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class GridIdentityComponent : Component
{
    public Color GridColor = Color.Aquamarine;
    public bool ShowIff = true;
}

/// <summary>
/// Contains network state for ObjectColor.
/// </summary>
[Serializable, NetSerializable]
public sealed class GridIdentityComponentState : ComponentState
{
    public Color GridColor;
    public bool ShowIff;

    public GridIdentityComponentState(GridIdentityComponent component)
    {
        GridColor = component.GridColor;
        ShowIff = component.ShowIff;
    }
}
