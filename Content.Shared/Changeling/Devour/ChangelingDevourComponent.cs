using Content.Shared.Damage;
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

    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist = new()
    {
        Components = new[]
        {
            "MobState",
            "HumanoidAppearance"
        }
    };

    [DataField, AutoNetworkedField]
    public SoundSpecifier? ConsumeNoise = new SoundPathSpecifier("/Audio/Ambience/Antag/strawmeme.ogg");

    /// <summary>
    /// The windup time before the changeling begins to engage in devouring the identity of a target
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DevourWindupTime = 2f;

    /// <summary>
    /// The time it takes to FULLY consume someones identity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DevourConsumeTime = 10f;

    /// <summary>
    /// Damage cap that a target is allowed to be caused due to IdentityConsumption
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DevourConsumeDamageCap = 350f;

    [DataField, AutoNetworkedField]
    public EntityUid? CurrentDevourSound;

    [DataField, AutoNetworkedField]
    public DamageSpecifier DamagePerTick = new()
    {
        DamageDict = new()
        {
            { "Slash", 10},
            { "Piercing", 10 },
            { "Blunt", 5 }
        }
    };
    [DataField, AutoNetworkedField]
    public TimeSpan NextTick;
    /// <summary>
    /// The percentage of ANY brute damage resistance that will prevent devouring
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DevourPreventionPercentageThreshold = 0.1f;

}

