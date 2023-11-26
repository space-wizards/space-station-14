using Robust.Shared.Audio;

namespace Content.Server.Bodycamera;

[RegisterComponent]
[Access(typeof(BodyCameraSystem))]
public sealed partial class BodyCameraComponent : Component
{
    /// <summary>
    /// Is the bodycamera enabled and broadcasting
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled;

    /// <summary>
    /// Is the bodycamera equipped to a valid slot
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public bool Equipped;

    [ViewVariables(VVAccess.ReadWrite), DataField("powerOnSound")]
    public SoundSpecifier? PowerOnSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_success.ogg");

    [ViewVariables(VVAccess.ReadWrite), DataField("powerOffSound")]
    public SoundSpecifier? PowerOffSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_failed.ogg");
}
