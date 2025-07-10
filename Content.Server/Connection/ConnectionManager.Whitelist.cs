using System.Linq;
using System.Threading.Tasks;
using Content.Server.Connection.Whitelist;
using Content.Server.Connection.Whitelist.Conditions;
using Content.Server.Database;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Shared.Network;

namespace Content.Server.Connection;

/// <summary>
/// Handles whitelist conditions for incoming connections.
/// </summary>
public sealed partial class ConnectionManager
{
    private PlayerConnectionWhitelistPrototype[]? _whitelists;

    private void InitializeWhitelist()
    {
        _cfg.OnValueChanged(CCVars.WhitelistPrototypeList, UpdateWhitelists, true);
    }

    private void UpdateWhitelists(string s)
    {
        var list = new List<PlayerConnectionWhitelistPrototype>();
        foreach (var id in s.Split(','))
        {
            if (_prototypeManager.TryIndex(id, out PlayerConnectionWhitelistPrototype? prototype))
            {
                list.Add(prototype);
            }
            else
            {
                _sawmill.Fatal($"Whitelist prototype {id} does not exist. Denying all connections.");
                _whitelists = null; // Invalidate the list, causes deny on all connections.
                return;
            }
        }

        _whitelists = list.ToArray();
    }

    private bool IsValid(PlayerConnectionWhitelistPrototype whitelist, int playerCount)
    {
        return playerCount >= whitelist.MinimumPlayers && playerCount <= whitelist.MaximumPlayers;
    }

    public async Task<(bool isWhitelisted, string? denyMessage)> IsWhitelisted(PlayerConnectionWhitelistPrototype whitelist, NetUserData data, ISawmill sawmill)
    {
        var cacheRemarks = await _db.GetAllAdminRemarks(data.UserId);
        var cachePlaytime = await _db.GetPlayTimes(data.UserId);

        foreach (var condition in whitelist.Conditions)
        {
            bool matched;
            string denyMessage;
            switch (condition)
            {
                case ConditionAlwaysMatch:
                    matched = true;
                    denyMessage = Loc.GetString("whitelist-always-deny");
                    break;
                case ConditionManualWhitelistMembership:
                    matched = await CheckConditionManualWhitelist(data);
                    denyMessage = Loc.GetString("whitelist-manual");
                    break;
                case ConditionManualBlacklistMembership:
                    matched = await CheckConditionManualBlacklist(data);
                    denyMessage = Loc.GetString("whitelist-blacklisted");
                    break;
                case ConditionNotesDateRange conditionNotes:
                    matched = CheckConditionNotesDateRange(conditionNotes, cacheRemarks);
                    denyMessage = Loc.GetString("whitelist-notes");
                    break;
                case ConditionPlayerCount conditionPlayerCount:
                    matched = CheckConditionPlayerCount(conditionPlayerCount);
                    denyMessage = Loc.GetString("whitelist-player-count");
                    break;
                case ConditionPlaytime conditionPlaytime:
                    matched = CheckConditionPlaytime(conditionPlaytime, cachePlaytime);
                    denyMessage = Loc.GetString("whitelist-playtime", ("minutes", conditionPlaytime.MinimumPlaytime));
                    break;
                case ConditionNotesPlaytimeRange conditionNotesPlaytimeRange:
                    matched = CheckConditionNotesPlaytimeRange(conditionNotesPlaytimeRange, cacheRemarks, cachePlaytime);
                    denyMessage = Loc.GetString("whitelist-notes");
                    break;
                default:
                    throw new NotImplementedException($"Whitelist condition {condition.GetType().Name} not implemented");
            }

            sawmill.Verbose($"User {data.UserName} whitelist condition {condition.GetType().Name} result: {matched}");
            sawmill.Verbose($"Action: {condition.Action.ToString()}");
            switch (condition.Action)
            {
                case ConditionAction.Allow:
                    if (matched)
                    {
                        sawmill.Verbose($"User {data.UserName} passed whitelist condition {condition.GetType().Name} and it's a breaking condition");
                        return (true, denyMessage);
                    }
                    break;
                case ConditionAction.Deny:
                    if (matched)
                    {
                        sawmill.Verbose($"User {data.UserName} failed whitelist condition {condition.GetType().Name}");
                        return (false, denyMessage);
                    }
                    break;
                default:
                    sawmill.Verbose($"User {data.UserName} failed whitelist condition {condition.GetType().Name} but it's not a breaking condition");
                    break;
            }
        }
        sawmill.Verbose($"User {data.UserName} passed all whitelist conditions");
        return (true, null);
    }

