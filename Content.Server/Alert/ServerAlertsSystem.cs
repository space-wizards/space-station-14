using Content.Shared.Alert;

namespace Content.Server.Alert;

// The only reason this exists is because the DI system requires the shared AlertsSystem
// to be abstract.
internal sealed class ServerAlertsSystem : AlertsSystem { }
