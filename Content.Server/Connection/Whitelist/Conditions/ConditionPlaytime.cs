using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Shared.Network;

namespace Content.Server.Connection.Whitelist.Conditions;

public sealed partial class ConditionPlaytime : WhitelistCondition
{
    [DataField]
    public int MinimumPlaytime = 0; // In minutes
}
