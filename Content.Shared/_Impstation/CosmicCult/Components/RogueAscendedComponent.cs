using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.CosmicCult.Components;

/// <summary>
/// Component to designate a mob as a rogue astral ascendant.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class RogueAscendedComponent : Component
{
    /// <summary>
    /// The duration of our slumber DoAfter.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan RogueSlumberDoAfterTime = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The duration of our infection DoAfter.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan RogueInfectionDoAfterTime = TimeSpan.FromSeconds(8);

    /// <summary>
    /// The duration inflicted by Slumber Shell
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan RogueSlumberTime = TimeSpan.FromSeconds(25);

    [DataField]
    public SoundSpecifier InfectionSfx = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/ability_nova_impact.ogg");

    [DataField]
    public SoundSpecifier ShatterSfx = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/ascendant_shatter.ogg");

    [DataField]
    public SoundSpecifier MobSound = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/ascendant_noise.ogg");

    [DataField]
    public EntProtoId Vfx = "CosmicGenericVFX";

    [DataField, AutoNetworkedField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(7);
    public DamageSpecifier InfectionHeal = new()
    {
        DamageDict = new()
        {
            { "Blunt", 25},
            { "Slash", 25},
            { "Piercing", 25},
            { "Heat", 25},
            { "Shock", 25},
            { "Cold", 25},
            { "Poison", 25},
            { "Radiation", 25},
            { "Asphyxiation", 25}
        }
    };

}
