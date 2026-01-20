using Content.Shared.Changeling.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// Component responsible for Changelings Devour attack. Including the amount of damage
/// and how long it takes to devour someone
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(ChangelingDevourSystem))]
public sealed partial class ChangelingDevourComponent : Component
{
    /// <summary>
    /// The Action for devouring
    /// </summary>
    [DataField]
    public EntProtoId? ChangelingDevourAction = "ActionChangelingDevour";

    /// <summary>
    /// The action entity associated with devouring
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ChangelingDevourActionEntity;

    /// <summary>
    /// The whitelist of targets for devouring
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
    /// The Sound to use during consumption of a victim
    /// </summary>
    /// <remarks>
    /// 6 distance due to the default 15 being hearable all the way across PVS. Changeling is meant to be stealthy.
    /// 6 still allows the sound to be hearable, but not across an entire department.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? ConsumeNoise = new SoundCollectionSpecifier("ChangelingDevourConsume", AudioParams.Default.WithMaxDistance(6));

    /// <summary>
    /// The Sound to use during the windup before consuming a victim
    /// </summary>
    /// <remarks>
    /// 6 distance due to the default 15 being hearable all the way across PVS. Changeling is meant to be stealthy.
    /// 6 still allows the sound to be hearable, but not across an entire department.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? DevourWindupNoise = new SoundCollectionSpecifier("ChangelingDevourWindup", AudioParams.Default.WithMaxDistance(6));

    /// <summary>
    /// The time between damage ticks
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DamageTimeBetweenTicks = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The windup time before the changeling begins to engage in devouring the identity of a target
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DevourWindupTime = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The time it takes to FULLY consume someones identity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DevourConsumeTime = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Damage cap that a target is allowed to be caused due to IdentityConsumption
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DevourConsumeDamageCap = 350f;

    /// <summary>
    /// The Currently active devour sound in the world
    /// </summary>
    [DataField]
    public EntityUid? CurrentDevourSound;

    /// <summary>
    /// The damage profile for a single tick of devour damage
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier DamagePerTick = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Slash", 10},
            { "Piercing", 10 },
            { "Blunt", 5 },
        },
    };

    /// <summary>
    /// The list of protective damage types capable of preventing a devour if over the threshold
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<DamageTypePrototype>> ProtectiveDamageTypes = new()
    {
        "Slash",
        "Piercing",
        "Blunt",
    };

    /// <summary>
    /// The next Tick to deal damage on (utilized during the consumption "do-during" (a do after with an attempt event))
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextTick = TimeSpan.Zero;

    /// <summary>
    /// The percentage of ANY brute damage resistance that will prevent devouring
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DevourPreventionPercentageThreshold = 0.1f;

    public override bool SendOnlyToOwner => true;
}
