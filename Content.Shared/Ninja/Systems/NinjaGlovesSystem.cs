using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.CombatMode;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.DoAfter;
using Content.Shared.Electrocution;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Ninja.Components;
using Content.Shared.Popups;
using Content.Shared.Research.Components;
using Content.Shared.Tag;
using Content.Shared.Timing;
using Content.Shared.Toggleable;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Ninja.Systems;

public abstract class SharedNinjaGlovesSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] protected readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedNinjaSystem _ninja = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaGlovesComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<NinjaGlovesComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<NinjaGlovesComponent, ToggleActionEvent>(OnToggleAction);
        SubscribeLocalEvent<NinjaGlovesComponent, GotUnequippedEvent>(OnUnequipped);

        SubscribeLocalEvent<NinjaDoorjackComponent, InteractionAttemptEvent>(OnDoorjack);

        SubscribeLocalEvent<NinjaStunComponent, InteractionAttemptEvent>(OnStun);

        SubscribeLocalEvent<NinjaDrainComponent, InteractionAttemptEvent>(OnDrain);
        SubscribeLocalEvent<NinjaDrainComponent, DrainDoAfterEvent>(OnDrainDoAfter);

        SubscribeLocalEvent<NinjaDownloadComponent, InteractionAttemptEvent>(OnDownload);
        SubscribeLocalEvent<NinjaDownloadComponent, DownloadDoAfterEvent>(OnDownloadDoAfter);

        SubscribeLocalEvent<NinjaTerrorComponent, InteractionAttemptEvent>(OnTerror);
        SubscribeLocalEvent<NinjaTerrorComponent, TerrorDoAfterEvent>(OnTerrorDoAfter);
    }

    /// <summary>
    /// Disable glove abilities and show the popup if they were enabled previously.
    /// </summary>
    public void DisableGloves(NinjaGlovesComponent comp, EntityUid user)
    {
        if (comp.User != null)
        {
            comp.User = null;
            _popups.PopupEntity(Loc.GetString("ninja-gloves-off"), user, user);
        }
    }

    private void OnGetItemActions(EntityUid uid, NinjaGlovesComponent comp, GetItemActionsEvent args)
    {
        args.Actions.Add(comp.ToggleAction);
    }

    private void OnToggleAction(EntityUid uid, NinjaGlovesComponent comp, ToggleActionEvent args)
    {
        // client prediction desyncs it hard
        if (args.Handled || !_timing.IsFirstTimePredicted)
            return;

        args.Handled = true;

        var user = args.Performer;
        // need to wear suit to enable gloves
        if (!TryComp<NinjaComponent>(user, out var ninja)
            || ninja.Suit == null
            || !HasComp<NinjaSuitComponent>(ninja.Suit.Value))
        {
            ClientPopup(Loc.GetString("ninja-gloves-not-wearing-suit"), user);
            return;
        }

        var enabling = comp.User == null;
        var message = Loc.GetString(enabling ? "ninja-gloves-on" : "ninja-gloves-off");
        ClientPopup(message, user);

        if (enabling)
        {
            comp.User = user;
            _ninja.AssignGloves(ninja, uid);
            // set up interaction relay for handling glove abilities, comp.User is used to see the actual user of the events
            _interaction.SetRelay(user, uid, EnsureComp<InteractionRelayComponent>(user));
        }
        else
        {
            comp.User = null;
            _ninja.AssignGloves(ninja, null);
            RemComp<InteractionRelayComponent>(user);
        }
    }

    private void OnExamined(EntityUid uid, NinjaGlovesComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushText(Loc.GetString(comp.User != null ? "ninja-gloves-examine-on" : "ninja-gloves-examine-off"));
    }

    private void OnUnequipped(EntityUid uid, NinjaGlovesComponent comp, GotUnequippedEvent args)
    {
        comp.User = null;
        if (TryComp<NinjaComponent>(args.Equipee, out var ninja))
            _ninja.AssignGloves(ninja, null);
    }

    /// <summary>
    /// Helper for glove ability handlers, checks gloves, range, combat mode and stuff.
    /// </summary>
    protected bool GloveCheck(EntityUid uid, InteractionAttemptEvent args, [NotNullWhen(true)] out NinjaGlovesComponent? gloves,
        out EntityUid user, out EntityUid target)
    {
        if (args.Target != null && TryComp<NinjaGlovesComponent>(uid, out gloves)
            && gloves.User != null
            && !_combatMode.IsInCombatMode(gloves.User)
            && _timing.IsFirstTimePredicted
            && TryComp<HandsComponent>(gloves.User, out var hands)
            && hands.ActiveHandEntity == null)
        {
            user = gloves.User.Value;
            target = args.Target.Value;

            if (_interaction.InRangeUnobstructed(user, target))
                return true;
        }

        gloves = null;
        user = target = EntityUid.Invalid;
        return false;
    }

    private void OnDoorjack(EntityUid uid, NinjaDoorjackComponent comp, InteractionAttemptEvent args)
    {
        if (!GloveCheck(uid, args, out var gloves, out var user, out var target))
            return;

        // only allowed to emag non-immune doors
        if (!HasComp<DoorComponent>(target) || _tags.HasTag(target, comp.EmagImmuneTag))
            return;

        var handled = _emag.DoEmagEffect(user, target);
        if (!handled)
            return;

        ClientPopup(Loc.GetString("ninja-doorjack-success", ("target", Identity.Entity(target, EntityManager))), user, PopupType.Medium);
        _adminLogger.Add(LogType.Emag, LogImpact.High, $"{ToPrettyString(user):player} doorjacked {ToPrettyString(target):target}");
    }

    private void OnStun(EntityUid uid, NinjaStunComponent comp, InteractionAttemptEvent args)
    {
        if (!GloveCheck(uid, args, out var gloves, out var user, out var target))
            return;

        // short cooldown to prevent instant stunlocking
        if (_useDelay.ActiveDelay(uid))
            return;

        // battery can't be predicted since it's serverside
        if (user == target || _net.IsClient || !HasComp<StaminaComponent>(target))
            return;

        // take charge from battery
        if (!_ninja.TryUseCharge(user, comp.StunCharge))
        {
            _popups.PopupEntity(Loc.GetString("ninja-no-power"), user, user);
            return;
        }

        // not holding hands with target so insuls don't matter
        _electrocution.TryDoElectrocution(target, uid, comp.StunDamage, comp.StunTime, false, ignoreInsulation: true);
        _useDelay.BeginDelay(uid);
    }

    // can't predict PNBC existing so only done on server.
    protected virtual void OnDrain(EntityUid uid, NinjaDrainComponent comp, InteractionAttemptEvent args) { }

    private void OnDrainDoAfter(EntityUid uid, NinjaDrainComponent comp, DrainDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null)
            return;

        _ninja.TryDrainPower(args.User, comp, args.Target.Value);
    }

    private void OnDownload(EntityUid uid, NinjaDownloadComponent comp, InteractionAttemptEvent args)
    {
        if (!GloveCheck(uid, args, out var gloves, out var user, out var target))
            return;

        // can only hack the server, not a random console
        if (!TryComp<TechnologyDatabaseComponent>(target, out var database) || HasComp<ResearchClientComponent>(target))
            return;

        // fail fast if theres no tech right now
        if (database.TechnologyIds.Count == 0)
        {
            ClientPopup(Loc.GetString("ninja-download-fail"), user);
            return;
        }

        var doAfterArgs = new DoAfterArgs(user, comp.DownloadTime, new DownloadDoAfterEvent(), target: target, used: uid, eventTarget: uid)
        {
            BreakOnDamage = true,
            BreakOnUserMove = true,
            MovementThreshold = 0.5f,
            CancelDuplicate = false
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Cancel();
    }

    private void OnDownloadDoAfter(EntityUid uid, NinjaDownloadComponent comp, DownloadDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var user = args.User;
        var target = args.Target;

        if (!TryComp<NinjaComponent>(user, out var ninja)
            || !TryComp<TechnologyDatabaseComponent>(target, out var database))
            return;

        var gained = _ninja.Download(ninja, database.TechnologyIds);
        var str = gained == 0
            ? Loc.GetString("ninja-download-fail")
            : Loc.GetString("ninja-download-success", ("count", gained), ("server", target));

        _popups.PopupEntity(str, user, user, PopupType.Medium);
    }

    private void OnTerror(EntityUid uid, NinjaTerrorComponent comp, InteractionAttemptEvent args)
    {
        if (!GloveCheck(uid, args, out var gloves, out var user, out var target)
            || !TryComp<NinjaComponent>(user, out var ninja))
            return;

        if (!IsCommsConsole(target))
            return;

        // can only do it once
        if (ninja.CalledInThreat)
        {
            _popups.PopupEntity(Loc.GetString("ninja-terror-already-called"), user, user);
            return;
        }

        var doAfterArgs = new DoAfterArgs(user, comp.TerrorTime, new TerrorDoAfterEvent(), target: target, used: uid, eventTarget: uid)
        {
            BreakOnDamage = true,
            BreakOnUserMove = true,
            MovementThreshold = 0.5f,
            CancelDuplicate = false
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        // FIXME: doesnt work, don't show the console popup
        args.Cancel();
    }

    //for some reason shared comms console component isn't a component, so this has to be done server-side
    protected virtual bool IsCommsConsole(EntityUid uid)
    {
        return false;
    }

    private void OnTerrorDoAfter(EntityUid uid, NinjaTerrorComponent comp, TerrorDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var user = args.User;
        if (!TryComp<NinjaComponent>(user, out var ninja) || ninja.CalledInThreat)
            return;

        _ninja.CallInThreat(ninja);
    }

    private void ClientPopup(string msg, EntityUid user, PopupType type = PopupType.Small)
    {
        if (_net.IsClient)
            _popups.PopupEntity(msg, user, user, type);
    }
}
