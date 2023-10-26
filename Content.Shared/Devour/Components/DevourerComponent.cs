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

    [ViewVariables(VVAccess.ReadWrite), DataField("whitelist")]
    public EntityWhitelist? Whitelist = new()
    {
        Components = new[]
        {
            "MobState",
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

    /// <summary>
    /// The favorite food not only feeds you, but also heals
    /// </summary>
    [DataField("foodPreference")]
    public FoodPreference FoodPreference = FoodPreference.All;
}

