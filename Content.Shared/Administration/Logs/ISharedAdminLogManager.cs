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
///     _adminLogger.AddStructured(LogType.Action, LogImpact.Medium,
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
    ///     Records a structured admin log entry with an interpolated message, and optional
    ///     explicit payload, entity references, and player role mappings.
    ///     <para>This is the primary method for recording admin logs.</para>
    ///
    ///     <para><b>Simple usage:</b></para>
    ///     <example>
    ///     <code>
    ///     _adminLogger.AddStructured(LogType.Action, LogImpact.Medium,
    ///         $"{user:player} bolted {door:target}");
    ///     </code>
    ///     </example>
    ///     <para>
    ///     The system <b>automatically</b> extracts players, entities, and role mappings
    ///     from format specifiers in the interpolated string. In most cases, just the
    ///     interpolated string is all you need — skip the optional parameters.
    ///     </para>
    ///
    ///     <para><b>When to use optional parameters:</b></para>
    ///     <list type="bullet">
    ///         <item>
    ///             <c>payload</c> — When you need searchable JSON fields that are
    ///             <b>not</b> entities (e.g., a slot name, damage number, on/off state).
    ///             Entity data from the format string is already captured automatically.
    ///             <code>new { slot = "pocket1" }</code>
    ///         </item>
    ///         <item>
    ///             <c>players</c> — Only when you have a player GUID but <b>no entity</b>
    ///             (e.g., a disconnected player, a voting action, a pre-round event).
    ///             Players attached to entities in the format string are extracted automatically.
    ///         </item>
    ///         <item>
    ///             <c>entities</c> — Only when an entity is <b>conditional/nullable</b> and
    ///             may not appear in the format string, or when you need to supply
    ///             prototype/name metadata for pre-round entities that aren't fully initialized.
    ///         </item>
    ///         <item>
    ///             <c>playerRoles</c> — Only when you need to <b>override</b> an auto-detected
    ///             role. Roles are inferred from the specifier key automatically
    ///             (e.g., <c>:actor</c> → Actor, <c>:victim</c> → Victim, <c>:target</c> → Target).
    ///             This parameter is almost never needed.
    ///         </item>
    ///     </list>
    ///
    ///     <para><b>Example with payload (for non-entity metadata):</b></para>
    ///     <example>
    ///     <code>
    ///     _adminLogger.AddStructured(LogType.Stripping, LogImpact.Low,
    ///         $"{user:actor} placed {item:subject} in {target:victim}'s {slot} slot",
    ///         new { slot });
    ///     </code>
    ///     </example>
    ///     <para>
    ///     The entities, players, and roles above are all extracted from the format
    ///     specifiers (<c>:actor</c>, <c>:subject</c>, <c>:victim</c>). Only <c>slot</c>
    ///     needs the payload.
    ///     </para>
    /// </summary>
    void AddStructured(
        LogType type,
        LogImpact impact,
        [InterpolatedStringHandlerArgument("")] ref LogStringHandler handler,
        object? payload = null,
        IReadOnlyCollection<Guid>? players = null,
        IReadOnlyCollection<AdminLogEntityRef>? entities = null,
        IReadOnlyDictionary<Guid, AdminLogEntityRole>? playerRoles = null);

    /// <summary>
    ///     Records a structured admin log entry with a default impact of
    ///     <see cref="LogImpact.Medium"/>.
    /// </summary>
    void AddStructured(
        LogType type,
        [InterpolatedStringHandlerArgument("")] ref LogStringHandler handler,
        object? payload = null,
        IReadOnlyCollection<Guid>? players = null,
        IReadOnlyCollection<AdminLogEntityRef>? entities = null,
        IReadOnlyDictionary<Guid, AdminLogEntityRole>? playerRoles = null);
}
