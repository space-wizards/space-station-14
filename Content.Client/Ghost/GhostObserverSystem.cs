using Content.Shared.Ghost.Components;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Ghost;

public sealed partial class GhostObserverSystem : EntitySystem
{
    [Dependency] private readonly GhostSystem _ghost = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GhostObserverComponent, LocalPlayerAttachedEvent>(OnPlayerAttach);
    }

    private void OnPlayerAttach(EntityUid uid, GhostObserverComponent component, LocalPlayerAttachedEvent localPlayerAttachedEvent)
    {
        _ghost.SetGhostVisibility(true);
    }
}
