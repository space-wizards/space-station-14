using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Chemistry.Components;

[RegisterComponent]
public sealed class SmokeComponent : Component
{
    public const string SolutionName = "solutionArea";

    [DataField("nextReact", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextReact = TimeSpan.Zero;

    /// <summary>
    ///     Have we reacted with our tile yet?
    /// </summary>
    [DataField("reactedTile")]
    public bool ReactedTile = false;
}
