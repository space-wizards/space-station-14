using Content.Shared.Alert;
using Robust.Shared.GameObjects;

namespace Content.Server.Alert;

[RegisterComponent]
[ComponentReference(typeof(SharedAlertsComponent))]
public sealed class ServerAlertsComponent : SharedAlertsComponent { }
