using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Server.Chat;

/// <summary>
/// Entity will say the things when activated
/// </summary>
[RegisterComponent]
public sealed partial class SpeakOnUseComponent : Component
{
    /// <summary>
    /// The identifier for the dataset prototype containing messages to be spoken by this entity.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<LocalizedDatasetPrototype> Pack { get; private set; }

}
