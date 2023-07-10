using Content.Server.Administration;
using Content.Server.Mind.Components;
using Content.Server.Station.Systems;
using Content.Shared.Interaction;
using Content.Shared.Verbs;

namespace Content.Server._FTL.ShipRename;

/// <summary>
/// This handles renaming ships
/// </summary>
public sealed class ShipRenameSystem : EntitySystem
{
    [Dependency] private readonly QuickDialogSystem _quickDialogSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ShipRenameComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ShipRenameComponent, GetVerbsEvent<ActivationVerb>>(OnGetVerbs);
    }

    private void OnGetVerbs(EntityUid uid, ShipRenameComponent component, GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        args.Verbs.Add(new ActivationVerb
        {
            Text = Loc.GetString("ship-rename-verb-message"),
            Act = () =>
            {
                if (!component.GridId.HasValue)
                    return;
                if (!TryComp<MindContainerComponent>(args.User, out var mind))
                    return;
                if (!mind.HasMind)
                    return;
                if (mind.Mind.Session == null)
                    return;
                _quickDialogSystem.OpenDialog(mind.Mind.Session, Loc.GetString("ship-rename-popup-title"),
                    Loc.GetString("ship-rename-popup-prompt"),
                    (string name) =>
                    {
                        var finalString = name.Trim();
                        var station = _stationSystem.GetOwningStation(component.GridId.Value);
                        if (!station.HasValue)
                            return;
                        if (string.IsNullOrWhiteSpace(finalString))
                            return;
                        _stationSystem.RenameStation(station.Value, finalString);
                        QueueDel(uid);
                    });
            }
        });
    }

    private void OnComponentInit(EntityUid uid, ShipRenameComponent component, ComponentInit args)
    {
        component.GridId = Transform(uid).GridUid;
    }
}
