using System.Linq;
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

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
    public List<SunShadowCycleDirection> Directions = new()
    {
        new SunShadowCycleDirection(0f, new Vector2(0f, 3f), 0f),
        new SunShadowCycleDirection(0.25f, new Vector2(-3f, -0.1f), 0.5f),
        new SunShadowCycleDirection(0.5f, new Vector2(0f, -3f), 0.8f),
        new SunShadowCycleDirection(0.75f, new Vector2(3f, -0.1f), 0.5f),
    };
};

[DataDefinition]
[Serializable, NetSerializable]
public partial record struct SunShadowCycleDirection
{
    [DataField]
    public float Ratio;

    [DataField]
    public Vector2 Direction;

    [DataField]
    public float Alpha;

    public SunShadowCycleDirection(float ratio, Vector2 direction, float alpha)
    {
        Ratio = ratio;
        Direction = direction;
        Alpha = alpha;
    }
};
