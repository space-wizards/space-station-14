using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.CosmicCult.Components;

[RegisterComponent, AutoGenerateComponentState]
[Access(typeof(DeconversionSystem))]
public sealed partial class CleanseOnUseComponent : Component
{
    [DataField] public TimeSpan UseTime = TimeSpan.FromSeconds(10);

    [DataField] public SoundSpecifier SizzleSound = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");

    [DataField] public SoundSpecifier CleanseSound = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/cleanse_deconversion.ogg");
    [DataField] public SoundSpecifier MalignSound = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/glyph_trigger.ogg");

    [DataField] public EntProtoId CleanseVFX = "CleanseEffectVFX";

    [DataField] public EntProtoId MalignVFX = "CosmicGenericVFX";

    [DataField] public bool Enabled = true;

    /// <summary>
    /// When True allows an item to purge the Cosmic Cult's Malign Rifts onInteractInHand, utilized exclusively by the CosmicRiftSystem.
    /// </summary>
    [DataField] public bool CanPurge = false;
    [DataField, AutoNetworkedField]
    public DamageSpecifier SelfDamage = new()
    {
        DamageDict = new() {
            { "Caustic", 15 }
        }
    };

}
