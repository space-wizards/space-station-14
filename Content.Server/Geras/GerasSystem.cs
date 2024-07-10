using Content.Server.Polymorph.Systems;
using Content.Shared.Zombies;
using Content.Server.Actions;
using Content.Server.Popups;
using Content.Shared.Geras;
using Robust.Shared.Player;

namespace Content.Server.Geras;

/// <inheritdoc/>
public sealed class GerasSystem : SharedGerasSystem
{
    [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GerasComponent, MorphIntoGeras>(OnMorphIntoGeras);
        SubscribeLocalEvent<GerasComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GerasComponent, EntityZombifiedEvent>(OnZombification);
    }

    private void OnZombification(EntityUid uid, GerasComponent component, EntityZombifiedEvent args)
    {
        _actionsSystem.RemoveAction(uid, component.GerasActionEntity);
    }

    private void OnMapInit(EntityUid uid, GerasComponent component, MapInitEvent args)
    {
        // try to add geras action
        _actionsSystem.AddAction(uid, ref component.GerasActionEntity, component.GerasAction);
    }

    private void OnMorphIntoGeras(EntityUid uid, GerasComponent component, MorphIntoGeras args)
    {
        if (HasComp<ZombieComponent>(uid))
            return; // i hate zomber.

        var ent = _polymorphSystem.PolymorphEntity(uid, component.GerasPolymorphId);

        if (!ent.HasValue)
            return;

        _popupSystem.PopupEntity(Loc.GetString("geras-popup-morph-message-others", ("entity", ent.Value)), ent.Value, Filter.PvsExcept(ent.Value), true);
        _popupSystem.PopupEntity(Loc.GetString("geras-popup-morph-message-user"), ent.Value, ent.Value);

        args.Handled = true;
    }
}
