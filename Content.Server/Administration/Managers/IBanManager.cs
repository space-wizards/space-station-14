using System.Collections.Immutable;
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
    public void CreateServerBan(NetUserId? target, string? targetUsername, NetUserId? banningAdmin, (IPAddress, int)? addressRange, ImmutableArray<byte>? hwid, uint? minutes, NoteSeverity severity, string reason);
    public HashSet<string>? GetRoleBans(NetUserId playerUserId);
    public HashSet<ProtoId<JobPrototype>>? GetJobBans(NetUserId playerUserId);

    /// <summary>
    /// Retrieves a set of antagonist bans for the specified player.
    /// </summary>
    /// <param name="playerUserId">The user ID of the player.</param>
    /// <returns>
    /// A set of antagonist prototype IDs representing the banned antagonist roles for the player,
    /// or null if the player has no antagonist bans.
    /// </returns>
    public HashSet<ProtoId<AntagPrototype>>? GetAntagBans(NetUserId playerUserId);

    /// <summary>
    /// Checks if the player is banned from a specific antagonist role.
    /// Will also return true if they are banned on the All antagonists condition.
    /// </summary>
    /// <param name="userId">Player's user ID.</param>
    /// <param name="antag">Antagonist role ID to check for ban.</param>
    /// <returns>True if the player is banned from the specified antagonist role, false otherwise.</returns>
    public bool IsAntagBanned(NetUserId userId, string antag);

    /// <summary>
    /// Checks if the player is banned from any of the specified antagonist roles.
    /// Will also return true if they are banned on the All antagonists condition.
    /// </summary>
    /// <param name="userId">Player's user ID.</param>
    /// <param name="antags">Antagonist role IDs to check for ban.</param>
    /// <returns>True if the player is banned from any of the specified antagonist roles, false otherwise.</returns>
    public bool IsAntagBanned(NetUserId userId, IEnumerable<string> antags);

    /// <summary>
    /// Checks if the player is banned from any of the specified antagonist roles.
    /// Will also return true if they are banned on the All antagonists condition.
    /// </summary>
    /// <param name="userId">Player's user ID.</param>
    /// <param name="antags">Antagonist role IDs to check for ban as ProtoId<AntagPrototype>.</param>
    /// <returns>True if the player is banned from any of the specified antagonist roles, false otherwise.</returns>
    public bool IsAntagBanned(NetUserId userId, IEnumerable<ProtoId<AntagPrototype>> antags);


    /// <summary>
    /// Checks if the player is banned from a specific role.
    /// </summary>
    /// <param name="userId">Player's user ID.</param>
    /// <param name="roles">Role IDs to check for ban.</param>
    /// <returns>True if the player is banned from any of the specified roles, false otherwise.</returns>
    public bool IsRoleBanned(NetUserId userId, IEnumerable<string> roles);

    /// <summary>
    /// Creates a job ban for the specified target, username or GUID
    /// </summary>
    /// <param name="target">Target user, username or GUID, null for none</param>
    /// <param name="role">Role to be banned from</param>
    /// <param name="severity">Severity of the resulting ban note</param>
    /// <param name="reason">Reason for the ban</param>
    /// <param name="minutes">Number of minutes to ban for. 0 and null mean permanent</param>
    /// <param name="timeOfBan">Time when the ban was applied, used for grouping role bans</param>
    public void CreateRoleBan(NetUserId? target, string? targetUsername, NetUserId? banningAdmin, (IPAddress, int)? addressRange, ImmutableArray<byte>? hwid, string role, uint? minutes, NoteSeverity severity, string reason, DateTimeOffset timeOfBan);

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
    /// <param name="pSession">Player's user ID</param>
    public void SendRoleBans(NetUserId userId);

    /// <summary>
    /// Sends role bans to the target
    /// </summary>
    /// <param name="pSession">Player's session</param>
    public void SendRoleBans(ICommonSession pSession);
}
