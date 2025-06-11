using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Shared.Paper;

/// <summary>
/// If added to an entity that has a <see cref="PaperComponent"/>, the name,
/// description and contents of the paper will be replaced with a random
/// entry from the specified <see cref="LocalizedDatasetPrototype"/>.
/// Requires <see cref="PaperComponent"/>.
/// </summary>
[RegisterComponent]
public sealed partial class RandomPaperContentComponent : Component
{
    [DataField(required: true)]
    public ProtoId<LocalizedDatasetPrototype> Dataset;
}
