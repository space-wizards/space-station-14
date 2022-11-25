using System.Linq;
using System.Threading;
using Content.Server.Atmos.EntitySystems;
using Content.Server.DoAfter;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Mech;

public sealed class MechSystem : SharedMechSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MechComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);
        SubscribeLocalEvent<MechComponent, MechOpenUiEvent>(OnOpenUi);
        SubscribeLocalEvent<MechComponent, MechEntryFinishedEvent>(OnEntryFinished);
        SubscribeLocalEvent<MechComponent, MechEntryCanclledEvent>(OnEntryExitCancelled);
        SubscribeLocalEvent<MechComponent, MechExitFinishedEvent>(OnExitFinished);
        SubscribeLocalEvent<MechComponent, MechExitCanclledEvent>(OnEntryExitCancelled);

        SubscribeLocalEvent<SharedMechComponent, MechEquipmentToggleMessage>(OnEnableEquipmentMessage);
        SubscribeLocalEvent<SharedMechComponent, MechEquipmentRemoveMessage>(OnRemoveEquipmentMessage);

        SubscribeLocalEvent<MechPilotComponent, InhaleLocationEvent>(OnInhale);
        SubscribeLocalEvent<MechPilotComponent, ExhaleLocationEvent>(OnExhale);
        SubscribeLocalEvent<MechPilotComponent, AtmosExposedGetAirEvent>(OnExpose);
    }

    private void OnMapInit(EntityUid uid, MechComponent component, MapInitEvent args)
    {
        var xform = Transform(uid);
        foreach (var ent in component.StartingEquipment.Select(equipment => Spawn(equipment, xform.Coordinates)))
        {
            component.EquipmentContainer.Insert(ent);
        }

        component.Integrity = component.MaxIntegrity;
        component.Energy = component.MaxEnergy;

        Dirty(component);
    }

    private void OnEnableEquipmentMessage(EntityUid uid, SharedMechComponent component, MechEquipmentToggleMessage args)
    {
        if (!Exists(args.Equipment) || Deleted(args.Equipment))
            return;

        var ev = new EquipmentGetInformationEvent(new MechEquipmentUiInformation(args.Equipment));
        RaiseLocalEvent(args.Equipment, ref ev);

        if (!ev.Information.CanBeEnabled) //assure that this is valid
            return;

        var toggleEv = new MechEquipmentToggleEvent(args.Enabled);
        RaiseLocalEvent(args.Equipment, ref toggleEv);
    }

    private void OnRemoveEquipmentMessage(EntityUid uid, SharedMechComponent component, MechEquipmentRemoveMessage args)
    {
        if (!Exists(args.Equipment) || Deleted(args.Equipment))
            return;

        var ev = new EquipmentGetInformationEvent(new MechEquipmentUiInformation(args.Equipment));
        RaiseLocalEvent(args.Equipment, ref ev);

        if (!ev.Information.CanBeRemoved) //assure that this is valid
            return;

        if (!component.EquipmentContainer.ContainedEntities.Contains(args.Equipment))
            return;

        RemoveEquipment(uid, args.Equipment, component);
    }

    private void OnOpenUi(EntityUid uid, MechComponent component, MechOpenUiEvent args)
    {
        args.Handled = true;
        ToggleMechUi(uid, component);
    }

    private void OnAlternativeVerb(EntityUid uid, MechComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (CanInsert(uid, args.User, component))
        {
            var enterVerb = new AlternativeVerb
            {
                Text = Loc.GetString("mech-verb-enter"),
                Act = () =>
                {
                    if (component.EntryTokenSource != null)
                        return;
                    component.EntryTokenSource = new CancellationTokenSource();
                    _doAfter.DoAfter(new DoAfterEventArgs(args.User, component.EntryDelay, component.EntryTokenSource.Token, uid)
                    {
                        BreakOnUserMove = true,
                        BreakOnStun = true,
                        TargetFinishedEvent = new MechEntryFinishedEvent(args.User),
                        TargetCancelledEvent = new MechEntryCanclledEvent()
                    });
                }
            };
            var openUiVerb = new AlternativeVerb //can't hijack someone else's mech
            {
                Act = () => ToggleMechUi(uid, component, args.User),
                Text = Loc.GetString("mech-ui-open-verb")
            };
            args.Verbs.Add(enterVerb);
            args.Verbs.Add(openUiVerb);
        }
        else if (!IsEmpty(component))
        {
            var ejectVerb = new AlternativeVerb
            {
                Text = Loc.GetString("mech-verb-exit"),
                Priority = 1, // Promote to top to make ejecting the ALT-click action
                Act = () =>
                {
                    if (component.EntryTokenSource != null)
                        return;
                    var delay = component.ExitDelay;
                    if (args.User == component.PilotSlot.ContainedEntity)
                        delay *= 0.5f;

                    component.EntryTokenSource = new CancellationTokenSource();
                    _doAfter.DoAfter(new DoAfterEventArgs(args.User, delay, component.EntryTokenSource.Token, uid)
                    {
                        BreakOnUserMove = true,
                        BreakOnTargetMove = true,
                        BreakOnStun = true,
                        TargetFinishedEvent = new MechExitFinishedEvent(),
                        TargetCancelledEvent = new MechExitCanclledEvent()
                    });
                }
            };
            args.Verbs.Add(ejectVerb);
        }
    }

    private void OnEntryFinished(EntityUid uid, MechComponent component, MechEntryFinishedEvent args)
    {
        component.EntryTokenSource = null;
        TryInsert(uid, args.User, component);
    }

    private void OnExitFinished(EntityUid uid, MechComponent component, MechExitFinishedEvent args)
    {
        component.EntryTokenSource = null;
        TryEject(uid, component);
    }

    private void OnEntryExitCancelled(EntityUid uid, MechComponent component, EntityEventArgs args)
    {
        component.EntryTokenSource = null;
    }

    private void ToggleMechUi(EntityUid uid, MechComponent? component = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref component))
            return;
        user ??= component.PilotSlot.ContainedEntity;
        if (user == null)
            return;

        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        UpdateUserInterface(uid, component);
        _ui.TryToggleUi(uid, MechUiKey.Key, actor.PlayerSession);
    }

    public override void UpdateUserInterface(EntityUid uid, SharedMechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        base.UpdateUserInterface(uid, component);

        var state = new MechBoundUserInterfaceState();
        foreach (var equipment in component.EquipmentContainer.ContainedEntities)
        {
            var ev = new EquipmentGetInformationEvent(new MechEquipmentUiInformation(equipment));
            RaiseLocalEvent(equipment, ref ev);

            state.EquipmentInfo.Add(ev.Information);
        }

        _ui.TrySetUiState(uid, MechUiKey.Key, state);
    }

    public override bool TryInsert(EntityUid uid, EntityUid? toInsert, SharedMechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!base.TryInsert(uid, toInsert, component))
            return false;

        var mech = (MechComponent) component;

        if (mech.Airtight)
        {
            var coordinates = Transform(uid).MapPosition;
            if (_map.TryFindGridAt(coordinates, out var grid))
            {
                var tile = grid.GetTileRef(coordinates);

                if (_atmosphere.GetTileMixture(tile.GridUid, null, tile.GridIndices, true) is {} environment)
                {
                    _atmosphere.Merge(mech.Air, environment.RemoveVolume(MechComponent.GasMixVolume));
                }
            }
        }
        return true;
    }

    public override bool TryEject(EntityUid uid, SharedMechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!base.TryEject(uid, component))
            return false;

        var mech = (MechComponent) component;

        if (mech.Airtight)
        {
            var coordinates = Transform(uid).MapPosition;
            if (_map.TryFindGridAt(coordinates, out var grid))
            {
                var tile = grid.GetTileRef(coordinates);

                if (_atmosphere.GetTileMixture(tile.GridUid, null, tile.GridIndices, true) is {} environment)
                {
                    _atmosphere.Merge(environment, mech.Air);
                    mech.Air.Clear();
                }
            }
        }

        return true;
    }

    #region Atmos Handling
    private void OnInhale(EntityUid uid, MechPilotComponent component, InhaleLocationEvent args)
    {
        if (!TryComp<MechComponent>(component.Mech, out var mech))
            return;

        if (mech.Airtight)
            args.Gas = mech.Air;
    }

    private void OnExhale(EntityUid uid, MechPilotComponent component, ExhaleLocationEvent args)
    {
        if (!TryComp<MechComponent>(component.Mech, out var mech))
            return;

        if (mech.Airtight)
            args.Gas = mech.Air;
    }

    private void OnExpose(EntityUid uid, MechPilotComponent component, ref AtmosExposedGetAirEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<MechComponent>(component.Mech, out var mech))
            return;

        args.Gas = mech.Airtight ? mech.Air : _atmosphere.GetContainingMixture(component.Mech);

        args.Handled = true;
    }
    #endregion
}