    #region Condition Checking

    private async Task<bool> CheckConditionManualWhitelist(NetUserData data)
    {
        return await _db.GetWhitelistStatusAsync(data.UserId);
    }

    private async Task<bool> CheckConditionManualBlacklist(NetUserData data)
    {
        return await _db.GetBlacklistStatusAsync(data.UserId);
    }

    private bool CheckConditionNotesDateRange(ConditionNotesDateRange conditionNotes, List<IAdminRemarksRecord> remarks)
    {
        var range = DateTime.UtcNow.AddDays(-conditionNotes.Range);

        return CheckRemarks(remarks,
            conditionNotes.IncludeExpired,
            conditionNotes.IncludeSecret,
            conditionNotes.MinimumSeverity,
            conditionNotes.MinimumNotes,
            adminRemarksRecord => adminRemarksRecord.CreatedAt > range);
    }

    private bool CheckConditionPlayerCount(ConditionPlayerCount conditionPlayerCount)
    {
        var count = _plyMgr.PlayerCount;
        return count >= conditionPlayerCount.MinimumPlayers && count <= conditionPlayerCount.MaximumPlayers;
    }

    private bool CheckConditionPlaytime(ConditionPlaytime conditionPlaytime, List<PlayTime> playtime)
    {
        var tracker = playtime.Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall);
        if (tracker is null)
        {
            return false;
        }

        return tracker.TimeSpent.TotalMinutes >= conditionPlaytime.MinimumPlaytime;
    }

    private bool CheckConditionNotesPlaytimeRange(
        ConditionNotesPlaytimeRange conditionNotesPlaytimeRange,
        List<IAdminRemarksRecord> remarks,
        List<PlayTime> playtime)
    {
        var overallTracker = playtime.Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall);
        if (overallTracker is null)
        {
            return false;
        }

        return CheckRemarks(remarks,
            conditionNotesPlaytimeRange.IncludeExpired,
            conditionNotesPlaytimeRange.IncludeSecret,
            conditionNotesPlaytimeRange.MinimumSeverity,
            conditionNotesPlaytimeRange.MinimumNotes,
            adminRemarksRecord => adminRemarksRecord.PlaytimeAtNote >= overallTracker.TimeSpent - TimeSpan.FromMinutes(conditionNotesPlaytimeRange.Range));
    }

    private bool CheckRemarks(List<IAdminRemarksRecord> remarks, bool includeExpired, bool includeSecret, NoteSeverity minimumSeverity, int MinimumNotes, Func<IAdminRemarksRecord, bool> additionalCheck)
    {
        var utcNow = DateTime.UtcNow;

        var notes = remarks.Count(r => r is AdminNoteRecord note && note.Severity >= minimumSeverity && (includeSecret || !note.Secret) && (includeExpired || note.ExpirationTime == null || note.ExpirationTime > utcNow));
        if (notes < MinimumNotes)
        {
            return false;
        }

        foreach (var adminRemarksRecord in remarks)
        {
            // If we're not including expired notes, skip them
            if (!includeExpired && (adminRemarksRecord.ExpirationTime == null || adminRemarksRecord.ExpirationTime <= utcNow))
                continue;

            // In order to get the severity of the remark, we need to see if it's an AdminNoteRecord.
            if (adminRemarksRecord is not AdminNoteRecord adminNoteRecord)
                continue;

            // We want to filter out secret notes if we're not including them.
            if (!includeSecret && adminNoteRecord.Secret)
                continue;

            // At this point, we need to remove the note if it's not within the severity range.
            if (adminNoteRecord.Severity < minimumSeverity)
                continue;

            // Perform the additional check specific to each method
            if (!additionalCheck(adminRemarksRecord))
                continue;

            // If we've made it this far, we have a match
            return true;
        }

        // No matches
        return false;
    }

    #endregion
}
