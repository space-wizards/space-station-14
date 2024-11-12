using Content.Shared.Actions;

namespace Content.Shared.Jaunt;
public sealed class JauntSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JauntComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<JauntComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnInit(Entity<JauntComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.Action, ent.Comp.JauntAction);
    }

    private void OnShutdown(Entity<JauntComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.Action);
    }

}

