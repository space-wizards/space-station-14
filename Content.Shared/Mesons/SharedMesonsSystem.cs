using Content.Shared.Actions;
using Content.Shared.Clothing;
using Content.Shared.Toggleable;

namespace Content.Shared.Mesons;

public sealed class SharedMesonsSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MesonsComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<MesonsComponent, MapInitEvent>(OnMapInit);
    }

    private void OnGetItemActions(Entity<MesonsComponent> uid, ref GetItemActionsEvent args)
    {
        if (uid.Comp.ActionEntity is {} actionEntity)
            args.AddAction(actionEntity);
    }

    private void OnMapInit(Entity<MesonsComponent> uid, ref MapInitEvent args)
    {
        _actionContainer.EnsureAction(uid, ref uid.Comp.ActionEntity, uid.Comp.Action);
    }
}
