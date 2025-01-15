using Content.Server.Body.Components;
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
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    protected override void HandlePvsOverride(EntityUid uid, EntityUid target)
    {
        if(!TryComp<ActorComponent>(uid, out var actor))
            return;
        base.HandlePvsOverride(uid, target);
        _pvsOverrideSystem.AddSessionOverride(target, actor.PlayerSession);
    }

    protected override void ReplaceLingsLungs(EntityUid uid)
    {
        if (!TryComp<BodyComponent>(uid, out var body))
            return;

        var lingLungs = Spawn("OrganLingLungs", _transform.GetMapCoordinates(uid));
        foreach (var entity in _bodySystem.GetBodyOrganEntityComps<LungComponent>((uid, body)))
        {
            if (!_container.TryGetContainingContainer((entity.Owner, null, null), out var container))
                return;

            QueueDel(entity);
            if(!_container.CanInsert(lingLungs, container))
                Log.Log(LogLevel.Error,"AAAAAAAAAAAAAAAAAAA");
            _container.Insert(lingLungs, container);

            //_bodySystem.InsertOrgan(container.Owner, lingLungs, container.ID);

        }
    }
    /// <summary>
    /// Inform another Session of the entities stored for Transformation
    /// </summary>
    /// <param name="session">The Session you wish to inform</param>
    /// <param name="common">The Target storage of identities</param>
    public void HandOverPvsOverride(ICommonSession session, ChangelingIdentityComponent comp)
    {
        foreach (var entity in comp.ConsumedIdentities)
        {
            _pvsOverrideSystem.AddSessionOverride(entity, session);
        }
    }
}
