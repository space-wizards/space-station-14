using System.Net;
using System.Net.Sockets;
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
    /// Create a server ban in the database, blocking connection for matching players.
    /// </summary>
    void CreateServerBan(CreateServerBanInfo banInfo);

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
    [Obsolete("Use CreateServerBan(CreateBanInfo) instead")]
    public void CreateServerBan(NetUserId? target,
        string? targetUsername,
        NetUserId? banningAdmin,
        (IPAddress, int)? addressRange,
        ImmutableTypedHwid? hwid,
        uint? minutes,
        NoteSeverity severity,
        string reason)
    {
        var info = new CreateServerBanInfo(reason);
        if (target != null)
        {
            ArgumentNullException.ThrowIfNull(targetUsername);
            info.AddUser(target.Value, targetUsername);
        }

        if (addressRange != null)
            info.AddAddressRange(addressRange.Value);

        if (hwid != null)
            info.AddHWId(hwid);

        if (minutes > 0)
            info.WithMinutes(minutes.Value);

        if (banningAdmin != null)
            info.WithBanningAdmin(banningAdmin.Value);

        info.WithSeverity(severity);

        CreateServerBan(info);
    }

    /// <summary>
    /// Gets a list of prefixed prototype IDs with the player's role bans.
    /// </summary>
    public HashSet<BanRoleDef>? GetRoleBans(NetUserId playerUserId);

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
    /// Creates a role ban, preventing matching players from playing said roles.
    /// </summary>
    public void CreateRoleBan(CreateRoleBanInfo banInfo);

    /// <summary>
    /// Pardons a role ban by its ID.
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

/// <summary>
/// Base info to fill out in created ban records.
/// </summary>
/// <seealso cref="CreateServerBanInfo"/>
/// <seealso cref="CreateRoleBanInfo"/>
[Access(typeof(BanManager), Other = AccessPermissions.Execute)]
public abstract class CreateBanInfo
{
    [Access(Other = AccessPermissions.Read)]
    public const int DefaultMaskIpv4 = 32;
    [Access(Other = AccessPermissions.Read)]
    public const int DefaultMaskIpv6 = 64;

    internal readonly HashSet<(NetUserId UserId, string UserName)> Users = [];
    internal readonly HashSet<(IPAddress Address, int Mask)> AddressRanges = [];
    internal readonly HashSet<ImmutableTypedHwid> HWIds = [];
    internal readonly HashSet<int> RoundIds = [];
    internal TimeSpan? Duration;
    internal NoteSeverity? Severity;
    internal string Reason;
    internal NetUserId? BanningAdmin;

    protected CreateBanInfo(string reason)
    {
        Reason = reason;
    }

    /// <summary>
    /// Add a user to be matched by the ban.
    /// </summary>
    /// <remarks>
    /// Bans can target multiple users at once.
    /// </remarks>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="username">The name of the user (used for logging purposes).</param>
    /// <returns>The current object, for easy chaining.</returns>
    public CreateBanInfo AddUser(NetUserId userId, string username)
    {
        Users.Add((userId, username));
        return this;
    }

    /// <summary>
    /// Add an IP address to be matched by the ban.
    /// </summary>
    /// <remarks>
    /// Bans can target multiple addresses at once.
    /// </remarks>
    /// <param name="address">
    /// The IP address to add. If null, nothing is done.
    /// </param>
    /// <returns>The current object, for easy chaining.</returns>
    public CreateBanInfo AddAddress(IPAddress? address)
    {
        if (address == null)
            return this;

        return AddAddressRange(
            address,
            address.AddressFamily == AddressFamily.InterNetwork ? DefaultMaskIpv4 : DefaultMaskIpv6);
    }

    /// <summary>
    /// Add an IP address range to be matched by the ban.
    /// </summary>
    /// <remarks>
    /// Bans can target multiple address ranges at once.
    /// </remarks>
    /// <returns>The current object, for easy chaining.</returns>
    public CreateBanInfo AddAddressRange((IPAddress Address, int Mask) addressRange)
    {
        return AddAddressRange(addressRange.Address, addressRange.Mask);
    }

    /// <summary>
    /// Add an IP address range to be matched by the ban.
    /// </summary>
    /// <remarks>
    /// Bans can target multiple address ranges at once.
    /// </remarks>
    /// <returns>The current object, for easy chaining.</returns>
    public CreateBanInfo AddAddressRange(IPAddress address, int mask)
    {
        AddressRanges.Add((address, mask));
        return this;
    }

    /// <summary>
    /// Add a hardware IP (HWID) to be matched by the ban.
    /// </summary>
    /// <remarks>
    /// Bans can target multiple HWIDs at once.
    /// </remarks>
    /// <param name="hwId">
    /// The HWID to add. If null, nothing is done.
    /// </param>
    /// <returns>The current object, for easy chaining.</returns>
    public CreateBanInfo AddHWId(ImmutableTypedHwid? hwId)
    {
        if (hwId != null)
            HWIds.Add(hwId);

        return this;
    }

