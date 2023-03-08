using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Communications;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.DoAfter;
using Content.Shared.Electrocution;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Ninja.Components;
using Content.Shared.Popups;
using Content.Shared.Research.Components;
using Content.Shared.Tag;
using Content.Shared.Toggleable;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Ninja.Systems;

public abstract class SharedNinjaGlovesSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] protected readonly SharedDoAfterSystem _doafter = default!;
    [Dependency] private readonly SharedElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedNinjaSystem _ninja = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaGlovesComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<NinjaGlovesComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<NinjaGlovesComponent, ToggleActionEvent>(OnToggleAction);
        SubscribeLocalEvent<NinjaGlovesComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<NinjaGlovesComponent, DoAfterEvent>(EndedDoAfter);

        SubscribeLocalEvent<NinjaDoorjackComponent, InteractionAttemptEvent>(OnDoorjack);

        SubscribeLocalEvent<NinjaStunComponent, InteractionAttemptEvent>(OnStun);

        SubscribeLocalEvent<NinjaDrainComponent, InteractionAttemptEvent>(OnDrain);
        SubscribeLocalEvent<NinjaDrainComponent, DoAfterEvent>(OnDrainDoAfter);

        SubscribeLocalEvent<NinjaDownloadComponent, InteractionAttemptEvent>(OnDownload);
        SubscribeLocalEvent<NinjaDownloadComponent, DoAfterEvent>(OnDownloadDoAfter);

        SubscribeLocalEvent<NinjaTerrorComponent, InteractionAttemptEvent>(OnTerror);
        SubscribeLocalEvent<NinjaTerrorComponent, DoAfterEvent>(OnTerrorDoAfter);
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

    private void EndedDoAfter(EntityUid uid, NinjaGlovesComponent comp, DoAfterEvent args)
    {
        comp.Busy = false;
    }

    /// <summary>
    /// Helper for glove ability handlers, checks gloves and range
    /// </summary>
    protected bool GloveCheck(EntityUid uid, InteractionAttemptEvent args, [NotNullWhen(true)] out NinjaGlovesComponent? gloves,
        out EntityUid user, out EntityUid target)
    {
        if (args.Target != null && TryComp<NinjaGlovesComponent>(uid, out gloves) && gloves.User != null && _timing.IsFirstTimePredicted)
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

        _popups.PopupEntity(Loc.GetString("ninja-doorjack-success", ("target", Identity.Entity(target, EntityManager))), user,
            user, PopupType.Medium);
        _adminLogger.Add(LogType.Emag, LogImpact.High, $"{ToPrettyString(user):player} doorjacked {ToPrettyString(target):target}");
    }

    private void OnStun(EntityUid uid, NinjaStunComponent comp, InteractionAttemptEvent args)
    {
        if (!GloveCheck(uid, args, out var gloves, out var user, out var target))
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
    }

    // can't predict PNBC existing so only done on server.
    protected virtual void OnDrain(EntityUid uid, NinjaDrainComponent comp, InteractionAttemptEvent args) { }

    private void OnDrainDoAfter(EntityUid uid, NinjaDrainComponent comp, DoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        var user = args.Args.User;
        var target = args.Args.Target.Value;

        _ninja.TryDrainPower(user, comp, target);
    }

    private void OnDownload(EntityUid uid, NinjaDownloadComponent comp, InteractionAttemptEvent args)
    {
        if (!GloveCheck(uid, args, out var gloves, out var user, out var target))
            return;

        if (gloves.Busy || !TryComp<TechnologyDatabaseComponent>(target, out var database))
            return;

        // fail fast if theres no tech right now
        if (database.TechnologyIds.Count == 0)
        {
            _popups.PopupEntity(Loc.GetString("ninja-download-fail"), user, user);
            return;
        }

        var doafterArgs = new DoAfterEventArgs(user, comp.DownloadTime, target: target, used: uid)
        {
            BreakOnDamage = true,
            BreakOnStun = true,
            BreakOnUserMove = true,
            MovementThreshold = 0.5f
        };

        _doafter.DoAfter(doafterArgs);
        gloves.Busy = true;
    }

    private void OnDownloadDoAfter(EntityUid uid, NinjaDownloadComponent comp, DoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var user = args.Args.User;
        var target = args.Args.Target;

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

        if (gloves.Busy || !IsCommsConsole(target))
            return;

        // can only do it once
        if (ninja.CalledInThreat)
        {
            _popups.PopupEntity(Loc.GetString("ninja-terror-already-called"), user, user);
            return;
        }

        var doafterArgs = new DoAfterEventArgs(user, comp.TerrorTime, target: target, used: uid)
        {
            BreakOnDamage = true,
            BreakOnStun = true,
            BreakOnUserMove = true,
            MovementThreshold = 0.5f
        };

        _doafter.DoAfter(doafterArgs);
        gloves.Busy = true;
        // don't show the console popup
        args.Cancel();
    }

    //for some reason shared comms console component isn't a component, so this has to be done server-side
    protected virtual bool IsCommsConsole(EntityUid uid)
    {
        return false;
    }

    private void OnTerrorDoAfter(EntityUid uid, NinjaTerrorComponent comp, DoAfterEvent args)
    {
        var target = args.Args.Target;
        if (args.Cancelled || args.Handled || target == null || !IsCommsConsole(target.Value))
            return;

        var user = args.Args.User;
        if (!TryComp<NinjaComponent>(user, out var ninja) || ninja.CalledInThreat)
            return;

        _ninja.CallInThreat(ninja);
    }

    private void ClientPopup(string msg, EntityUid user)
    {
        if (_net.IsClient)
            _popups.PopupEntity(msg, user, user);
    }
}
