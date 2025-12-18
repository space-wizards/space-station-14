using System.Net;
using System.Threading.Tasks;
using Content.Shared.Database;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Managers;

public interface IBanManager
{
    public void Initialize();
    public void Restart();

    /// <summary>
    /// Bans the specified target, address range and / or HWID. One of them must be non-null
    /// </summary>
    /// <param name="target">Target user, username or GUID, null for none</param>
    /// <param name="banningAdmin">The person who banned our target</param>
    /// <param name="addressRange">Address range, null for none</param>
    /// <param name="hwid">H</param>
    /// <param name="minutes">Number of minutes to ban for. 0 and null mean permanent</param>
    /// <param name="severity">Severity of the resulting ban note</param>
    /// <param name="reason">Reason for the ban</param>
    public void CreateServerBan(NetUserId? target, string? targetUsername, NetUserId? banningAdmin, (IPAddress, int)? addressRange, ImmutableTypedHwid? hwid, uint? minutes, NoteSeverity severity, string reason);

    /// <summary>
    /// Gets a list of prefixed prototype IDs with the player's role bans.
    /// </summary>
    public HashSet<string>? GetRoleBans(NetUserId playerUserId);

    /// <summary>
    /// Checks if the player is currently banned from any of the listed roles.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="antags">A list of valid antag prototype IDs.</param>
    /// <returns>Returns True if an active role ban is found for this player for any of the listed roles.</returns>
    public bool IsRoleBanned(ICommonSession player, List<ProtoId<AntagPrototype>> antags);

    /// <summary>
    /// Checks if the player is currently banned from any of the listed roles.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="jobs">A list of valid job prototype IDs.</param>
    /// <returns>Returns True if an active role ban is found for this player for any of the listed roles.</returns>
    public bool IsRoleBanned(ICommonSession player, List<ProtoId<JobPrototype>> jobs);

    /// <summary>
    /// Gets a list of prototype IDs with the player's job bans.
    /// </summary>
    public HashSet<ProtoId<JobPrototype>>? GetJobBans(NetUserId playerUserId);

    /// <summary>
    /// Gets a list of prototype IDs with the player's antag bans.
    /// </summary>
    public HashSet<ProtoId<AntagPrototype>>? GetAntagBans(NetUserId playerUserId);

    /// <summary>
    /// Creates a job ban for the specified target, username or GUID
    /// </summary>
    /// <param name="target">Target user, username or GUID, null for none</param>
    /// <param name="targetUsername">The username of the target, if known</param>
    /// <param name="banningAdmin">The responsible admin for the ban</param>
    /// <param name="addressRange">The range of IPs that are to be banned, if known</param>
    /// <param name="hwid">The HWID to be banned, if known</param>
    /// <param name="role">The role ID to be banned from. Either an AntagPrototype or a JobPrototype</param>
    /// <param name="minutes">Number of minutes to ban for. 0 and null mean permanent</param>
    /// <param name="severity">Severity of the resulting ban note</param>
    /// <param name="reason">Reason for the ban</param>
    /// <param name="timeOfBan">Time when the ban was applied, used for grouping role bans</param>
    public void CreateRoleBan<T>(
        NetUserId? target,
        string? targetUsername,
        NetUserId? banningAdmin,
        (IPAddress, int)? addressRange,
        ImmutableTypedHwid? hwid,
        ProtoId<T> role,
        uint? minutes,
        NoteSeverity severity,
        string reason,
        DateTimeOffset timeOfBan
    ) where T : class, IPrototype;

    /// <summary>
    /// Pardons a role ban for the specified target, username or GUID
    /// </summary>
    /// <param name="banId">The id of the role ban to pardon.</param>
    /// <param name="unbanningAdmin">The admin, if any, that pardoned the role ban.</param>
    /// <param name="unbanTime">The time at which this role ban was pardoned.</param>
    public Task<string> PardonRoleBan(int banId, NetUserId? unbanningAdmin, DateTimeOffset unbanTime);

    /// <summary>
    /// Sends role bans to the target
    /// </summary>
    /// <param name="pSession">Player's session</param>
    public void SendRoleBans(ICommonSession pSession);
}
