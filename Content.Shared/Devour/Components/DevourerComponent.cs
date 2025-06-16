using Content.Shared.Chemistry.Reagent;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Devour.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedDevourSystem))]
public sealed partial class DevourerComponent : Component
{
    [DataField("devourAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? DevourAction = "ActionDevour";

    [DataField("devourActionEntity")]
    public EntityUid? DevourActionEntity;

    [ViewVariables(VVAccess.ReadWrite), DataField("soundDevour")]
    public SoundSpecifier? SoundDevour = new SoundPathSpecifier("/Audio/Effects/demon_consume.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    [DataField("devourTime")]
    public float DevourTime = 3f;

    /// <summary>
    /// The amount of time it takes to devour something
    /// <remarks>
    /// NOTE: original intended design was to increase this proportionally with damage thresholds, but those proved quite difficult to get consistently. right now it devours the structure at a fixed timer.
    /// </remarks>
    /// </summary>
    [DataField("structureDevourTime")]
    public float StructureDevourTime = 10f;

    [ViewVariables(VVAccess.ReadWrite), DataField("soundStructureDevour")]
    public SoundSpecifier? SoundStructureDevour = new SoundPathSpecifier("/Audio/Machines/airlock_creaking.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    /// <summary>
    /// Where the entities go when it devours them, empties when it is butchered.
    /// </summary>
    public Container Stomach = default!;

    [ViewVariables(VVAccess.ReadWrite), DataField("shouldStoreDevoured")]
    public bool ShouldStoreDevoured = true;

    /// <summary>
    /// Determines what things the devourer can consume.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("whitelist")]
    public EntityWhitelist? Whitelist = new()
    {
        Components = new[]
    {
            "MobState",
        }
    };

    /// <summary>
    /// Determines what things end up in the dragon's stomach if they eat it.
    /// If it isn't in the whitelist, it's deleted.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("stomachStorageWhitelist")]
    public EntityWhitelist? StomachStorageWhitelist = new()
    {
        Components = new[]
        {
            "MobState",
        }
    };

    /// <summary>
    /// Determine's the dragon's food preference.  If the eaten thing matches,
    /// it is rewarded with the reward chemical.  If null, all food is fine.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("foodPreferenceWhitelist")]
    public EntityWhitelist? FoodPreferenceWhitelist = new()
    {
        Components = new[]
        {
            "HumanoidAppearance",
        }
    };

    /// <summary>
    /// The chemical ID injected upon devouring
    /// </summary>
    [DataField("chemical", customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>))]
    public string Chemical = "Ichor";

    /// <summary>
    /// The amount of ichor injected per devour
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("healRate")]
    public float HealRate = 15f;

}

