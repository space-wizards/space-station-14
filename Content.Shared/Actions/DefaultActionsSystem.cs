namespace Content.Shared.Actions;

/// <summary>
/// Handles adding actions from <see cref="DefaultActionsComponent"/> on mapinit.
/// </summary>
public sealed class DefaultActionsSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DefaultActionsComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<DefaultActionsComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<ActionsComponent>(ent, out var actions))
            return;

        foreach (var pair in ent.Comp.Actions)
        {
            _actions.AddAction(performer: ent, ref pair.Entity, pair.Id, container: ent, actions);
        }
    }
}
