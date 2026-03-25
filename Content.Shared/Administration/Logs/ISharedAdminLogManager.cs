using System.Runtime.CompilerServices;
using Content.Shared.Database;

namespace Content.Shared.Administration.Logs;

/// <summary>
///     Shared interface for recording admin log events.
///     See below for the DI standard
///     <code>[Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;</code>
///
///     <para><b>Quick-start example:</b></para>
///     <code>
///     _adminLogger.Add(LogType.Action, LogImpact.Medium,
///         $"{user:player} buckled {target:target} to {strap:subject}");
///     </code>
///
///     <para><b>Format specifiers (the <c>:role</c> syntax):</b></para>
///     The text after <c>:</c> serves as the <b>key name</b> for the
///     value in the log's JSON metadata.
///   . Valid role-like names include:
///     <list type="bullet">
///         <item><c>:player</c> / <c>:user</c> — The acting player.</item>
///         <item><c>:target</c> — The entity being acted upon.</item>
///         <item><c>:subject</c> — The primary subject of the log entry.</item>
///         <item><c>:tool</c> / <c>:using</c> — The item/tool being used.</item>
///         <item><c>:entity</c> — A generic entity reference.</item>
///     </list>
///     These become JSON keys in the stored log. Example:
///     <code>$"{user:player} hit {target:target} with {weapon:tool}"</code>
///     produces JSON like <c>{"player": "Urist (1234)", "target": "Ian (5678)", "tool": "Wrench (9012)"}</c>.
///
///     <para><b>LogImpact guidelines:</b></para>
///     <list type="bullet">
///         <item>
///             <see cref="LogImpact.Low"/> — Routine actions: picking up items, opening doors,
///             toggling lights, chatting. High volume, low admin interest.
///         </item>
///         <item>
///             <see cref="LogImpact.Medium"/> — Actions with gameplay impact but not
///             inherently suspicious: cuffing, setting atmos device parameters, using
///             medical tools on others, bolting doors.
///         </item>
///         <item>
///             <see cref="LogImpact.High"/> — Actions that may indicate griefing or
///             that admins should be aware of: emagging, cutting cables, modifying ID access,
///             arming explosives.
///         </item>
///         <item>
///             <see cref="LogImpact.Extreme"/> — Irreversible, round-altering, actions:
///             arming the nuke, detonating explosives. Admins are notified.
///         </item>
///     </list>
/// </summary>
public interface ISharedAdminLogManager
{
    public bool Enabled { get; }

    /// <summary>
    ///     Converts a format-specifier name to the JSON naming convention.
    /// </summary>
    public string ConvertName(string name);

    /// <summary>
    ///     Provides access to the entity manager so that <see cref="LogStringHandler"/> can
    ///     call <c>ToPrettyString()</c> on entities automatically.
    /// </summary>
    public IEntityManager EntityManager { get; }

    /// <summary>
    ///     Records an admin log with the specified type, impact, and interpolated message.
    ///     <para>This is the primary method for recording admin logs.</para>
    ///     <example>
    ///     <code>
    ///     _adminLogger.Add(LogType.Action, LogImpact.Medium,
    ///         $"{user:player} bolted {door:target}");
    ///     </code>
    ///     </example>
    /// </summary>
    void Add(LogType type, LogImpact impact, [InterpolatedStringHandlerArgument("")] ref LogStringHandler handler);

    /// <summary>
    ///     Records an admin log with the specified type and a default impact of
    ///     <see cref="LogImpact.Medium"/>. See <see cref="Add(LogType, LogImpact, ref LogStringHandler)"/>
    ///     for full documentation.
    /// </summary>
    void Add(LogType type, [InterpolatedStringHandlerArgument("")] ref LogStringHandler handler);

    /// <summary>
    ///     Records a fully pre-built structured log entry with an explicit payload object,
    ///     entity references, and player role mappings.
    ///
    ///     <para>
    ///     For most use cases, prefer <see cref="Add(LogType, LogImpact, ref LogStringHandler)"/>
    ///     which handles entity extraction automatically.
    ///     </para>
    ///     <example>
    ///     <code>
    ///     _adminLogger.AddStructured(LogType.Stripping, LogImpact.Low,
    ///         $"{user:actor} placed {item:subject} in {target:victim}'s {slot} slot",
    ///         new { user = (int) user, target = (int) target, item = (int) held, slot },
    ///         players: players,
    ///         entities: [ new AdminLogEntityRef(user, AdminLogEntityRole.Actor) ]);
    ///     </code>
    ///     </example>
    /// </summary>
    void AddStructured(
        LogType type,
        LogImpact impact,
        string message,
        object? payload,
        IReadOnlyCollection<Guid>? players = null,
        IReadOnlyCollection<AdminLogEntityRef>? entities = null,
        IReadOnlyDictionary<Guid, AdminLogEntityRole>? playerRoles = null);
}
