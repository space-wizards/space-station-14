using Content.Shared.Chemistry.Reagent;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Devour.Components;

/// <summary>
/// Allows an entity to eat whitelisted entities via an action.
/// Eaten mobs will be stored inside a container and released when the devourer is gibbed.
/// Eating something that fits their food preference will reward the devourer by being injected with a specific reagent.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(DevourSystem))]
public sealed partial class DevourerComponent : Component
{
    /// <summary>
    /// Action prototype for devouring.
    /// </summary>
    [DataField]
    public EntProtoId DevourAction = "ActionDevour";

    /// <summary>
    /// The spawned action entity for devouring.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? DevourActionEntity;

    /// <summary>
    /// The amount of time it takes to devour a mob.
    /// <remarks>
    [DataField, AutoNetworkedField]
    public float DevourTime = 3f;

    /// <summary>
    /// The amount of time it takes to devour a structure.
    /// <remarks>
    /// NOTE: original intended design was to increase this proportionally with damage thresholds, but those proved quite difficult to get consistently. right now it devours the structure at a fixed timer.
    /// </remarks>
    /// </summary>
    [DataField, AutoNetworkedField]
    public float StructureDevourTime = 10f;

    /// <summary>
    /// The sound to play when finishing devouring something.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundDevour = new SoundPathSpecifier("/Audio/Effects/demon_consume.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    /// <summary>
    /// The sound to play when starting to devour a structure.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundStructureDevour = new SoundPathSpecifier("/Audio/Machines/airlock_creaking.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    /// <summary>
    /// The container to store the eaten entities in.
    /// </summary>
    [ViewVariables]
    public static string StomachContainerId = "stomach";

    /// <summary>
    /// Where the entities go when it devours them, empties when it is butchered.
    /// </summary>
    [ViewVariables]
    public Container Stomach = default!;

    /// <summary>
    /// Determines what things the devourer can consume.
    /// </summary>
    [DataField, AutoNetworkedField]
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
    [DataField, AutoNetworkedField]
    public EntityWhitelist? StomachStorageWhitelist;

    /// <summary>
    /// Determine's the dragon's food preference. If the eaten thing matches,
    /// it is rewarded with the reward chemical. If null, all food is fine.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? FoodPreferenceWhitelist;

    /// <summary>
    /// The chemical ID injected upon devouring.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ReagentPrototype> Chemical = "Ichor";

    /// <summary>
    /// The amount of solution injected per devour.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HealRate = 15f;

}

