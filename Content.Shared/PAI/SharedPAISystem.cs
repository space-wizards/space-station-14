using Content.Shared.Actions;

namespace Content.Shared.PAI;

/// <summary>
/// pAIs, or Personal AIs, are essentially portable ghost role generators.
/// In their current implementation, they create a ghost role anyone can access,
/// and that a player can also "wipe" (reset/kick out player).
/// Theoretically speaking pAIs are supposed to use a dedicated "offer and select" system,
///  with the player holding the pAI being able to choose one of the ghosts in the round.
/// This seems too complicated for an initial implementation, though,
///  and there's not always enough players and ghost roles to justify it.
/// </summary>
public abstract class SharedPAISystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PAIComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PAIComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMapInit(Entity<PAIComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ent.Comp.ShopActionId);
    }

    private void OnShutdown(Entity<PAIComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent, ent.Comp.ShopAction);
    }
}
public sealed partial class PAIShopActionEvent : InstantActionEvent
{
}
