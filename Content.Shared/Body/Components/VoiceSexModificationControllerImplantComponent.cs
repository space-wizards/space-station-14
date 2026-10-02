using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Body.Components;

/// <summary>
/// Enables an implanted entity to change their voice/sex with an action.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class VoiceSexModificationControllerImplantComponent : Component
{
    /// <summary>
    /// If true, the implant and action is deleted on use.
    /// </summary>
    [DataField]
    public bool DeleteOnUse = true;

    /// <summary>
    /// If set, this sound will play when a modification is made.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Effects/guardian_inject.ogg");
}
