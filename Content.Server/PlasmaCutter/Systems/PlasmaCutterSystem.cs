using System.Threading;
using Content.Server.Administration.Logs;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Server.PlasmaCutter.Components;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Content.Shared.Audio;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Toggleable;
using Content.Server.Construction.Conditions;
using Content.Server.Storage.Components;
using Microsoft.Extensions.Logging.Abstractions;

namespace Content.Server.PlasmaCutter.Systems;

public sealed class PlasmaCutterSystem : EntitySystem
{
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlasmaCutterComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<PlasmaCutterComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<PlasmaCutterComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<PlasmaCutterComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnMeleeHit(EntityUid uid, PlasmaCutterComponent component, MeleeHitEvent args)
    {
        if (!args.Handled && component.Activated) //Checking if cutter activated and handled by person
        {
            if (component.CurrentAmmo >= 1) //Checking for ammo
            {
                args.BonusDamage += component.ActivatedMeleeDamageBonus; //If all checks done, adding bonus to damage
                component.CurrentAmmo--;
            }
            else
            {
                TurnOff(component); //Else, turn off cutter
            }
        }
    }

    private void OnExamine(EntityUid uid, PlasmaCutterComponent component, ExaminedEvent args)
    {
        var msg = Loc.GetString("pc-component-examine-detail-count",
            ("ammoCount", component.CurrentAmmo));
        args.PushMarkup(msg); //If examined showing how many ammo left
    }

    private void OnUseInHand(EntityUid uid, PlasmaCutterComponent component, UseInHandEvent args)
    {
        //If activated turning off, if not activated turning on
        if (component.Activated)
        {
            TurnOff(component);
        }
        else
        {
            TurnOn(component, args.User);
        }
    }

    private void TurnOff(PlasmaCutterComponent comp)
    {
        if (!comp.Activated) //checks
            return;

        if (TryComp<AppearanceComponent>(comp.Owner, out var appearance) &&
            TryComp<ItemComponent>(comp.Owner, out var item)) //Getting components
        {
            _item.SetHeldPrefix(comp.Owner, "off", item);
            appearance.SetData(ToggleVisuals.Toggled, false); //Setting visuals
        }

        SoundSystem.Play(comp.successSound.GetSound(), Filter.Pvs(comp.Owner), comp.Owner, AudioHelpers.WithVariation(0.25f));

        comp.Activated = false;
    }

    private void TurnOn(PlasmaCutterComponent comp, EntityUid user)
    {
        if (comp.Activated) //checks
            return;
        if (comp.CurrentAmmo <= 0)
            return;

        var playerFilter = Filter.Pvs(comp.Owner, entityManager: EntityManager);
        if (EntityManager.TryGetComponent<AppearanceComponent>(comp.Owner, out var appearance) &&
            EntityManager.TryGetComponent<ItemComponent>(comp.Owner, out var item)) //Getting components
        {
            _item.SetHeldPrefix(comp.Owner, "on", item);
            appearance.SetData(ToggleVisuals.Toggled, true); //Setting visuals
        }

        SoundSystem.Play(comp.successSound.GetSound(), playerFilter, comp.Owner, AudioHelpers.WithVariation(0.25f));

        comp.Activated = true;
    }

