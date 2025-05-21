using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Radio;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.Borgs;

/// <summary>
/// Information for a borg type that can be selected by <see cref="BorgSwitchableTypeComponent"/>.
/// </summary>
/// <seealso cref="SharedBorgSwitchableTypeSystem"/>
[Prototype]
public sealed partial class BorgTypePrototype : IPrototype
{
    [ValidatePrototypeId<SoundCollectionPrototype>]
    private static readonly ProtoId<SoundCollectionPrototype> DefaultFootsteps = new("FootstepBorg");

    [IdDataField]
    public required string ID { get; set; }

    //
    // Description info (name/desc) is configured via localization strings directly.
    //

    /// <summary>
    /// The prototype displayed in the selection menu for this type.
    /// </summary>
    [DataField]
    public required EntProtoId DummyPrototype;

    //
    // Functional information
    //

    /// <summary>
    /// The amount of free module slots this borg type has.
    /// </summary>
    /// <remarks>
    /// This count is on top of the modules specified in <see cref="DefaultModules"/>.
    /// </remarks>
    /// <seealso cref="BorgChassisComponent.ModuleCount"/>
    [DataField]
    public int ExtraModuleCount { get; set; } = 0;

    /// <summary>
    /// The whitelist for borg modules that can be inserted into this borg type.
    /// </summary>
    /// <seealso cref="BorgChassisComponent.ModuleWhitelist"/>
    [DataField]
    public EntityWhitelist? ModuleWhitelist { get; set; }

    /// <summary>
    /// Inventory template used by this borg.
    /// </summary>
    /// <remarks>
    /// This template must be compatible with the normal borg templates,
    /// so in practice it can only be used to differentiate the visual position of the slots on the character sprites.
    /// </remarks>
    /// <seealso cref="InventorySystem.SetTemplateId"/>
    [DataField]
    public ProtoId<InventoryTemplatePrototype> InventoryTemplateId { get; set; } = "borgShort";

    /// <summary>
    /// Radio channels that this borg will gain access to from this module.
    /// </summary>
    /// <remarks>
    /// These channels are provided on top of the ones specified in
    /// <see cref="BorgSwitchableTypeComponent.InherentRadioChannels"/>.
    /// </remarks>
    [DataField]
    public ProtoId<RadioChannelPrototype>[] RadioChannels = [];

    /// <summary>
    /// Borg module types that are always available to borgs of this type.
    /// </summary>
    /// <remarks>
    /// These modules still work like modules, although they cannot be removed from the borg.
    /// </remarks>
    /// <seealso cref="BorgModuleComponent.DefaultModule"/>
    [DataField]
    public EntProtoId[] DefaultModules = [];

    /// <summary>
    /// Additional components to add to the borg entity when this type is selected.
    /// </summary>
    [DataField]
    public ComponentRegistry? AddComponents { get; set; }

    //
    // Visual information
    //

    /// <summary>
    /// The sprite state for the main borg body.
    /// </summary>
    [DataField]
    public string SpriteBodyState { get; set; } = "robot";

    /// <summary>
    /// An optional movement sprite state for the main borg body.
    /// </summary>
    [DataField]
    public string? SpriteBodyMovementState { get; set; }

    /// <summary>
    /// Sprite state used to indicate that the borg has a mind in it.
    /// </summary>
    /// <seealso cref="BorgChassisComponent.HasMindState"/>
    [DataField]
    public string SpriteHasMindState { get; set; } = "robot_e";

    /// <summary>
    /// Sprite state used to indicate that the borg has no mind in it.
    /// </summary>
    /// <seealso cref="BorgChassisComponent.NoMindState"/>
    [DataField]
    public string SpriteNoMindState { get; set; } = "robot_e_r";

    /// <summary>
    /// Sprite state used when the borg's flashlight is on.
    /// </summary>
    [DataField]
    public string SpriteToggleLightState { get; set; } = "robot_l";

    //
    // Minor information
    //

    /// <summary>
    /// String to use on petting success.
    /// </summary>
    /// <seealso cref="InteractionPopupComponent"/>
    [DataField]
    public string PetSuccessString { get; set; } = "petting-success-generic-cyborg";

    /// <summary>
    /// String to use on petting failure.
    /// </summary>
    /// <seealso cref="InteractionPopupComponent"/>
    [DataField]
    public string PetFailureString { get; set; } = "petting-failure-generic-cyborg";

    //
    // Sounds
    //

    /// <summary>
    /// Sound specifier for footstep sounds created by this borg.
    /// </summary>
    [DataField]
    public SoundSpecifier FootstepCollection { get; set; } = new SoundCollectionSpecifier(DefaultFootsteps);
}
