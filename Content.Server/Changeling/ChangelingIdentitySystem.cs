using Content.Shared.Changeling;
using Robust.Server.GameStates;
using Robust.Shared.Player;

namespace Content.Server.Changeling;

public sealed class ChangelingIdentitySystem : SharedChangelingIdentitySystem
{
    [Dependency] private readonly PvsOverrideSystem _pvsOverrideSystem = default!;

    protected override void HandlePvsOverride(EntityUid uid, ChangelingIdentityComponent component, EntityUid target)
    {
        if(!TryComp<ActorComponent>(uid, out var actor))
            return;
        base.HandlePvsOverride(uid, component, target);
        _pvsOverrideSystem.AddSessionOverride(target, actor.PlayerSession);
    }
}
