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
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] protected readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedNinjaSystem _ninja = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] private readonly TagSystem _tags = default!;
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
    public void DisableGloves(EntityUid uid, NinjaGlovesComponent comp)
    {
        if (comp.User != null)
        {
            var user = comp.User.Value;
            comp.User = null;
            Dirty(comp);

            _appearance.SetData(uid, ToggleVisuals.Toggled, false);
            RemComp<InteractionRelayComponent>(user);
            Popup.PopupClient(Loc.GetString("ninja-gloves-off"), user, user);
        }
    }

    /// <summary>
    /// Adds the toggle action when equipped.
    /// Since the event does not pass user this can't be nice and just not add the action if it isn't a ninja wearing but oh well.
    /// </summary>
    private void OnGetItemActions(EntityUid uid, NinjaGlovesComponent comp, GetItemActionsEvent args)
    {
        args.Actions.Add(comp.ToggleAction);
    }

    /// <summary>
    /// Toggle gloves, if the user is a ninja wearing a ninja suit.
    /// </summary>
    private void OnToggleAction(EntityUid uid, NinjaGlovesComponent comp, ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var user = args.Performer;
        // need to wear suit to enable gloves
        if (!TryComp<NinjaComponent>(user, out var ninja)
            || ninja.Suit == null
            || !HasComp<NinjaSuitComponent>(ninja.Suit.Value))
        {
            Popup.PopupEntity(Loc.GetString("ninja-gloves-not-wearing-suit"), user, user);
            return;
        }

        var enabling = comp.User == null;
        _appearance.SetData(uid, ToggleVisuals.Toggled, enabling);
        var message = Loc.GetString(enabling ? "ninja-gloves-on" : "ninja-gloves-off");
        Popup.PopupEntity(message, user, user);

        if (enabling)
        {
            comp.User = user;
            _ninja.AssignGloves(ninja, uid);
            // set up interaction relay for handling glove abilities, comp.User is used to see the actual user of the events
            // FIXME: probably breaks if ninja goes in ripley and exits, pretty minor but its reason to move away from relay if possible
            _interaction.SetRelay(user, uid, EnsureComp<InteractionRelayComponent>(user));
            Dirty(comp);
        }
        else
        {
            DisableGloves(uid, comp);
        }
    }

    /// <summary>
    /// Show if the gloves are enabled when examining.
    /// </summary>
    private void OnExamined(EntityUid uid, NinjaGlovesComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushText(Loc.GetString(comp.User != null ? "ninja-gloves-examine-on" : "ninja-gloves-examine-off"));
    }

    /// <summary>
    /// Disable gloves when unequipped and clean up ninja's gloves reference
    /// </summary>
    private void OnUnequipped(EntityUid uid, NinjaGlovesComponent comp, GotUnequippedEvent args)
    {
        if (comp.User != null)
        {
            var user = comp.User.Value;
            Popup.PopupClient(Loc.GetString("ninja-gloves-off"), user, user);
            DisableGloves(uid, comp);
        }
    }

    /// <summary>
    /// Helper for glove ability handlers, checks gloves, suit, range, combat mode and stuff.
    /// </summary>
    protected bool GloveCheck(EntityUid uid, InteractionAttemptEvent args, [NotNullWhen(true)] out NinjaGlovesComponent? gloves,
        out EntityUid user, out EntityUid target)
    {
        if (args.Target != null && TryComp<NinjaGlovesComponent>(uid, out gloves)
            && gloves.User != null
            && _timing.IsFirstTimePredicted
            && !_combatMode.IsInCombatMode(gloves.User)
            && TryComp<NinjaComponent>(gloves.User, out var ninja)
            && ninja.Suit != null
            && !_useDelay.ActiveDelay(ninja.Suit.Value)
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

    /// <summary>
    /// Emag doors on click when enabled.
    /// </summary>
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

        Popup.PopupClient(Loc.GetString("ninja-doorjack-success", ("target", Identity.Entity(target, EntityManager))), user, user, PopupType.Medium);
        _adminLogger.Add(LogType.Emag, LogImpact.High, $"{ToPrettyString(user):player} doorjacked {ToPrettyString(target):target}");
    }

    /// <summary>
    /// Stun mobs on click when enabled and not on cooldown.
    /// </summary>
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
            Popup.PopupEntity(Loc.GetString("ninja-no-power"), user, user);
            return;
        }

        // not holding hands with target so insuls don't matter
        _electrocution.TryDoElectrocution(target, uid, comp.StunDamage, comp.StunTime, false, ignoreInsulation: true);
        _useDelay.BeginDelay(uid);
    }

    /// <summary>
    /// Start do after for draining a power source.
    /// Can't predict PNBC existing so only done on server.
    /// </summary>
    protected virtual void OnDrain(EntityUid uid, NinjaDrainComponent comp, InteractionAttemptEvent args) { }

    /// <summary>
    /// Drain power from a power source (on server) and repeat if it succeeded.
    /// Client will predict always succeeding since power is serverside.
    /// </summary>
    private void OnDrainDoAfter(EntityUid uid, NinjaDrainComponent comp, DrainDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null)
            return;

        // repeat if there is still power to drain
        args.Repeat = _ninja.TryDrainPower(args.User, comp, args.Target.Value);
    }

    /// <summary>
    /// Start do after for downloading techs from a r&d server.
    /// Will only try if there is at least 1 tech researched.
    /// </summary>
    private void OnDownload(EntityUid uid, NinjaDownloadComponent comp, InteractionAttemptEvent args)
    {
        if (!GloveCheck(uid, args, out var gloves, out var user, out var target))
            return;

        // can only hack the server, not a random console
        if (!TryComp<TechnologyDatabaseComponent>(target, out var database) || HasComp<ResearchClientComponent>(target))
            return;

        // fail fast if theres no tech right now
        if (database.UnlockedTechnologies.Count == 0)
        {
            Popup.PopupClient(Loc.GetString("ninja-download-fail"), user, user);
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

    /// <summary>
    /// Update greentext research nodes information from a server.
    /// Can't predict roles so only done on server.
    /// </summary>
    protected virtual void OnDownloadDoAfter(EntityUid uid, NinjaDownloadComponent comp, DownloadDoAfterEvent args) { }

    /// <summary>
    /// Start do after for calling in a threat.
    /// Can't predict roles for checking if already called.
    /// </summary>
    protected virtual void OnTerror(EntityUid uid, NinjaTerrorComponent comp, InteractionAttemptEvent args) { }

    /// <summary>
    /// Start a gamerule and update greentext information.
    /// Can't predict roles or anything announcements related so only done on server.
    /// </summary>
    protected virtual void OnTerrorDoAfter(EntityUid uid, NinjaTerrorComponent comp, TerrorDoAfterEvent args) { }
}
