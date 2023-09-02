using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Server.Weapons.Melee.EnergySword;

[RegisterComponent]
internal sealed partial class EnergySwordComponent : Component
{
    public Color BladeColor = Color.DodgerBlue;

    public bool Hacked = false;

    public bool Activated = false;

    [DataField("isSharp")]
    public bool IsSharp = true;

    /// <summary>
    ///     Does this become hidden when deactivated
    /// </summary>
    [DataField("secret")]
    public bool Secret { get; set; } = false;

    /// <summary>
    ///     RGB cycle rate for hacked e-swords.
    /// </summary>
    [DataField("cycleRate")]
    public float CycleRate = 1f;

    [DataField("activateSound")]
    public SoundSpecifier ActivateSound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/ebladeon.ogg");

    [DataField("deActivateSound")]
    public SoundSpecifier DeActivateSound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/ebladeoff.ogg");

    [DataField("onHitOn")]
    public SoundSpecifier OnHitOn { get; set; } = new SoundPathSpecifier("/Audio/Weapons/eblade1.ogg");

    [DataField("onHitOff")]
    public SoundSpecifier OnHitOff { get; set; } = new SoundPathSpecifier("/Audio/Weapons/genhit1.ogg");

    [DataField("colorOptions")]
    public List<Color> ColorOptions = new()
    {
        Color.Tomato,
        Color.DodgerBlue,
        Color.Aqua,
        Color.MediumSpringGreen,
        Color.MediumOrchid
    };

    [DataField("litDamageBonus")]
    public DamageSpecifier LitDamageBonus = new();

    [DataField("litDisarmMalus")]
    public float LitDisarmMalus = 0.6f;
}

[ByRefEvent]
public readonly record struct EnergySwordActivatedEvent();

[ByRefEvent]
public readonly record struct EnergySwordDeactivatedEvent();
