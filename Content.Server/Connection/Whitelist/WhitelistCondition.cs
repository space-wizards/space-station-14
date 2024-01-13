using System.Text.Json.Serialization;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Robust.Shared.Network;

namespace Content.Server.Connection.Whitelist;

[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class WhitelistCondition
{
    /// <summary>
    /// Function that checks if the player should be allowed to join.
    /// </summary>
    /// <param name="data"></param>
    /// <returns>
    /// A tuple with the first value being whether the player should be allowed to join and the second value being the
    /// reason why they should not be allowed to join.
    /// </returns>
    public abstract Task<(bool sucess, string denyReason)> Condition(NetUserData data);

    /// <summary>
    /// If this condition succeeds, the next conditions will be skipped.
    /// </summary>
    [DataField]
    public virtual bool BreakIfConditionSuccess { get; set; } = false;

    /// <summary>
    /// If this condition fails, the next conditions will be skipped and the player will be denied.
    /// </summary>
    [DataField]
    public virtual bool BreakIfConditionFail { get; set; } = true;
}
