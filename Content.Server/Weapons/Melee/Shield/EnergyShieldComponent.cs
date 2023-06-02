using Robust.Shared.Audio;

namespace Content.Server.Weapons.Melee.EnergyShield;

[RegisterComponent]
internal sealed class EnergyShieldComponent : Component
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

    [DataField("litDisarmMalus")]
    public float LitDisarmMalus = 0.6f;
}

[ByRefEvent]
public readonly record struct EnergyShieldActivatedEvent();

[ByRefEvent]
public readonly record struct EnergyShieldDeactivatedEvent();
