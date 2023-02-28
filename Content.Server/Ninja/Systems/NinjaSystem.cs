using Content.Server.Administration.Commands;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Configurations;
using Content.Server.Mind.Components;
using Content.Server.Objectives;
using Content.Server.Power.Components;
using Content.Server.PowerCell;
using Content.Server.Traitor;
using Content.Server.Warps;
using Content.Shared.Alert;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;
using Content.Shared.Roles;
using Content.Shared.PowerCell.Components;
using Content.Shared.Rounding;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Ninja.Systems;

public sealed class NinjaSystem : SharedNinjaSystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly SharedSubdermalImplantSystem _implants = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly NinjaSuitSystem _suit = default!;
    [Dependency] private readonly TraitorRuleSystem _traitorRule = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaComponent, ComponentStartup>(OnNinjaStartup);
        SubscribeLocalEvent<NinjaComponent, MindAddedMessage>(OnNinjaMindAdded);
        SubscribeLocalEvent<NinjaComponent, AttackedEvent>(OnNinjaAttacked);
    }

    public override void Update(float frameTime)
    {
        foreach (var ninja in EntityQuery<NinjaComponent>())
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
        if (HasComp<NinjaComponent>(user))
            return;

        AddComp<NinjaComponent>(user);
        SetOutfitCommand.SetOutfit(user, "SpaceNinjaGear", EntityManager);
        GreetNinja(mind);
    }

    /// <summary>
    /// Update the alert for the ninja's suit power indicator.
    /// </summary>
    public void SetSuitPowerAlert(EntityUid uid, NinjaComponent? comp = null)
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

    private void OnNinjaStartup(EntityUid uid, NinjaComponent comp, ComponentStartup args)
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

    private void OnNinjaMindAdded(EntityUid uid, NinjaComponent comp, MindAddedMessage args)
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

    private void OnNinjaAttacked(EntityUid uid, NinjaComponent comp, AttackedEvent args)
    {
        if (comp.Suit != null && TryComp<NinjaSuitComponent>(comp.Suit, out var suit))
        {
            _suit.RevealNinja(suit, uid);
            // TODO: disable abilities for 5 seconds
        }
    }

    private void UpdateNinja(EntityUid uid, NinjaComponent ninja, float frameTime)
    {
        if (ninja.Suit == null || !TryComp<NinjaSuitComponent>(ninja.Suit.Value, out var suit))
            return;

        float wattage = _suit.SuitWattage(suit);

        SetSuitPowerAlert(uid, ninja);
        if (!GetNinjaBattery(uid, out var battery) || !battery.TryUseCharge(wattage * frameTime))
        {
            // ran out of power, reveal ninja
            _suit.RevealNinja(suit, uid);
        }
    }

    /// <summary>
    /// Get the battery component in a ninja's suit, if it's worn.
    /// </summary>
    public bool GetNinjaBattery(EntityUid user, [NotNullWhen(true)] out BatteryComponent? battery)
    {
        if (TryComp<NinjaComponent>(user, out var ninja)
            && ninja.Suit != null
            && _powerCell.TryGetBatteryFromSlot(ninja.Suit.Value, out battery))
        {
            return true;
        }

        battery = null;
        return false;
    }

    private void AddObjective(Mind.Mind mind, string name)
    {
        if (_proto.TryIndex<ObjectivePrototype>(name, out var objective))
            mind.TryAddObjective(objective);
        else
            Logger.Error($"Ninja has unknown objective prototype: {name}");
    }
}
