using Content.Shared.Database;

namespace Content.Shared.Administration.Logs;

/// <summary>
///     Explicit entity participation metadata for Tier 3 calls.
///     <para>
///     Most logs should not create <see cref="AdminLogEntityRef"/> values at all. Message-first logging is
///     the default, and <c>payload</c> handles extra JSON data. Use explicit refs only when the message
///     cannot preserve the participants you need to query later.
///     </para>
///
///     <para><b>Good Tier 3 use cases:</b></para>
///     <list type="bullet">
///         <item>The entity is not rendered in the message but still needs to be queryable.</item>
///         <item>Multiple entities share the same role, such as several victims from one action.</item>
///         <item>The inferred role would be wrong or too ambiguous.</item>
///         <item>Pre-resolved prototype or name metadata must be preserved.</item>
///     </list>
///
///     <para><b>Avoid redundant refs:</b> if the message already says <c>{user:actor}</c>,
///     <c>{target:victim}</c>, or a similar correctly-labeled participant, do not restate that entity here.</para>
/// </summary>
public readonly record struct AdminLogEntityRef(
    EntityUid Entity,
    AdminLogEntityRole Role,
    string? PrototypeId = null,
    string? EntityName = null);
