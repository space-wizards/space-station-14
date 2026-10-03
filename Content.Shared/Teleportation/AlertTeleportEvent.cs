using Content.Shared.Alert;
using Content.Shared.Teleportation.Components;

namespace Content.Shared.Teleportation;

/// <summary>
/// If there is an <see cref="AlertTeleportComponent"/>, teleports to a specific entity.
/// </summary>
public sealed partial class AlertTeleportEvent : BaseAlertEvent;
