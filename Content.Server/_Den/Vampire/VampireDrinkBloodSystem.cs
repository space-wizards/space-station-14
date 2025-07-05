using Content.Server._Den.Components;
using Content.Shared._Den.Vampire.Events;
using Content.Server.Body.Components;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Popups;

namespace Content.Server._Den.Vampire;

/// <summary>
/// </summary>
public sealed class VampireDrinkBloodSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly VampireBloodEssenceSystem _vampireBloodEssenceSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireDrinkBloodComponent, VampireDrinkBloodAbility>(OnFeedStart);
        SubscribeLocalEvent<VampireDrinkBloodComponent, VampireDrinkBloodAbilityDoAfter>(OnFeedEnd);
        SubscribeLocalEvent<VampireDrinkBloodComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<VampireDrinkBloodComponent> entity, ref MapInitEvent args)
    {
        Logger.Info($"OnMapInit: Adding vampire drink blood action to entity {ToPrettyString(entity)}");
       _action.AddAction(entity, ref entity.Comp.Action, entity.Comp.ActionProto, entity);
    }

    private void OnFeedStart(Entity<VampireDrinkBloodComponent> ent, ref VampireDrinkBloodAbility args)
    {
        if (args.Handled)
        {
            Logger.Info($"OnFeedStart: Already handled for {ToPrettyString(ent)}");
            return;
        }

        args.Handled = true;

        if (args.Target == args.Performer)
        {
            Logger.Info($"OnFeedStart: Performer tried to target self ({ToPrettyString(args.Performer)})");
            return;
        }

        var target = args.Target;

        Logger.Info($"OnFeedStart: Target {ToPrettyString(target)}");

        if (!HasComp<BloodstreamComponent>(target))
        {
            Logger.Info($"OnFeedStart: Target {ToPrettyString(target)} has no BloodstreamComponent");
            return;
        }

        var drinkBloodDoAfter = new DoAfterArgs(EntityManager, ent, ent.Comp.DrinkBloodDuration, new VampireDrinkBloodAbilityDoAfter(),ent, target: target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnWeightlessMove = false,
        };

        if (!_doAfter.TryStartDoAfter(drinkBloodDoAfter))
        {
            Logger.Info($"OnFeedStart: Failed to start DoAfter for {ToPrettyString(ent)} -> {ToPrettyString(target)}");
            return;
        }

        _popup.PopupEntity(Loc.GetString("vampire-feeding-on-vampire", ("target", target)), ent, ent, PopupType.Medium);
        _popup.PopupEntity(Loc.GetString("vampire-feeding-on-target", ("vampire", ent)), ent, target, PopupType.LargeCaution);
        Logger.Info($"OnFeedStart: Started DoAfter for {ToPrettyString(ent)} -> {ToPrettyString(target)}");
    }

    private void OnFeedEnd(Entity<VampireDrinkBloodComponent> ent, ref VampireDrinkBloodAbilityDoAfter args)
    {

        Logger.Info($"OnFeedEndStart: Drinking blood to {ToPrettyString(ent)}");

        if (args.Handled || args.Cancelled)
        {
            Logger.Info($"OnFeedEnd: Skipped because {(args.Handled ? "Handled" : "Cancelled")} for {ToPrettyString(ent)}");
            return;

        }

        args.Handled = true;

        var target = args.Args.Target;
        Logger.Info($"OnFeedEnd: target {ToPrettyString(target)}");

        if (target is null || !TryComp<BloodstreamComponent>(target, out var targedBloodstream))
        {
            Logger.Info($"OnFeedEnd: Invalid or missing target for {ToPrettyString(ent)}");
            return;
        }

        Logger.Info($"OnFeedEnd: Drinking blood from {ToPrettyString(target)} to {ToPrettyString(ent)}");
        _vampireBloodEssenceSystem.DrinkBlood(ent, (target.Value, targedBloodstream));

        _popup.PopupEntity(Loc.GetString("vampire-feeding-successful-vampire", ("target", target)), ent, ent, PopupType.Medium);
        _popup.PopupEntity(Loc.GetString("vampire-feeding-successful-target", ("vampire", ent.Owner)), ent.Owner, target.Value, PopupType.MediumCaution);
    }
}
