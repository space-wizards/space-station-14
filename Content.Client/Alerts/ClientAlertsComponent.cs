using Content.Shared.Alert;
using Robust.Shared.GameObjects;

namespace Content.Client.Alerts;

/// <inheritdoc />
[RegisterComponent]
[ComponentReference(typeof(SharedAlertsComponent))]
public sealed class ClientAlertsComponent : SharedAlertsComponent { }
