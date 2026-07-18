using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Server.Vocalization.Components;

/// <summary>
/// A simple message provider for <see cref="VocalizationSystem"/> that randomly selects
/// messages from a <see cref="LocalizedDatasetPrototype"/>.
/// </summary>
[RegisterComponent]
public sealed partial class DatasetVocalizerComponent : Component
{
    /// <summary>
    /// ID of the <see cref="LocalizedDatasetPrototype"/> that will provide messages.
    /// </summary>
    [DataField]
    public ProtoId<LocalizedDatasetPrototype> Dataset;
}
