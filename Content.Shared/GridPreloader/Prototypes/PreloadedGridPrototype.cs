using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.GridPreloader.Prototypes;

/// <summary>
/// Creating this prototype will automatically load the grid at the specified path at the beginning of the round,
/// and allow the GridPreloader system to load them in the middle of the round. This is needed for optimization,
/// because loading grids in the middle of a round causes the server to lag.
/// </summary>
[Prototype]
public sealed partial class PreloadedGridPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = string.Empty;

    [DataField(required: true)]
    public ResPath Path;

    [DataField]
    public int Copies = 1;
}
