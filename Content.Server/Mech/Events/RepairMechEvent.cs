using Content.Shared.Construction;
using JetBrains.Annotations;

namespace Content.Server.Mech.Events;

/// <summary>
/// Construction graph event to repair a mech in broken state.
/// </summary>
[UsedImplicitly]
[DataDefinition]
public sealed partial class RepairMechEvent : EntityEventArgs
{
}
