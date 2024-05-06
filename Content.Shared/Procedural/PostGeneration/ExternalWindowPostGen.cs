using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// If external areas are found will try to generate windows.
/// </summary>
public sealed partial class ExternalWindowPostGen : IDunGenLayer
{
    [DataField]
    public List<EntProtoId> Entities = new()
    {
        "Grille",
        "Window",
    };
}
