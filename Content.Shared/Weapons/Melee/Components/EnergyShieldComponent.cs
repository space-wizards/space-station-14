using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Melee.EnergyShield;

[RegisterComponent, NetworkedComponent]
public sealed class ItemToggleComponent : Component
{
    public bool Activated = false;

    [DataField("isSharp")]
    public bool IsSharp = true;

    /// <summary>
    ///     Does this become hidden when deactivated
    /// </summary>
    [DataField("secret")]
    public bool Secret { get; set; } = false;

    [DataField("activateSound")]
    public SoundSpecifier ActivateSound { get; set; } = default!;

    [DataField("deActivateSound")]
    public SoundSpecifier DeActivateSound { get; set; } = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("activatedDisarmMalus")]
    public float ActivatedDisarmMalus = 0.6f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("offSize")]
    public int OffSize = 1;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("onSize")]
    public int OnSize = 9999;
}

[ByRefEvent]
public readonly record struct EnergyShieldActivatedEvent();

[ByRefEvent]
public readonly record struct EnergyShieldDeactivatedEvent();