    private async void OnAfterInteract(EntityUid uid, PlasmaCutterComponent pc, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (pc.CancelToken != null)
        {
            pc.CancelToken?.Cancel();
            pc.CancelToken = null;
            args.Handled = true;
            return;
        }

        if (!args.ClickLocation.IsValid(EntityManager)) return;

        var clickLocationMod = args.ClickLocation;
        // Initial validity check
        if (!clickLocationMod.IsValid(EntityManager))
            return;
        // Note: Ideally there'd be a better way, but there isn't right now.
        var gridIdOpt = clickLocationMod.GetGridUid(EntityManager);
        if (!(gridIdOpt is EntityUid gridId) || !gridId.IsValid())
        {
            clickLocationMod = clickLocationMod.AlignWithClosestGridTile();
            gridIdOpt = clickLocationMod.GetGridUid(EntityManager);
            // Check if fixing it failed / get final grid ID
            if (!(gridIdOpt is EntityUid gridId2) || !gridId2.IsValid())
                return;
            gridId = gridId2;
        }

        var mapGrid = _mapManager.GetGrid(gridId);
        var tile = mapGrid.GetTileRef(clickLocationMod);
        var snapPos = mapGrid.TileIndicesFor(clickLocationMod);

        args.Handled = true;
        var user = args.User;

        //Using a cutter isn't instantaneous
        pc.CancelToken = new CancellationTokenSource();
        var doAfterEventArgs = new DoAfterEventArgs(user, pc.UseDelay, pc.CancelToken.Token, args.Target)
        {
            BreakOnDamage = true,
            BreakOnStun = true,
            NeedHand = true,
            ExtraCheck = () => IsPCStillValid(pc, args, mapGrid, tile) //All of the sanity checks are here
        };

        var result = await _doAfterSystem.WaitDoAfter(doAfterEventArgs);

        pc.CancelToken = null;

        if (result == DoAfterStatus.Cancelled)
            return;

        if (args.Target != null && EntityManager.TryGetComponent<LockComponent>(args.Target.Value, out var Lock))
        {
            Lock.Locked = false;
            EntityManager.RemoveComponent<LockComponent>(args.Target.Value); //Literally destroys the lock as a tell it was broken
            args.Handled = true;
            SoundSystem.Play(pc.sparksSound.GetSound(), Filter.Pvs(uid, entityManager: EntityManager), pc.Owner);
            pc.CurrentAmmo--;
            return;
        }

        if (!tile.IsBlockedTurf(true)) //Delete the turf
        {
            mapGrid.SetTile(snapPos, Tile.Empty);
            _adminLogger.Add(LogType.PlasmaCutter, LogImpact.High, $"{ToPrettyString(args.User):user} used Plasma Cutter to set grid: {tile.GridUid} tile: {snapPos} to space");
        }
        else //Delete what the user targeted
        {
            if (args.Target is { Valid: true } target)
            {              
                _adminLogger.Add(LogType.PlasmaCutter, LogImpact.High, $"{ToPrettyString(args.User):user} used Plasma Cutter to delete {ToPrettyString(target):target}");
                QueueDel(target); //Deleting target
            }
        }

        SoundSystem.Play(pc.sparksSound.GetSound(), Filter.Pvs(uid, entityManager: EntityManager), pc.Owner);
        pc.CurrentAmmo--;
        if (pc.CurrentAmmo <= 0) //If no ammo, turning off cutter
        {
            TurnOff(pc);
        }
        args.Handled = true;
    }

    private bool IsPCStillValid(PlasmaCutterComponent pc, AfterInteractEvent eventArgs, MapGridComponent mapGrid, TileRef tile)
    {
        //Less expensive checks first. Failing those ones, we need to check that the tile isn't obstructed.
        if (!pc.Activated)
        {
            _popup.PopupEntity(Loc.GetString("pc-component-not-lit"), pc.Owner, eventArgs.User);
            return false;
        }

        if (pc.CurrentAmmo <= 0)
        {
            _popup.PopupEntity(Loc.GetString("pc-component-no-ammo-message"), pc.Owner, eventArgs.User);
            return false;
        }

        var unobstructed = eventArgs.Target == null
            ? _interactionSystem.InRangeUnobstructed(eventArgs.User, mapGrid.GridTileToWorld(tile.GridIndices), popup: true)
            : _interactionSystem.InRangeUnobstructed(eventArgs.User, eventArgs.Target.Value, popup: true);

        if (!unobstructed)
            return false;

        if (tile.Tile.IsEmpty)
        {
            return false;
        }

        //They tried to decon a turf but the turf is blocked
        if (eventArgs.Target == null && tile.IsBlockedTurf(true))
        {
            _popup.PopupEntity(Loc.GetString("pc-component-tile-obstructed-message"), pc.Owner, eventArgs.User);
            return false;
        }

        if (eventArgs.Target != null && EntityManager.TryGetComponent<LockComponent>(eventArgs.Target.Value, out var Lock))
            return true;

        //They tried to decon a non-turf but its not in the whitelist
        if (eventArgs.Target != null && !_tagSystem.HasTag(eventArgs.Target.Value, "PlasmaCuttable"))
        {
            _popup.PopupEntity(Loc.GetString("pc-component-deconstruct-target-not-on-whitelist-message"), pc.Owner, eventArgs.User);
            return false;
        }
        return true;
    }
}
