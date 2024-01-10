using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.EnergyDome;

[RegisterComponent, Access(typeof(EnergyDomeSystem))] //Access add
public sealed partial class EnergyDomeComponent : Component
{
    /// <summary>
    /// A linked generator that uses energy
    /// </summary>
    [DataField]
    public EntityUid? Generator;

    /// <summary>
    /// How much energy will be spent from the battery per unit of damage taken by the shield.
    /// </summary>
    [DataField]
    public float EnergyLessForDamage = 50f;

    [DataField]
    public SoundSpecifier ParrySound = new SoundPathSpecifier("/Audio/Machines/energyshield_parry.ogg");
}
