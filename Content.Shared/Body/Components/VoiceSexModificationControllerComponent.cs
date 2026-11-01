using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Components;

/// <summary>
/// Enables an entity to change their voice/sex at will using an action.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VoiceSexModificationControllerComponent : Component
{
    /// <summary>
    /// The action prototype that allows you to modify voice/sex.
    /// </summary>
    [DataField]
    public EntProtoId Action = "ActionVoiceSexModificationController";

    /// <summary>
    /// Entity to hold the action prototype.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// If set, this sound will play when a change is made.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Voice/Slime/slime_squish.ogg");
}
