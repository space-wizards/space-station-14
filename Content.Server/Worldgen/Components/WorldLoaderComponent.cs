using Content.Server.Worldgen.Systems;

namespace Content.Server.Worldgen.Components;

/// <summary>
/// This is used for allowing static, non-player objects to load chunks.
/// Objects with this component will load at a minimum the chunk they're in, potentially more depending on the range.
/// </summary>
[RegisterComponent]
public sealed class WorldLoaderComponent : Component
{
    [DataField("range"), ViewVariables(VVAccess.ReadWrite)]
    public float Range = 64.0f;

    [DataField("loadingFlags"), ViewVariables(VVAccess.ReadWrite)]
    public LoadingFlags LoadingFlags = LoadingFlags.Stationary;
}
