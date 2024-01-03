using Content.Server.Actions;
using Content.Shared.Actions;

namespace Content.Server.Geras;

/// <summary>
/// A Geras is the small morph of a slime. This system handles exactly that.
/// </summary>
public sealed class GerasSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GerasComponent, MorphIntoGeras>(OnMorphIntoGeras);
        SubscribeLocalEvent<GerasComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, GerasComponent component, MapInitEvent args)
    {
        // try to add geras action
        _actions.AddAction(uid, ref component.GerasActionEntity, component.GerasAction);
    }

    private void OnMorphIntoGeras(EntityUid uid, GerasComponent component, MorphIntoGeras args)
    {
        Log.Info("wowie");
    }
}
