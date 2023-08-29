using Robust.Shared.Audio;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Melee;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class InfectOnMeleeComponent : Component
{
    /// <summary>
    /// infection chance determines the % chance that target will be infected.
    /// </summary>

    [ViewVariables(VVAccess.ReadWrite)]
    public float InfectionChance = 0.15f;

    /// <summary>
    /// If this is true then target will be cluwned.
    /// </summary>

    [DataField("cluwnification")]
    public bool Cluwinification = false;

    /// <summary>
    /// Sound played on infection.
    /// </summary>
 
    [DataField("infectionSound")]
    public SoundSpecifier InfectionSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/Magic/staff_animation.ogg");
}
