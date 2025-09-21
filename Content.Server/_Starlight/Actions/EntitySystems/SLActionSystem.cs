using Content.Server.Actions;
using Content.Shared._Starlight.Medical.Limbs;
using Content.Shared.Actions;
using Content.Shared.Starlight.Abstract.Codegen;

namespace Content.Server._Starlight.Actions.EntitySystems;

[GenerateLocalSubscriptions<IWithAction>]
public sealed partial class SLActionSystem : EntitySystem
{
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllWithAction<MapInitEvent>(InitActionGeneric);
    }

    private void InitActionGeneric(Entity<IWithAction> ent, ref MapInitEvent ev)
    {
        var actionEnt = ent.Comp.ActionEntity; // (╯‵□′)╯︵┻━┻

        if (_actionContainer.EnsureAction(ent, ref actionEnt, out var action, ent.Comp.Action)
            && ent.Comp.EntityIcon)
            _actions.SetEntityIcon((actionEnt.Value, action), ent);

        ent.Comp.ActionEntity = actionEnt; //(ヘ･_･)ヘ┳━┳
    }
}
