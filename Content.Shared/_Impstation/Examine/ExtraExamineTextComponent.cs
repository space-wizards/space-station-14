using Robust.Shared.GameStates;
using Content.Shared._Impstation.Clothing;

namespace Content.Shared._Impstation.Examine;

/// <summary>
/// Adds examine text to the entity, intentionally "obvious details".
/// Like, that's it. It's basic -- all it does is add the lines to the attached entity.
/// This is particularly used for assigning players unique examine text.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ExtraExamineTextSystem), typeof(WearerGetsExamineTextSystem))]
public sealed partial class ExtraExamineTextComponent : Component
{
    /// <summary>
    /// The LocIds that will be added to the attached entity's examination.
    ///
    /// The key is the source of the line, and the value is the text
    /// (built by the component that creates this one).
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<EntityUid, LocId> Lines { get; set; } = new();

}
