using Content.Server.Explosion.Components;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;

namespace Content.Server.Explosion.EntitySystems;

public sealed class ToggleOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly IEntitySystemManager _entSystemManager = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ToggleOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    public void OnTrigger(EntityUid uid, ToggleOnTriggerComponent triggerComp, TriggerEvent args)
    {
        var itemToggleSystem = _entSystemManager.GetEntitySystem<SharedItemToggleSystem>();
        EntityUid user;
        if (args.User != null)
        {
            user = (EntityUid) args.User;
        }
        else
        {
            user = uid;
        }

        TryComp<ItemToggleComponent>(uid, out var toggleComp);
        itemToggleSystem.Toggle(uid, user, true, toggleComp);
    }
}
