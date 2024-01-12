using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Robust.Shared.Network;

namespace Content.Server.Connection.Whitelist;

[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class WhitelistCondition
{
    public abstract bool Condition(NetUserData data);

    /// <summary>
    /// If this condition fails, this message will be sent to the client.
    /// </summary>
    public abstract string DenyMessage { get; }
}
