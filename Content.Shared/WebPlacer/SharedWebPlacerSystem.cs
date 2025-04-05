using Content.Shared.Actions;

namespace Content.Shared.WebPlacer;

/// <summary>
/// Gives the component owner (probably a spider) an action to spawn entites around itself.
/// Spawning is handled by <see cref="WebPlacerSystem"/>.
/// </summary>
/// <seealso cref="WebPlacerComponent"/>
public abstract class SharedWebPlacerSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WebPlacerComponent, MapInitEvent>(OnInit);
    }

    private void OnInit(Entity<WebPlacerComponent> webPlacer, ref MapInitEvent args)
    {
        _action.AddAction(webPlacer.Owner, ref webPlacer.Comp.ActionEntity, webPlacer.Comp.SpawnWebAction, webPlacer.Owner);
    }
}
