using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Database;

namespace Content.Server.Database
{
    public record ServerRoleBanNote(int Id, int? RoundId, Round? Round, Guid? PlayerUserId, Player? Player,
        TimeSpan PlaytimeAtNote, string Message, NoteSeverity Severity, Player? CreatedBy, DateTime CreatedAt,
        Player? LastEditedBy, DateTime? LastEditedAt, DateTime? ExpirationTime, bool Deleted, string[] Roles,
        Player? UnbanningAdmin, DateTime? UnbanTime) : IAdminRemarksCommon;
}
