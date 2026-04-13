using System.Runtime.CompilerServices;
using Content.Shared.Database;

namespace Content.Shared.Administration.Logs;

/// <summary>
///     Shared interface for recording structured admin log events.
///     See below for the DI standard:
///     <code>[Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;</code>
///
///     <para>
///     follows a simple three-tier model: message-only first, payload when you need extra machine-readable
///     detail, and explicit semantics only for the real exception cases. Most call sites should stay in Tier 1.
///     </para>
///
///     <para><b>Quick-start example:</b></para>
///     <code>
///     _adminLogger.Add(LogType.Action, LogImpact.Medium,
///         $"{user:actor} buckled {target:target} to {strap:subject}");
///     </code>
///
///     <para><b>Format specifiers (the <c>:role</c> syntax):</b></para>
///     The text after <c>:</c> gives the interpolated value a semantic role for searchable participation
///     tracking, and also becomes the key name in the handler's internal <c>Values</c> dictionary.
///     Recommended specifiers:
///     <list type="bullet">
///         <item><c>:actor</c> / <c>:user</c> / <c>:player</c> — The acting entity/player → <see cref="AdminLogEntityRole.Actor"/>.</item>
///         <item><c>:target</c> — The entity being acted upon → <see cref="AdminLogEntityRole.Target"/>.</item>
///         <item><c>:victim</c> — The entity receiving a negative action → <see cref="AdminLogEntityRole.Victim"/>.</item>
///         <item><c>:tool</c> / <c>:using</c> / <c>:weapon</c> — The instrument used → <see cref="AdminLogEntityRole.Tool"/>.</item>
///         <item><c>:subject</c> — The primary subject of the action → <see cref="AdminLogEntityRole.Subject"/>.</item>
///         <item><c>:entity</c> — Generic entity reference when no more specific role fits.</item>
///     </list>
///
///     <para><b>How semantic capture works:</b></para>
///     <para>
///     Interpolated <see cref="EntityUid"/> and <see cref="Robust.Shared.Player.ICommonSession"/> values
///     are captured from the message and recorded as searchable participants. Their roles come from the
///     semantic label, with a fallback heuristic for the cases that still need one.
///     </para>
///     <para>
///     <b>Important:</b> interpolation controls participant extraction, not JSON storage. The stored
///     <c>Json</c> payload comes only from the explicit <c>payload</c> argument.
///     </para>
///
///     <para><b>Capture rules by interpolation type:</b></para>
///     <list type="bullet">
///         <item><see cref="EntityUid"/> — Always captured. Key = format specifier or variable name.</item>
///         <item><see cref="Robust.Shared.Player.ICommonSession"/> — Always captured as player data. Key = variable name only; the format specifier does not affect keying.</item>
///         <item><c>string</c> / <c>object</c> without a format specifier — Not captured.</item>
///         <item><c>string</c> / <c>object</c> with a format specifier — Captured using that specifier as the semantic key.</item>
///     </list>
///
///     <para><b>Heuristic limitations:</b></para>
///     Role inference is keyword-based. Use explicit semantic specifiers rather than relying on
///     variable names that only happen to look descriptive.
///     <list type="bullet">
///         <item><c>source</c> is not a reliable stand-in for Actor. Prefer <c>:actor</c>.</item>
///         <item><c>used</c> only maps cleanly for some log types. Prefer <c>:tool</c> or <c>:subject</c>.</item>
///         <item><c>food</c> is usually intent, not role. Prefer <c>:tool</c> or <c>:subject</c>.</item>
///         <item><c>entity</c> is intentionally generic. Prefer a stronger label whenever you know the role.</item>
///     </list>
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
///             <see cref="LogImpact.Extreme"/> — Irreversible, round-altering actions:
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
    ///     Records a structured admin log entry with an interpolated message and optional explicit metadata.
    ///     <para>This is the primary admin logging API.</para>
    ///
    ///     <para><b>Three-tier authoring model:</b></para>
    ///     Start with the message. Add <c>payload</c> only when you need extra machine-readable detail.
    ///     Reach for explicit entities or player roles only when the message cannot represent the event faithfully.
    ///
    ///     <para><b>Tier 1 — Message-only (default for most logs):</b></para>
    ///     <example>
    ///     <code>
    ///     _adminLogger.Add(LogType.MeleeHit, LogImpact.Medium,
    ///         $"{attacker:actor} hit {target:victim} with {weapon:tool}");
    ///     </code>
    ///     </example>
    ///     <para>
    ///     Use Tier 1 when the message already contains the important participants and one entity per role is enough.
    ///     </para>
    ///
    ///     <para><b>Tier 2 — Message + payload (for non-entity metadata):</b></para>
    ///     <example>
    ///     <code>
    ///     _adminLogger.Add(LogType.Stripping, LogImpact.Low,
    ///         $"{user:actor} placed {item:subject} in {target:victim}'s {slot} slot",
    ///         new { slot });
    ///     </code>
    ///     </example>
    ///     <para>
    ///     Use Tier 2 when you need extra JSON data that is not itself a participant.
    ///     </para>
    ///
    ///     <para><b>Tier 3 — Message + explicit entity refs / player roles:</b></para>
    ///     <example>
    ///     <code>
    ///     // Multi-target: targets cannot all appear as named interpolation arguments.
    ///     _adminLogger.Add(LogType.MeleeHit, LogImpact.Medium,
    ///         $"{user:actor} hit {targets.Count} targets using {weapon:tool}",
    ///         entities: targetRefs);
    ///
    ///     // Entity not in message: source is nullable and not interpolated.
    ///     _adminLogger.Add(LogType.Chat, LogImpact.Low,
    ///         $"Station announcement from {sender}: {message}",
    ///         players: sourcePlayer != null ? new[] { sourcePlayer.Value } : null,
    ///         entities: source != null
    ///             ? new[] { new AdminLogEntityRef(source.Value, AdminLogEntityRole.Actor) }
    ///             : null);
    ///
    ///     // Role correction: auto-detection would infer the wrong role.
    ///     _adminLogger.Add(LogType.InteractActivate, LogImpact.Low,
    ///         $"{user:user} activated {used:used}",
    ///         entities: new[]
    ///         {
    ///             new AdminLogEntityRef(user, AdminLogEntityRole.Actor),
    ///             new AdminLogEntityRef(used, AdminLogEntityRole.Subject),
    ///         });
    ///     </code>
    ///     </example>
    ///     <para>Use Tier 3 only when:</para>
    ///     <list type="bullet">
    ///         <item>One action affects <b>multiple entities of the same role</b> (e.g., wide melee hitting N victims).</item>
    ///         <item>A participant is <b>not rendered</b> in the message but must still be queryable (e.g., nullable source, weapon not in text).</item>
    ///         <item>A role would be <b>ambiguous or wrong</b> under the heuristic (e.g., <c>used</c> → Other when you need Subject).</item>
    ///         <item>One entity or player must participate in <b>multiple semantic roles</b>, such as self-action actor/victim cases.</item>
    ///         <item>You have a <b>player GUID without an entity</b> (disconnected player, voting, pre-round event).</item>
    ///         <item><b>Pre-resolved metadata</b> must be preserved and cannot be recovered later.</item>
    ///     </list>
    ///     <para>
    ///     Tier 3 is the exception path. If the message already says enough, prefer Tier 1 or Tier 2.
    ///     </para>
    ///     <para>
    ///     For known awkward cases, prefer the shared helper path in <see cref="AdminLogHelpers"/>
    ///     over rebuilding <c>players</c>, <c>entities</c>, and <c>playerRoles</c> by hand.
    ///     </para>
    ///
    ///     <para><b>Redundancy rule:</b></para>
    ///     <para>
    ///     If a participant already appears in the interpolated message with a semantic label that
    ///     maps correctly, do <b>not</b> restate it in <c>entities</c> or <c>playerRoles</c>.
    ///     Redundant explicit metadata makes logs harder to read and easier to skew. Explicit entries
    ///     for the same <see cref="EntityUid"/> suppress auto-detected entries by UID, regardless of role.
    ///     </para>
    ///
    ///     <para><b>Realistic Decision Tree:</b></para>
    ///     <list type="number">
    ///         <item>Start with the message you want admins to read.</item>
    ///         <item>Add semantic labels to each important participant.</item>
    ///         <item>If that fully describes the participants, stop at Tier 1.</item>
    ///         <item>If you still need searchable scalar metadata, add <c>payload</c> and stop at Tier 2.</item>
    ///         <item>Reach for explicit <c>players</c>, <c>entities</c>, or <c>playerRoles</c> only when Tier 1/2 cannot represent the event honestly.</item>
    ///     </list>
    ///
    ///     <para><b>Merge priority:</b></para>
    ///     <list type="bullet">
    ///         <item><c>players</c> — Explicit + auto-detected values are combined and deduplicated.</item>
    ///         <item><c>entities</c> — Explicit entries are added first; auto-detected entries are skipped if that UID is already present.</item>
    ///         <item><c>playerRoles</c> — Auto-detected roles are added via TryAdd (first wins); explicit roles then overwrite. Each player GUID stores only one role.</item>
    ///     </list>
    /// </summary>
    void Add(
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
    ///     The same Tier 1 / Tier 2 / Tier 3 guidance applies.
    /// </summary>
    void Add(
        LogType type,
        [InterpolatedStringHandlerArgument("")] ref LogStringHandler handler,
        object? payload = null,
        IReadOnlyCollection<Guid>? players = null,
        IReadOnlyCollection<AdminLogEntityRef>? entities = null,
        IReadOnlyDictionary<Guid, AdminLogEntityRole>? playerRoles = null);
}
