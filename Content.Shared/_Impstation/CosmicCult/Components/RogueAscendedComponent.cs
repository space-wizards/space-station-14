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
    /// The duration of our infection DoAfter.
    /// </summary>
    [DataField, AutoNetworkedField] public TimeSpan RogueInfectionTime = TimeSpan.FromSeconds(15);

    [DataField] public SoundSpecifier InfectionSfx = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/ability_nova_impact.ogg");
    [DataField] public SoundSpecifier ShatterSfx = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/ascendant_shatter.ogg");
    [DataField] public SoundSpecifier MobSound = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/ascendant_noise.ogg");
    [DataField] public EntProtoId Vfx = "CosmicGenericVFX";
    [DataField, AutoNetworkedField] public TimeSpan StunTime = TimeSpan.FromSeconds(7);
    public DamageSpecifier InfectionHeal = new()
    {
        DamageDict = new()
        {
            { "Blunt", 50},
            { "Slash", 50},
            { "Piercing", 50},
            { "Heat", 50},
            { "Shock", 50},
            { "Cold", 99},
            { "Poison", 50},
            { "Radiation", 50},
            { "Asphyxiation", 50}
        }
    };

}
