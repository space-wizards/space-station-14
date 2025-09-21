using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Restrict;

/// <summary>
/// Component to be used when a use of an entity is restricted by having equipped a specific item.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RestrictByEquippedTagComponent : Component
{
    /// <summary>
    /// The tag that must be present on an equipped item for the user to be allowed to use this entity.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<TagPrototype> RequiredTag = default!;

    /// <summary>
    /// The message to show when the user doesn't have the required equipped item.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public string DenialMessage = "You do not have the required item to use this.";

    /// <summary>
    /// The sound to play when user is denied the access. If null, no sound is played.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? DenialSound;
} 