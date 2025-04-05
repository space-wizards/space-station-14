using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Changeling.Devour;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedChangelingDevourSystem))]
public sealed partial class ChangelingDevourComponent : Component
{
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? ChangelingDevourAction = "ActionChangelingDevour";

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
            "HumanoidAppearance",
        ],
    };

    /// <summary>
    /// The Sound to use during consumption of a victim
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? ConsumeNoise = new SoundCollectionSpecifier("ChangelingDevourConsume");

    /// <summary>
    /// The Sound to use during the windup before consuming a victim
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? DevourWindupNoise = new SoundCollectionSpecifier("ChangelingDevourWindup");

    /// <summary>
    /// The time between damage ticks
    /// </summary>
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
    [DataField, AutoNetworkedField]
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
    /// The next Tick to deal damage on (utilized during the consumption "do-during" (a do after with an attempt event))
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan NextTick = TimeSpan.Zero;

    /// <summary>
    /// The percentage of ANY brute damage resistance that will prevent devouring
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DevourPreventionPercentageThreshold = 0.1f;

}

