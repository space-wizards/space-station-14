using System.Linq;
using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared.Light.Components;

/// <summary>
/// Applies <see cref="SunShadowComponent"/> direction vectors based on a time-offset. Will track <see cref="LightCycleComponent"/> on on MapInit
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SunShadowCycleComponent : Component
{
    /// <summary>
    /// How long an entire cycle lasts
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromMinutes(30);

    [DataField, AutoNetworkedField]
    public TimeSpan Offset;

    // Originally had this as ratios but it was slightly annoying to use.

    /// <summary>
    /// Time to have each direction applied. Will lerp from the current value to the next one.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<(float Ratio, Vector2 Direction, float Alpha)> Directions = new()
    {
        (0f, new Vector2(0f, 3f), 0f),
        (0.25f, new Vector2(-3f, -0.1f), 0.5f),
        (0.5f, new Vector2(0f, -3f), 0.8f),
        (0.75f, new Vector2(3f, -0.1f), 0.5f),
    };
}
