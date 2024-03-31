using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Tilenol;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TilenolComponent : Component
{
    public bool IsSliding => SlideStart != null;

    [AutoNetworkedField]
    public EntityCoordinates Origin;

    [AutoNetworkedField]
    public Vector2 Destination;

    /// <summary>
    /// When the current slide started
    /// </summary>
    [AutoNetworkedField]
    public TimeSpan? SlideStart;

    /// <summary>
    /// How long the current slide should take to finish.
    /// </summary>
    [AutoNetworkedField]
    public TimeSpan SlideDuration;

    /// <summary>
    /// When the most recent slide finished.
    /// </summary>
    [AutoNetworkedField]
    public TimeSpan? LastSlideEnd;

    /// <summary>
    /// Time in seconds between slide attempts. This effectively imposes a speed limit.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan SlideDelay = TimeSpan.Zero; // TimeSpan.FromSeconds(0.2f);

    /// <summary>
    /// Modifier used for calculating <see cref="SlideDuration"/>
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float SlideSpeed = 1;

    /// <summary>
    /// Whether to use a constant or quadratic slide speed.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool LinearInterp;

}
