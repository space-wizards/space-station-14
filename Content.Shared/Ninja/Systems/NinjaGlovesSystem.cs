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
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Ninja.Components;
using Content.Shared.Popups;
using Content.Shared.Research.Components;
using Content.Shared.Tag;
using Content.Shared.Toggleable;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Ninja.Systems;

public abstract class NinjaGlovesSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doafter = default!;
    [Dependency] private readonly SharedElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedNinjaSystem _ninja = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TagSystem _tags = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaGlovesComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<NinjaGlovesComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<NinjaGlovesComponent, ToggleNinjaGlovesEvent>(OnToggle);
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
        if (comp.Enabled)
        {
            comp.Enabled = false;
            _popups.PopupEntity(Loc.GetString("ninja-gloves-off"), user, user);
        }
    }

	private void OnToggleAction(EntityUid uid, NinjaGlovesComponent comp, ToggleActionEvent args)
	{
		if (args.Handled)
			return;

		args.Handled = true;

		if (!HasComp<NinjaComponent>(user))
			return;

		comp.Enabled = !comp.Enabled;
	}

    private void OnGetItemActions(EntityUid uid, NinjaGlovesComponent comp, GetItemActionsEvent args)
    {
        _ninja.AssignGloves(ninja, uid);
        args.Actions.Add(comp.ToggleAction);
    }

    private void OnToggle(EntityUid uid, NinjaGlovesComponent comp, ToggleNinjaGlovesEvent args)
    {
        var user = args.Performer;
        // need to wear suit to enable gloves
        if (!TryComp<NinjaComponent>(user, out var ninja)
            || ninja.Suit == null
            || !HasComp<NinjaSuitComponent>(ninja.Suit.Value))
        {
            _popups.PopupEntity(Loc.GetString("ninja-gloves-not-wearing-suit"), user, user);
            return;
        }

        comp.Enabled = !comp.Enabled;
        var message = Loc.GetString(comp.Enabled ? "ninja-gloves-on" : "ninja-gloves-off");
        _popups.PopupEntity(message, user, user);

		// set up interaction relay for handling glove abilities
        if (comp.Enabled)
        {
        	_interaction.SetRelay(user, uid, EnsureComp<InteractionRelayComponent>(user));
        }
        else
        {
        	_interaction.SetRelay(user, uid, null);
        }
    }

    private void OnExamine(EntityUid uid, NinjaGlovesComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushText(Loc.GetString(comp.Enabled ? "ninja-gloves-examine-on" : "ninja-gloves-examine-off"));
    }

    private void OnUnequipped(EntityUid uid, NinjaGlovesComponent comp, GotUnequippedEvent args)
    {
		if (TryComp<NinjaComponent>(args.Equipee, out var ninja))
	        _ninja.AssignGloves(ninja, null);
    }

    private void EndedDoAfter(EntityUid uid, NinjaGlovesComponent comp, DoAfterEvent args)
    {
        comp.Busy = false;
    }

    private void OnDoorjack(EntityUid uid, NinjaDoorjackComponent comp, InteractionAttemptEvent args)
    {
        if (args.Handled || args.Target == null)
            return;

        var user = args.User;
        var target = args.Target.Value;

		// only allowed to emag non-immune doors
        if (!HasComp<DoorComponent>(target) || _tags.HasTag(target, comp.EmagImmuneTag))
            return;

        var handled = _emag.DoEmagEffect(user, target);
        if (!handled)
            return;

        _popups.PopupEntity(Loc.GetString("ninja-doorjack-success", ("target", Identity.Entity(target, EntityManager))), user,
            user, PopupType.Medium);
        _adminLogger.Add(LogType.Emag, LogImpact.High, $"{ToPrettyString(user):player} doorjacked {ToPrettyString(target):target}");

        args.Handled = true;
    }

	private void OnStun(EntityUid uid, NinjaStunComponent comp, InteractionAttemptEvent args)
	{
		if (args.Handled || args.Target == null)
			return;

        var user = args.User;
        var target = args.Target.Value;
        // battery can't be predicted since it's serverside
        if (user == target || _net.Client || !HasComp<StaminaComponent>(target))
        	return;

        // take charge from battery
        if (!_ninja.TryUseCharge(user, comp.StunCharge))
        {
            _popups.PopupEntity(Loc.GetString("ninja-no-power"), user, user);
            return;
        }

        // not holding hands with target so insuls don't matter
        args.Handled = _electrocution.TryDoElectrocution(target, uid, comp.StunDamage, comp.StunTime, false, ignoreInsulation: true);
        return;
    }

	private void OnDrain(EntityUid uid, NinjaDrainComponent comp, InteractionAttemptEvent args)
	{
		if (args.Handled
			|| args.Target == null
			|| !TryComp<NinjaGlovesComponent>(uid, out var gloves))
			return;

		var user = args.User;
		var target = args.Target.Value;
        if (!HasComp<PowerNetworkBatteryComponent>(target) || !HasComp<BatteryComponent>(target))
        	return;

        // nicer for spam-clicking to not open apc ui so cancel it
        if (gloves.Busy)
        {
            args.Handled = true;
		    return;
        }

        var doafterArgs = new DoAfterEventArgs(user, comp.DrainTime, target: target, used: uid)
        {
            BreakOnDamage = true,
            BreakOnStun = true,
            BreakOnUserMove = true,
            MovementThreshold = 0.5f
        };

        _doafter.DoAfter(doafterArgs);
        gloves.Busy = true;
        args.Handled = true;
    }

    private void OnDrainDoAfter(EntityUid uid, NinjaDrainComponent comp, DoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var user = args.Args.User;
        var target = args.Args.Target;

        if (!_ninja.GetNinjaBattery(user, out var suitBattery))
            // took suit off or something, ignore draining
            return;

        if (!TryComp<BatteryComponent>(target, out var battery) || !TryComp<PowerNetworkBatteryComponent>(target, out var pnb))
            return;

        if (suitBattery.IsFullyCharged)
        {
            _popups.PopupEntity(Loc.GetString("ninja-drain-full"), user, user, PopupType.Medium);
            return;
        }

        if (MathHelper.CloseToPercent(battery.CurrentCharge, 0))
        {
            _popups.PopupEntity(Loc.GetString("ninja-drain-empty", ("battery", target)), user, user, PopupType.Medium);
            return;
        }

        var available = battery.CurrentCharge;
        var required = suitBattery.MaxCharge - suitBattery.CurrentCharge;
        // higher tier storages can charge more
        var maxDrained = pnb.MaxSupply * comp.DrainTime;
        var input = Math.Min(Math.Min(available, required / comp.DrainEfficiency), maxDrained);
        if (battery.TryUseCharge(input))
        {
            var output = input * comp.DrainEfficiency;
            suitBattery.CurrentCharge += output;
            _popups.PopupEntity(Loc.GetString("ninja-drain-success", ("battery", target)), user, user);
            // TODO: spark effects
            _audio.PlayPvs(comp.SparkSound, uid);
        }
    }

	private void OnDownload(EntityUid uid, NinjaDownloadComponent comp, InteractionAttemptEvent args)
	{
		if (args.Handled
			|| args.Target == null
			|| !TryComp<NinjaGlovesComponent>(uid, out var gloves))
			return;

		var user = args.User;
		var target = args.Target.Value;
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
        args.Handled = true;
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
		var user = args.User;
		if (args.Handled
			|| args.Target == null
			|| !TryComp<NinjaGlovesComponent>(uid, out var gloves)
			|| !TryComp<NinjaComponent>(user, out var ninja))
			return;

		var target = args.Target.Value;
        if (gloves.Busy || !HasComp<CommunicationsConsoleComponent>(target))
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
        args.Handled = true;
    }

    private void OnTerrorDoAfter(EntityUid uid, NinjaTerrorComponent comp, DoAfterEvent args)
    {
        var target = args.Args.Target;
        if (args.Cancelled || args.Handled || !HasComp<CommunicationsConsoleComponent>(target))
            return;

        var user = args.Args.User;
        if (!TryComp<NinjaComponent>(user, out var ninja) || ninja.CalledInThreat)
            return;

        _ninja.CallInThreat(ninja);
    }
}
