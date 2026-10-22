using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Adds the given tags when triggered.
/// If TargetUser is true the tags will be added to the user instead.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AddTagsOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// The tags to add.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<TagPrototype>> Tags = new();
}

