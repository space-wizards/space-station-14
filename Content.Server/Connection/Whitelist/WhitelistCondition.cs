using System.Text.Json.Serialization;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Robust.Shared.Network;

namespace Content.Server.Connection.Whitelist;

[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class WhitelistCondition
{
    public abstract Task<bool> Condition(NetUserData data);

    /// <summary>
    /// If this condition fails, this message will be sent to the client.
    /// </summary>
    public abstract string DenyMessage { get; }

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
