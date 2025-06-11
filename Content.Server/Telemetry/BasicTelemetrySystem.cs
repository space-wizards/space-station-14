using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared.Telemetry;

namespace Content.Server.Telemetry;

public sealed class BasicTelemetrySystem : SharedBasicTelemetrySystem
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly GameTicker _ticker = default!;

    public override void AddTelemetryData(string campaign, string metadata)
    {
        _db.AddTelemetryData(_ticker.RoundId, campaign, metadata);
    }
}
