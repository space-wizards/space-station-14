using Content.Shared.Dataset;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Makes the entity speak a message when triggered.
/// If TargetUser is true then they will be forced to speak instead.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpeakOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// The text to speak. This has priority over Pack.
    /// </summary>
    [DataField]
    public LocId? Text;

    /// <summary>
    /// The identifier for the dataset prototype containing messages to be spoken by this entity.
    /// The spoken text will be picked randomly from it.
    /// </summary>
    [DataField]
    public ProtoId<LocalizedDatasetPrototype>? Pack;
}
