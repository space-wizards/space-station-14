using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Devour.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedDevourSystem))]
public sealed partial class DevourerComponent : Component
{
    [DataField]
    public ProtoId<EntityPrototype> DevourAction = "ActionDevour";

    [DataField]
    public EntityUid? DevourActionEntity;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public SoundSpecifier? SoundDevour = new SoundPathSpecifier("/Audio/Effects/demon_consume.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float DevourTime = 3f;

    /// <summary>
    /// The amount of time it takes to devour something
    /// <remarks>
    /// NOTE: original intended design was to increase this proportionally with damage thresholds, but those proved quite difficult to get consistently. right now it devours the structure at a fixed timer.
    /// </remarks>
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float StructureDevourTime = 10f;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public SoundSpecifier? SoundStructureDevour = new SoundPathSpecifier("/Audio/Machines/airlock_creaking.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    /// <summary>
    /// Where the entities go when it devours them, empties when it is butchered.
    /// </summary>
    public Container Stomach = default!;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public bool ShouldStoreDevoured = true;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public EntityWhitelist? Whitelist = new()
    {
        Components = new[]
        {
            "MobState",
        }
    };

    /// <summary>
    /// The favorite food not only feeds you, but also increases your passive healing.
    /// </summary>
    [DataField]
    public FoodPreference FoodPreference = FoodPreference.All;

    /// <summary>
    /// Passive healing added for each devoured favourite food.
    /// </summary>
    [DataField]
    public DamageSpecifier? PassiveDevourHealing = new();

    /// <summary>
    /// The passive damage done to devoured entities.
    /// </summary>
    [DataField]
    public DamageSpecifier? StomachDamage = new();

    /// <summary>
    /// The MobStates the stomach is allowed to deal damage on.
    /// </summary>
    [DataField]
    public List<MobState> DigestibleStates = new ();
}
