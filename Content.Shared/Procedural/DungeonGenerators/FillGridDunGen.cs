using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.DungeonGenerators;

/// <summary>
/// Fills unreserved tiles with the specified entity prototype.
/// </summary>
public sealed partial class FillGridDunGen : IDunGen
{
    [DataField]
    public EntProtoId Proto;
}
