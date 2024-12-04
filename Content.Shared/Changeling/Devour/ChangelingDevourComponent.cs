using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Damage;
using Content.Shared.Devour;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Changeling.Devour;


[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedChangelingDevourSystem))]
public sealed partial class ChangelingDevourComponent : Component
{
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? ChangelingDevourAction = "ActionChangelingDevour";

    [DataField]
    public EntityUid? ChangelingDevourActionEntity;

    [DataField]
    public EntityWhitelist? Whitelist = new()
    {
        Components = new[]
        {
            "MobState",
        }
    };

    public SoundSpecifier? ConsumeTickNoise = new SoundPathSpecifier("/Audio/Ambience/Antag/strawmeme.ogg");

    /// <summary>
    /// The windup time before the changeling begins to engage in devouring the identity of a target
    /// </summary>
    [DataField]
    public float DevourWindupTime = 2f;

    /// <summary>
    /// The time it takes to FULLY consume someones identity.
    /// </summary>
    [DataField]
    public float DevourConsumeTime = 10f;

    [DataField]
    public int ConsumeTicksToComplete = 10;
    /// <summary>
    /// Damage cap that a target is allowed to be caused due to IdentityConsumption
    /// </summary>
    [DataField]
    public float DevourConsumeDamageCap = 350f;

    [DataField]
    public ChangelingDevourWindupDoAfterEvent? CurrentWindupEvent = null;
    [DataField]
    public ChangelingDevourConsumeDoAfterEvent? CurrentDevourEvent = null;
    [DataField]
    public EntityUid? CurrentDevourSound = null;

    [DataField]
    public DamageSpecifier DamagePerTick = new()
    {
        DamageDict = new()
        {
            { "Slash", 10},
            { "Piercing", 10 },
            { "Blunt", 5 }
        }
    };

    [DataField]
    public TimeSpan NextTick;
    /// <summary>
    /// The percentage of ANY brute damage resistance that will prevent devouring
    /// </summary>
    [DataField]
    public float DevourPreventionPercentageThreshold = 10f;

}

