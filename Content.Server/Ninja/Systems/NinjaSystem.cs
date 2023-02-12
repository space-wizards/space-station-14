using Content.Server.Actions;
using Content.Server.GameTicking.Rules;
using Content.Server.Ninja.Components;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Emag.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.GameStates;

namespace Content.Server.Ninja.Systems;

public sealed partial class NinjaSystem : GameRuleSystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly PopupSystem _popups = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;
    [Dependency] private readonly TagSystem _tags = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceNinjaGlovesComponent, GotEquippedEvent>(OnGlovesEquipped);
        SubscribeLocalEvent<SpaceNinjaGlovesComponent, GotUnequippedEvent>(OnGlovesUnequipped);
        SubscribeLocalEvent<SpaceNinjaGlovesComponent, NinjaEmagEvent>(OnEmagAction);

        // TODO: maybe have suit activation stuff
        SubscribeLocalEvent<SpaceNinjaSuitComponent, GotEquippedEvent>(OnSuitEquipped);
        SubscribeLocalEvent<SpaceNinjaSuitComponent, GotUnequippedEvent>(OnSuitUnequipped);
        SubscribeLocalEvent<SpaceNinjaSuitComponent, ToggleCloakEvent>(OnToggleCloakAction);

        SubscribeLocalEvent<SpaceNinjaComponent, AttackedEvent>(OnNinjaAttacked);
    }

    private void OnGlovesEquipped(EntityUid uid, SpaceNinjaGlovesComponent comp, GotEquippedEvent args)
    {
        var user = args.Equipee;
        if (IsNinja(user) && TryComp<ActionsComponent>(user, out var actions))
        {
            _actions.AddAction(user, comp.EmagAction, uid, actions);
            // TODO: power drain, stun abilities
        }
    }

    private void OnGlovesUnequipped(EntityUid uid, SpaceNinjaGlovesComponent comp, GotUnequippedEvent args)
    {
        _actions.RemoveProvidedActions(args.Equipee, uid);
    }

    // stripped down version of EmagSystem's emagging code
    private void OnEmagAction(EntityUid uid, SpaceNinjaGlovesComponent component, NinjaEmagEvent args)
    {
        var target = args.Target;
        if (_tags.HasTag(target, component.EmagImmuneTag))
            return;

        var user = args.Performer;
        var handled = _emag.DoEmagEffect(user, target);
        if (!handled)
            return;

        _popups.PopupEntity(Loc.GetString("emag-success", ("target", Identity.Entity(target, EntityManager))), user,
            user, PopupType.Medium);
        args.Handled = true;

        _adminLogger.Add(LogType.Emag, LogImpact.High, $"{ToPrettyString(user):player} emagged {ToPrettyString(target):target}");
    }

    private void OnSuitEquipped(EntityUid uid, SpaceNinjaSuitComponent comp, GotEquippedEvent args)
    {
        var user = args.Equipee;
        if (TryComp<SpaceNinjaComponent>(user, out var ninja) && TryComp<ActionsComponent>(user, out var actions))
        {
            _actions.AddAction(user, comp.ToggleCloakAction, uid, actions);
            // TODO: emp ability

            // mark the user as wearing this suit, used when being attacked
            ninja.Suit = uid;

            // initialize stealth
            AddComp<StealthComponent>(user);
            UpdateStealth(user, comp.Cloaked);
        }
    }

    private void OnSuitUnequipped(EntityUid uid, SpaceNinjaSuitComponent comp, GotUnequippedEvent args)
    {
        var user = args.Equipee;
        _actions.RemoveProvidedActions(user, uid);

        // mark the user as not wearing a suit
        if (TryComp<SpaceNinjaComponent>(user, out var ninja))
        {
            ninja.Suit = null;
        }

        // force uncloak
        comp.Cloaked = false;
        RemComp<StealthComponent>(user);
    }

    private void OnToggleCloakAction(EntityUid uid, SpaceNinjaSuitComponent comp, ToggleCloakEvent args)
    {
        comp.Cloaked = !comp.Cloaked;
        UpdateStealth(args.Performer, comp.Cloaked);
        args.Handled = true;
    }

    private void UpdateStealth(EntityUid user, bool cloaked)
    {
        if (TryComp<StealthComponent>(user, out var stealth))
        {
            if (cloaked)
                // slightly visible, but doesn't change when moving so it's ok
                _stealth.SetVisibility(user, stealth.MinVisibility + 0.25f, stealth);

            _stealth.SetEnabled(user, cloaked, stealth);
        }
    }

    private void OnNinjaAttacked(EntityUid uid, SpaceNinjaComponent comp, AttackedEvent args)
    {
        if (comp.Suit != null && TryComp<SpaceNinjaSuitComponent>(comp.Suit, out var suit))
        {
            if (suit.Cloaked)
            {
                suit.Cloaked = false;
                UpdateStealth(uid, false);
                // TODO: disable all actions for 5 seconds
            }
        }
    }

    private bool IsNinja(EntityUid user)
    {
        return HasComp<SpaceNinjaComponent>(user);
    }
}
