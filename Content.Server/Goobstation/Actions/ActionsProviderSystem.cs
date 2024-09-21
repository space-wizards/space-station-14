using Content.Shared.Actions;

namespace Content.Server.Actions;

public sealed partial class ActionsProviderSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionsProviderComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<ActionsProviderComponent> ent, ref ComponentInit args)
    {
        foreach (var action in ent.Comp.Actions)
            _actions.AddAction(ent, action);
    }
}
