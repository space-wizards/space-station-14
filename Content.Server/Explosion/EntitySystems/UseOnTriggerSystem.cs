using Content.Server.Explosion.Components;
using Content.Shared.Interaction;

namespace Content.Server.Explosion.EntitySystems;

public sealed class UseOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UseOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    public void OnTrigger(EntityUid uid, UseOnTriggerComponent comp, TriggerEvent args)
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
        _interactionSystem.UseInHandInteraction(user, uid, false, false, true);
    }
}
