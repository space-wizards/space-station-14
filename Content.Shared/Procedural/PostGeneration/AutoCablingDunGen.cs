using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// Runs cables throughout the dungeon.
/// </summary>
public sealed partial class AutoCablingDunGen : IDunGenLayer
{
    [DataField(required: true)]
    public EntProtoId Entity;
}
