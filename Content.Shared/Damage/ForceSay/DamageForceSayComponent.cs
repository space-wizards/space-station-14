using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.Damage.ForceSay;

/// <summary>
/// This is used for forcing clients to send messages with a suffix attached (like -GLORF) when taking large amounts
/// of damage, or things like entering crit or being stunned.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DamageForceSayComponent : Component
{
    /// <summary>
    ///     The localization string that the message & suffix will be passed into
    /// </summary>
    [DataField]
    public LocId ForceSayMessageWrap = "damage-force-say-message-wrap";

    /// <summary>
    ///     Same as <see cref="ForceSayMessageWrap"/> but for cases where no suffix is used,
    ///     such as when going into crit.
    /// </summary>
    [DataField]
    public LocId ForceSayMessageWrapNoSuffix = "damage-force-say-message-wrap-no-suffix";

    /// <summary>
    ///     The fluent string prefix to use when picking a random suffix
    /// </summary>
    [DataField]
    public string ForceSayStringPrefix = "damage-force-say-";

    /// <summary>
    ///     The number of suffixes that exist for use with <see cref="ForceSayStringPrefix"/>.
    ///     i.e. (prefix)-1 through (prefix)-(count)
    /// </summary>
    [DataField]
    public int ForceSayStringCount = 7;

    /// <summary>
    ///     The amount of total damage between <see cref="ValidDamageGroups"/> that needs to be taken before
    ///     a force say occurs.
    /// </summary>
    [DataField]
    public FixedPoint2 DamageThreshold = FixedPoint2.New(10);

    /// <summary>
    ///     A list of damage group types that are considered when checking <see cref="DamageThreshold"/>.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<DamageGroupPrototype>>? ValidDamageGroups = new()
    {
        "Brute",
        "Burn",
    };

    /// <summary>
    ///     The time enforced between force says to avoid spam.
    /// </summary>
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(5.0);

    public TimeSpan? NextAllowedTime = null;
}
