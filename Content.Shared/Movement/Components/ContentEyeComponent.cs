using System.Numerics;
using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Holds SS14 eye data not relevant for engine, e.g. lerp targets.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedContentEyeSystem))]
public sealed partial class ContentEyeComponent : Component
{
    /// <summary>
    /// Zoom we're lerping to.
    /// </summary>
    [DataField("targetZoom"), AutoNetworkedField]
    public Vector2 TargetZoom = Vector2.One;

    /// <summary>
    /// How far we're allowed to zoom out.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("maxZoom"), AutoNetworkedField]
    public Vector2 MaxZoom = Vector2.One;
}
