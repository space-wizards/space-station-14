using Content.Shared.Actions;
using Content.Shared.CombatMode;
using Content.Shared.Doors.Components;
using Content.Shared.DoAfter;
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
using Content.Shared.Timing;
using Content.Shared.Toggleable;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Ninja.Systems;

public abstract class SharedNinjaGlovesSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] protected readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] protected readonly SharedInteractionSystem Interaction = default!;
    [Dependency] private readonly SharedSpaceNinjaSystem _ninja = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaGlovesComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<NinjaGlovesComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<NinjaGlovesComponent, GotUnequippedEvent>(OnUnequipped);

        // TODO: maybe move into r&d server???
        SubscribeLocalEvent<NinjaDownloadComponent, InteractionAttemptEvent>(OnDownload);
        SubscribeLocalEvent<NinjaDownloadComponent, DownloadDoAfterEvent>(OnDownloadDoAfter);

        // TODO: move into comms
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

            Appearance.SetData(uid, ToggleVisuals.Toggled, false);
            RemComp<InteractionRelayComponent>(user);
            Popup.PopupClient(Loc.GetString("ninja-gloves-off"), user, user);

            RemComp<BatteryDrainerComponent>(user);
            RemComp<EmagProviderComponent>(user);
            RemComp<StunProviderComponent>(user);
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
            && TryComp<SpaceNinjaComponent>(gloves.User, out var ninja)
            && ninja.Suit != null
            && !_useDelay.ActiveDelay(gloves.User.Value)
            && TryComp<HandsComponent>(gloves.User, out var hands)
            && hands.ActiveHandEntity == null)
        {
            user = gloves.User.Value;
            target = args.Target.Value;

            if (Interaction.InRangeUnobstructed(user, target))
                return true;
        }

        gloves = null;
        user = target = EntityUid.Invalid;
        return false;
    }

    /// <summary>
    /// GloveCheck but for abilities stored on the player, skips some checks.
    /// Intended to be more generic, doesn't require the user to be a ninja or have any ninja equipment.
    /// </summary>
    public bool AbilityCheck(EntityUid uid, InteractionAttemptEvent args, out EntityUid target)
    {
        if (args.Target != null
            && _timing.IsFirstTimePredicted
            && !_combatMode.IsInCombatMode(uid)
            && !_useDelay.ActiveDelay(uid)
            && TryComp<HandsComponent>(uid, out var hands)
            && hands.ActiveHandEntity == null)
        {
            target = args.Target.Value;

            if (Interaction.InRangeUnobstructed(uid, target))
                return true;
        }

        target = EntityUid.Invalid;
        return false;
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
