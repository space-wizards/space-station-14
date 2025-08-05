using System.Linq;
using System.Reflection;
using Content.Server.Actions;
using Content.Server.Administration.Systems;
using Content.Shared._Starlight.Medical.Limbs;
using Content.Shared.Actions;
using Robust.Shared.Reflection;

namespace Content.Server._Starlight.Actions;
public sealed partial class SLActionSystem : EntitySystem
{
    [Dependency] private readonly IReflectionManager _reflection = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly StarlightEntitySystem _entities = default!;

    private static MethodInfo? s_handlerMethod;
    public override void Initialize() 
    {
        base.Initialize();

        if (s_handlerMethod == null)
        {
            s_handlerMethod = typeof(SLActionSystem).GetMethod(
                nameof(InitActionGeneric),
                BindingFlags.NonPublic | BindingFlags.Instance
            );
        }

        this.SubscribeAllComponents<IWithAction, MapInitEvent>(_reflection, s_handlerMethod!);
    }

    private void InitActionGeneric<T>(Entity<T> ent, ref MapInitEvent ev)
        where T : IWithAction, IComponent
    {
        var actionEnt = ent.Comp.ActionEntity; // (╯‵□′)╯︵┻━┻

        if (_actionContainer.EnsureAction(ent, ref actionEnt, out var action, ent.Comp.Action) 
            && ent.Comp.EntityIcon)
            _actions.SetEntityIcon((actionEnt!.Value, action), ent);

        ent.Comp.ActionEntity = actionEnt; //(ヘ･_･)ヘ┳━┳
    }
}
