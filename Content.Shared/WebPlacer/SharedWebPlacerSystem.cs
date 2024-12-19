using Content.Shared.Actions;

namespace Content.Shared.WebPlacer;

/// <summary>
/// Gives the component owner (probably a spider) an action to spawn entites around itself. Spawning handled by <see cref="WebPlacerSystem"/>.
/// </summary>
public abstract class SharedWebPlacerSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WebPlacerComponent, MapInitEvent>(OnInit);
    }

    private void OnInit(EntityUid uid, WebPlacerComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ref component.ActionEntity, component.SpawnWebAction, uid);
    }
}
