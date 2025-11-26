using JetBrains.Annotations;

namespace Content.Shared.Mech.Events;

/// <summary>
/// Construction graph event to repair a mech in broken state.
/// </summary>
[UsedImplicitly]
[DataDefinition]
public sealed partial class RepairMechEvent : EntityEventArgs;
