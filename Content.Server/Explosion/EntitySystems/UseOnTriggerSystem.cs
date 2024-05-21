using Content.Server.Explosion.Components;
using Content.Shared.Interaction;

namespace Content.Server.Explosion.EntitySystems;

public sealed class UseOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly IEntitySystemManager _entSystemManager = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UseOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    public void OnTrigger(EntityUid uid, UseOnTriggerComponent comp, TriggerEvent args)
    {
        var interactionSystem = _entSystemManager.GetEntitySystem<SharedInteractionSystem>();
        EntityUid user;
        if (args.User != null)
        {
            user = (EntityUid) args.User;
        }
        else
        {
            user = uid;
        }
        interactionSystem.InteractionActivate(user, uid, false, true, false);
    }
}
