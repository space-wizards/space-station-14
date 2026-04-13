using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;

namespace Content.Server.Administration.Logs;

/// <summary>
///     For system events that the manager needs to know about.
///     <see cref="IAdminLogManager"/> for admin log usage.
/// </summary>
public sealed class AdminLogSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(ev => _adminLogger.RoundStarting(ev.Id));
        SubscribeLocalEvent<GameRunLevelChangedEvent>(ev => _adminLogger.RunLevelChanged(ev.New));
    }

    public override void Update(float frameTime)
    {
        _adminLogger.Update();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _adminLogger.Shutdown();
    }
}
