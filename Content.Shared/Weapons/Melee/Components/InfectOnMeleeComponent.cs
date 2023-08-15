using Robust.Shared.Audio;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Melee;

[RegisterComponent]
[NetworkedComponent]
public sealed class InfectOnMeleeComponent : Component
{

    [ViewVariables(VVAccess.ReadWrite)]
    public float Cluwinification = 0.15f;

    /// <summary>
    /// If this is true then has a % chance to transform target into a cluwne when hit with melee.
    /// </summary>
    [DataField("infectOnMelee")]
    public bool InfectOnMelee = true;

    [DataField("infectionSound")]
    public SoundSpecifier InfectionSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/Magic/staff_animation.ogg");
}
