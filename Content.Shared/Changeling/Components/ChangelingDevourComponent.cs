using Content.Shared.Changeling.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// Component responsible for Changelings Devour attack. Including the amount of damage
/// and how long it takes to devour someone
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ChangelingDevourSystem))]
public sealed partial class ChangelingDevourComponent : Component
{
    /// <summary>
    /// The action for devouring.
    /// </summary>
    [DataField]
    public EntProtoId? ChangelingDevourAction = "ActionChangelingDevour";

    /// <summary>
    /// The action entity associated with devouring.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ChangelingDevourActionEntity;

    /// <summary>
    /// The whitelist of targets for devouring.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist = new()
    {
        Components =
        [
            "MobState",
            "HumanoidProfile",
        ],
    };

    /// <summary>
    /// The sound to use during consumption of a victim.
    /// </summary>
    /// <remarks>
    /// 6 distance due to the default 15 being hearable all the way across PVS. Changeling is meant to be stealthy.
    /// 6 still allows the sound to be hearable, but not across an entire department.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? ConsumeNoise = new SoundCollectionSpecifier("ChangelingDevourConsume", AudioParams.Default.WithMaxDistance(6));

    /// <summary>
    /// The sound to use during the windup before consuming a victim.
    /// </summary>
    /// <remarks>
    /// 6 distance due to the default 15 being hearable all the way across PVS. Changeling is meant to be stealthy.
    /// 6 still allows the sound to be hearable, but not across an entire department.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? DevourWindupNoise = new SoundCollectionSpecifier("ChangelingDevourWindup", AudioParams.Default.WithMaxDistance(6));

    /// <summary>
    /// The windup time before the changeling begins to engage in devouring the identity of a target.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DevourWindupTime = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The time it takes to consume someones identity.
    /// Starts after the windup.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DevourConsumeTime = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The currently active devour sound in the world.
    /// </summary>
    [DataField]
    public EntityUid? CurrentDevourSound;

    /// <summary>
    /// The damage dealt after the windup finished and devouring started.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier WindupDamage = new()
    {
        DamageDict = new()
        {
            { "Slash", 10},
            { "Piercing", 10 },
            { "Blunt", 5 },
        },
    };

    /// <summary>
    /// The damage dealt after the devouring is fully finished.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier DevourDamage = new()
    {
        DamageDict = new()
        {
            { "Slash", 20},
            { "Piercing", 20 },
            { "Blunt", 10 },
        },
    };

    /// <summary>
    /// The list of protective damage types capable of preventing a devour if over the threshold.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<DamageTypePrototype>> ProtectiveDamageTypes = new()
    {
        "Slash",
        "Piercing",
        "Blunt",
    };

    /// <summary>
    /// The percentage of ANY brute damage resistance that will prevent devouring.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DevourPreventionPercentageThreshold = 0.1f;

    public override bool SendOnlyToOwner => true;
}
