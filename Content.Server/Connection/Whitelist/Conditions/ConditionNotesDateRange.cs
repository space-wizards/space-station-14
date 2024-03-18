using System.Linq;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.Database;
using Robust.Shared.Network;

namespace Content.Server.Connection.Whitelist.Conditions;

public sealed partial class ConditionNotesDateRange : WhitelistCondition
{
    [DataField]
    public bool IncludeExpired = false;

    [DataField]
    public NoteSeverity MinimumSeverity  = NoteSeverity.Minor;

    /// <summary>
    /// Range in days to check for notes.
    /// </summary>
    [DataField]
    public int Range = int.MaxValue;

    [DataField]
    public bool IncludeSecret = false;
}
