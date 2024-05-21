using Content.Server.Explosion.Components;
using Content.Shared.Interaction;

namespace Content.Server.Explosion.EntitySystems;

public sealed class InteractOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly SharedInteractionSystem _sharedInteractionSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InteractOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    public void OnTrigger(EntityUid uid, InteractOnTriggerComponent comp, TriggerEvent args)
    {
        EntityUid user;
        if (args.User != null)
        {
            user = (EntityUid) args.User;
        }
        else
        {
            user = uid;
        }
        _sharedInteractionSystem.InteractHand(user, uid);
    }
}
