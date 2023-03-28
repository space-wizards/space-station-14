using Content.Shared.Body.Surgery.Systems;
using Robust.Server.GameObjects;

namespace Content.Server.Body.Surgery.Systems;

public sealed class SurgerySystem : SharedSurgerySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    protected override bool OpenSelectUi(EntityUid uid, EntityUid surgeon, EntityUid target, Enum key, BoundUserInterfaceState state)
    {
        if (!TryComp<ActorComponent>(surgeon, out var actor) ||
            !_ui.TryOpen(uid, key, actor.PlayerSession))
            return false;

        return _ui.TrySetUiState(uid, key, state, actor.PlayerSession);
    }
}
