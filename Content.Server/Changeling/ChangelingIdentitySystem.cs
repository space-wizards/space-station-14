using Content.Server.Body.Components;
using Content.Server.Mind;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Changeling;
using Content.Shared.Players;
using Robust.Server.GameStates;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.Changeling;

public sealed class ChangelingIdentitySystem : SharedChangelingIdentitySystem
{
    [Dependency] private readonly PvsOverrideSystem _pvsOverrideSystem = default!;

    protected override void HandlePvsOverride(EntityUid uid, EntityUid target)
    {
        if(!TryComp<ActorComponent>(uid, out var actor))
            return;
        base.HandlePvsOverride(uid, target);
        _pvsOverrideSystem.AddSessionOverride(target, actor.PlayerSession);
    }

    /// <summary>
    /// Inform another Session of the entities stored for Transformation
    /// </summary>
    /// <param name="session">The Session you wish to inform</param>
    /// <param name="comp">The Target storage of identities</param>
    public void HandOverPvsOverride(ICommonSession session, ChangelingIdentityComponent comp)
    {
        foreach (var entity in comp.ConsumedIdentities)
        {
            _pvsOverrideSystem.AddSessionOverride(entity, session);
        }
    }
}
