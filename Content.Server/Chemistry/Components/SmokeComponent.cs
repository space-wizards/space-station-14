using Content.Shared.Chemistry.Components;
using Content.Shared.Fluids.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Chemistry.Components;

/// <summary>
/// Stores solution on an anchored entity that has touch and ingestion reactions
/// to entities that collide with it. Similar to <see cref="PuddleComponent"/>
/// </summary>
[RegisterComponent]
public sealed class SmokeComponent : SharedSmokeComponent
{
    public const string SolutionName = "solutionArea";

    [DataField("nextReact", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextReact = TimeSpan.Zero;

    [DataField("spreadAmount")]
    public int SpreadAmount = 0;

    [DataField("smokeColor")]
    public Color SmokeColor = Color.Black;

    /// <summary>
    ///     Have we reacted with our tile yet?
    /// </summary>
    [DataField("reactedTile")]
    public bool ReactedTile = false;
}
