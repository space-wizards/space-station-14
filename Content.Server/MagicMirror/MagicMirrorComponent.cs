using Content.Shared.Humanoid;
using Robust.Shared.Audio;

namespace Content.Server.MagicMirror;

[RegisterComponent]
public sealed partial class MagicMirrorComponent : Component
{
    public Entity<HumanoidAppearanceComponent>? Target;

    /// <summary>
    /// radius in which the component can edit hairstyles
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Range = 1f;

    /// <summary>
    /// doafter time required to add a new slot
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float AddSlotTime = 5f;

    /// <summary>
    /// doafter time required to remove a existing slot
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float RemoveSlotTime = 2f;

    /// <summary>
    /// doafter time required to change slot
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SelectSlotTime = 3f;

    /// <summary>
    /// doafter time required to recolor slot
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ChangeSlotTime = 1f;

    /// <summary>
    /// sound emitted when slots are changed
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier ChangeHairSound = new SoundPathSpecifier("/Audio/Items/scissors.ogg");
}
