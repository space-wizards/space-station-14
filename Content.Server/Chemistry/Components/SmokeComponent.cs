using Content.Shared.Fluids.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Chemistry.Components;

/// <summary>
/// Stores solution on an anchored entity that has touch and ingestion reactions
/// to entities that collide with it. Similar to <see cref="PuddleComponent"/>
/// </summary>
[RegisterComponent]
public sealed partial class SmokeComponent : Component
{
    public const string SolutionName = "solutionArea";

    [DataField("nextReact", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextReact = TimeSpan.Zero;

    [DataField("spreadAmount")]
    public int SpreadAmount = 0;

    /// <summary>
    ///     Have we reacted with our tile yet?
    /// </summary>
    [DataField("reactedTile")]
    public bool ReactedTile = false;
}
