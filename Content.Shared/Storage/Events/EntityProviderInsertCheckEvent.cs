using Content.Shared.Storage.Components;

namespace Content.Shared.Storage.Events;

/// <summary>
/// This is used for more deep checks on what entities are allowed to be inserted into a <see cref="EntityProviderComponent"/>.
/// Is fired after whitelist check got OK.
/// </summary>
/// <example>Broken lights satisfy the whitelist requirement, but shouldn't be inserted into light replacers.</example>
[ByRefEvent]
public record struct EntityProviderInsertCheckEvent(string? FailureMessage = null);
