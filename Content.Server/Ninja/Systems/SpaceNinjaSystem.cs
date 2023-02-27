using Content.Server.Administration.Commands;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Communications;
using Content.Server.Electrocution;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Configurations;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind.Components;
using Content.Server.Objectives;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.PowerCell;
using Content.Server.Traitor;
using Content.Server.Warps;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;
using Content.Shared.Popups;
using Content.Shared.PowerCell.Components;
using Content.Shared.Research.Components;
using Content.Shared.Roles;
using Content.Shared.Rounding;
using Content.Shared.Stealth.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Content.Server.Ninja.Systems;

public sealed class NinjaSystem : SharedSpaceNinjaSystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly DoAfterSystem _doafter = default!;
    [Dependency] private readonly ElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedSubdermalImplantSystem _implants = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SpaceNinjaSuitSystem _suit = default!;
    [Dependency] private readonly TraitorRuleSystem _traitorRule = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceNinjaComponent, ComponentStartup>(OnNinjaStartup);
        SubscribeLocalEvent<SpaceNinjaComponent, MindAddedMessage>(OnNinjaMindAdded);
        SubscribeLocalEvent<SpaceNinjaComponent, AttackedEvent>(OnNinjaAttacked);
    }

    public override void Update(float frameTime)
    {
        foreach (var ninja in EntityQuery<SpaceNinjaComponent>())
        {
            var uid = ninja.Owner;
            UpdateNinja(uid, ninja, frameTime);
        }
    }

    /// <summary>
    /// Turns the player into a space ninja
    /// </summary>
    public void MakeNinja(Mind.Mind mind)
    {
        if (mind.OwnedEntity == null)
            return;

        // prevent double ninja'ing
        var user = mind.OwnedEntity.Value;
        if (HasComp<SpaceNinjaComponent>(user))
            return;

        AddComp<SpaceNinjaComponent>(user);
        SetOutfitCommand.SetOutfit(user, "SpaceNinjaGear", EntityManager);
        GreetNinja(mind);
    }

	/// <summary>
	/// Update the alert for the ninja's suit power indicator.
	/// </summary>
    public void SetSuitPowerAlert(EntityUid uid, SpaceNinjaComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false) || comp.Deleted || comp.Suit == null)
        {
            _alerts.ClearAlert(uid, AlertType.SuitPower);
            return;
        }

        if (GetNinjaBattery(uid, out var battery))
        {
             var severity = ContentHelpers.RoundToLevels(MathF.Max(0f, battery.CurrentCharge), battery.MaxCharge, 7);
            _alerts.ShowAlert(uid, AlertType.SuitPower, (short) severity);
        }
        else
        {
            _alerts.ClearAlert(uid, AlertType.SuitPower);
        }
    }

    private void OnNinjaStartup(EntityUid uid, SpaceNinjaComponent comp, ComponentStartup args)
    {
        var config = RuleConfig();

        // inject starting implants
        var coords = Transform(uid).Coordinates;
        foreach (var id in config.Implants)
        {
            var implant = Spawn(id, coords);

            if (!TryComp<SubdermalImplantComponent>(implant, out var implantComp))
                return;

            _implants.ForceImplant(uid, implant, implantComp);
        }

        // choose spider charge detonation point
        // currently based on warp points, something better could be done
        var warps = new List<EntityUid>();
        foreach (var warp in EntityManager.EntityQuery<WarpPointComponent>(true))
        {
            if (warp.Location != null)
                warps.Add(warp.Owner);
        }

        if (warps.Count > 0)
            comp.SpiderChargeTarget = _random.Pick(warps);
    }

    private void OnNinjaMindAdded(EntityUid uid, SpaceNinjaComponent comp, MindAddedMessage args)
    {
        if (TryComp<MindComponent>(uid, out var mind) && mind.Mind != null)
            GreetNinja(mind.Mind);
    }

    private void GreetNinja(Mind.Mind mind)
    {
        if (!mind.TryGetSession(out var session))
            return;

        var config = RuleConfig();
        var role = new TraitorRole(mind, _proto.Index<AntagPrototype>("SpaceNinja"));
        mind.AddRole(role);
        _traitorRule.Traitors.Add(role);
        foreach (var objective in config.Objectives)
            AddObjective(mind, objective);

        _chatMan.DispatchServerMessage(session, Loc.GetString("ninja-role-greeting"));
        _audio.PlayGlobal(config.GreetingSound, Filter.Empty().AddPlayer(session), false, AudioParams.Default);
    }

	/// <summary>
	/// Returns the space ninja spawn gamerule's config
	/// </summary>
    public NinjaRuleConfiguration RuleConfig()
    {
        return (NinjaRuleConfiguration) _proto.Index<GameRulePrototype>("SpaceNinjaSpawn").Configuration;
    }

    private void OnNinjaAttacked(EntityUid uid, SpaceNinjaComponent comp, AttackedEvent args)
    {
        if (comp.Suit != null && TryComp<SpaceNinjaSuitComponent>(comp.Suit, out var suit))
        {
        	_suit.RevealNinja(suit, uid);
        	// TODO: disable abilities for 5 seconds
        }
    }

    private void UpdateNinja(EntityUid uid, SpaceNinjaComponent ninja, float frameTime)
    {
        if (ninja.Suit == null || !TryComp<SpaceNinjaSuitComponent>(ninja.Suit.Value, out var suit))
            return;

        float wattage = SuitWattage(suit);

        SetSuitPowerAlert(uid, ninja);
        if (!GetNinjaBattery(uid, out var battery) || !battery.TryUseCharge(wattage * frameTime))
        {
            // ran out of power, reveal ninja
            _suit.RevealNinja(suit, uid);
        }
    }

    private bool GetNinjaBattery(EntityUid user, [NotNullWhen(true)] out BatteryComponent? battery)
    {
        if (TryComp<SpaceNinjaComponent>(user, out var ninja)
            && ninja.Suit != null
            && _powerCell.TryGetBatteryFromSlot(ninja.Suit.Value, out battery))
        {
            return true;
        }

        battery = null;
        return false;
    }

    private float SuitWattage(SpaceNinjaSuitComponent suit)
    {
        float wattage = suit.PassiveWattage;
        if (suit.Cloaked)
            wattage += suit.CloakWattage;
        return wattage;
    }

    private void AddObjective(Mind.Mind mind, string name)
    {
        if (_proto.TryIndex<ObjectivePrototype>(name, out var objective))
            mind.TryAddObjective(objective);
        else
            Logger.Error($"Ninja has unknown objective prototype: {name}");
    }
}
