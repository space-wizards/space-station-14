using Content.Shared.GameTicking;
using Robust.Server.GameObjects;

namespace Content.Server.RoleTimers;

/// <summary>
/// This handles...
/// </summary>
public sealed class RoleTimerSystem : EntitySystem
{
    [Dependency] private readonly RoleTimerManager _roleTimers = default!;

    private const float AutosaveDelay = 300;
    private float _currentAutosaveDelay = 300;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => _roleTimers.OnRoundEnd());
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);
    }

    public override void Update(float frameTime)
    {
        if (_currentAutosaveDelay >= AutosaveDelay)
        {
            _roleTimers.SaveFullCacheToDb();
        }
        _currentAutosaveDelay -= frameTime;
    }

    public void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        _roleTimers.PlayerRolesChanged(ev.Player.UserId);
    }

    public void OnPlayerDetached(PlayerDetachedEvent ev)
    {
        // This doesn't fire if the player doesn't leave their body. I guess it's fine?
        _roleTimers.PlayerRolesChanged(ev.Player.UserId, null, new HashSet<string>());
    }
}
