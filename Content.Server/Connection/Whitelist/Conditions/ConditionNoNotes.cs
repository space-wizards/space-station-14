using System.Linq;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.Database;
using Robust.Shared.Network;

namespace Content.Server.Connection.Whitelist.Conditions;

public sealed partial class ConditionNoNotes : WhitelistCondition
{
    public bool IncludeExpired = false;

    public NoteSeverity MinimumSeverity  = NoteSeverity.Minor;

    /// <summary>
    /// Range in days to check for notes.
    /// </summary>
    public int Range = int.MaxValue;

    public override async Task<bool> Condition(NetUserData data)
    {
        var db = IoCManager.Resolve<IServerDbManager>();
        var remarks = await db.GetAllAdminRemarks(data.UserId.UserId);
        var notes = new List<AdminNote>();
        foreach (var remark in remarks)
        {
            var note = await db.GetAdminNote(remark.Id);
            if (note is not null)
            {
                notes.Add(note);
            }
        }

        if (!IncludeExpired)
        {
            notes = notes.Where(n => n.ExpirationTime is null || n.ExpirationTime > DateTime.Now).ToList();
        }

        notes = notes.Where(n => n.CreatedAt > DateTime.Now.AddDays(-Range)).ToList();
        return notes.All(n => n.Severity < MinimumSeverity);
    }

    public override string DenyMessage { get; } = "whitelist-too-many-notes";
}
