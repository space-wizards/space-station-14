using Content.Server.GameTicking;
using Content.Shared.Ghost;

namespace Content.Server.Ghost;

public sealed class GhostVisibilitySystem : SharedGhostVisibilitySystem
{
    [Dependency] private readonly GameTicker _ticker = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        // Reset global visibility.
        if (ev.New is GameRunLevel.PreRoundLobby or GameRunLevel.InRound)
            AllVisible = false;

        var entityQuery = EntityQueryEnumerator<GhostVisibilityComponent, VisibilityComponent>();
        while (entityQuery.MoveNext(out var uid, out var ghost, out var vis))
        {
            UpdateVisibility((uid, ghost, vis));
        }
    }

    protected override bool ShouldBeVisible(GhostVisibilityComponent comp)
    {
        if (comp.VisibleOverride is { } val)
            return val;

        if (base.ShouldBeVisible(comp))
            return true;

        return _ticker.RunLevel == GameRunLevel.PostRound && comp.VisibleOnRoundEnd;
    }
}