    /// <summary>
    /// Add a relevant round ID to this ban.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If not specified, the current round ID is used for the ban.
    /// Therefore, the first call to this function will <i>replace</i> the round ID,
    /// and further calls will add additional round IDs.
    /// </para>
    /// <para>
    /// Bans can target multiple round IDs at once.
    /// </para>
    /// </remarks>
    /// <returns>The current object, for easy chaining.</returns>
    public CreateBanInfo AddRoundId(int roundId)
    {
        RoundIds.Add(roundId);
        return this;
    }

    /// <summary>
    /// Set how long the ban will last, in minutes.
    /// </summary>
    /// <remarks>
    /// If no duration is specified, the ban is permanent.
    /// </remarks>
    /// <param name="minutes">The duration of the ban, in minutes.</param>
    /// <returns>The current object, for easy chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <see cref="minutes"/> is not a positive number.
    /// </exception>
    public CreateBanInfo WithMinutes(int minutes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minutes);
        return WithMinutes((uint)minutes);
    }

    /// <summary>
    /// Set how long the ban will last, in minutes.
    /// </summary>
    /// <remarks>
    /// If no duration is specified, the ban is permanent.
    /// </remarks>
    /// <param name="minutes">The duration of the ban, in minutes.</param>
    /// <returns>The current object, for easy chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <see cref="minutes"/> is not a positive number.
    /// </exception>
    public CreateBanInfo WithMinutes(uint minutes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minutes);
        return WithDuration(TimeSpan.FromMinutes(minutes));
    }

    /// <summary>
    /// Set how long the ban will last.
    /// </summary>
    /// <remarks>
    /// If no duration is specified, the ban is permanent.
    /// </remarks>
    /// <param name="duration">The duration of the ban.</param>
    /// <returns>The current object, for easy chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <see cref="duration"/> is not a positive amount of time.
    /// </exception>
    public CreateBanInfo WithDuration(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be greater than zero.");

        Duration = duration;
        return this;
    }

    /// <summary>
    /// Set the severity of the ban.
    /// </summary>
    /// <remarks>
    /// If no severity is specified, the default is specified through server configuration.
    /// </remarks>
    /// <param name="severity"></param>
    /// <returns>The current object, for easy chaining.</returns>
    public CreateBanInfo WithSeverity(NoteSeverity severity)
    {
        Severity = severity;
        return this;
    }

    /// <summary>
    /// Set the reason for the ban.
    /// </summary>
    /// <remarks>
    /// This replaces the value given via the object constructor.
    /// </remarks>
    /// <returns>The current object, for easy chaining.</returns>
    public CreateBanInfo WithReason(string reason)
    {
        Reason = reason;
        return this;
    }

    /// <summary>
    /// Specify the admin responsible for placing the ban.
    /// </summary>
    /// <returns>The current object, for easy chaining.</returns>
    public CreateBanInfo WithBanningAdmin(NetUserId? banningAdmin)
    {
        BanningAdmin = banningAdmin;
        return this;
    }
}

/// <summary>
/// Stores info to create server ban records.
/// </summary>
/// <seealso cref="IBanManager.CreateServerBan(CreateServerBanInfo)"/>
[Access(typeof(BanManager), Other = AccessPermissions.Execute)]
public sealed class CreateServerBanInfo : CreateBanInfo
{
    /// <param name="reason">The reason for the server ban.</param>
    public CreateServerBanInfo(string reason) : base(reason)
    {
    }
}

/// <summary>
/// Stores info to create role ban records.
/// </summary>
/// <seealso cref="IBanManager.CreateRoleBan(CreateRoleBanInfo)"/>
[Access(typeof(BanManager), Other = AccessPermissions.Execute)]
public sealed class CreateRoleBanInfo : CreateBanInfo
{
    internal readonly HashSet<ProtoId<AntagPrototype>> AntagPrototypes = [];
    internal readonly HashSet<ProtoId<JobPrototype>> JobPrototypes = [];

    /// <param name="reason">The reason for the role ban.</param>
    public CreateRoleBanInfo(string reason) : base(reason)
    {
    }

    /// <summary>
    /// Add an antag role that will be unavailable for banned players.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bans can have multiple roles at once.
    /// </para>
    /// <para>
    /// While not checked in this function, adding a ban with invalid role IDs will cause a
    /// <see cref="UnknownPrototypeException"/> when actually creating the ban.
    /// </para>
    /// </remarks>
    /// <returns>The current object, for easy chaining.</returns>
    public CreateRoleBanInfo AddAntag(ProtoId<AntagPrototype> protoId)
    {
        AntagPrototypes.Add(protoId);
        return this;
    }

    /// <summary>
    /// Add a job role that will be unavailable for banned players.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bans can have multiple roles at once.
    /// </para>
    /// <para>
    /// While not checked in this function, adding a ban with invalid role IDs will cause a
    /// <see cref="UnknownPrototypeException"/> when actually creating the ban.
    /// </para>
    /// </remarks>
    /// <returns>The current object, for easy chaining.</returns>
    public CreateRoleBanInfo AddJob(ProtoId<JobPrototype> protoId)
    {
        JobPrototypes.Add(protoId);
        return this;
    }
}
