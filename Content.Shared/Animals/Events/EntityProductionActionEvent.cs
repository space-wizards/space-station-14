using Content.Shared.Actions;

namespace Content.Shared.Animals.Events;

/// <summary>
/// Instant action event used to manually request entity production.
/// </summary>
public sealed partial class EntityProductionActionEvent : InstantActionEvent;
