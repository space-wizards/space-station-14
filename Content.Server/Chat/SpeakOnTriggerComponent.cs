using Content.Shared.Dataset;
using Robust.Shared.Prototypes;
using Content.Shared.DeviceLinking;

namespace Content.Server.Chat;

/// <summary>
///     Makes the entity speak when triggered. If the item has UseDelay component, the system will respect that cooldown.
/// </summary>
[RegisterComponent]
public sealed partial class SpeakOnTriggerComponent : Component
{
    /// <summary>
    ///     The identifier for the dataset prototype containing messages to be spoken by this entity.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<LocalizedDatasetPrototype> Pack { get; private set; }

    /// <summary>
    ///     The port that makes the entity speak when triggered.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> ActivatePort = "Trigger";
}
