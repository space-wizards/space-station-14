using System.Linq;
using Content.Server.Administration;
using Content.Server.Body.Systems;
using Content.Server.Cargo.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.MobState;
using Content.Server.Stack;
using Content.Shared.Administration;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Materials;
using Content.Shared.MobState.Components;
using Content.Shared.Stacks;
using Robust.Shared.Console;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Cargo.Systems;

/// <summary>
/// This handles calculating the price of items, and implements two basic methods of pricing materials.
/// </summary>
public sealed class PricingSystem : EntitySystem
{
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    [Dependency] private readonly BodySystem _bodySystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<StaticPriceComponent, PriceCalculationEvent>(CalculateStaticPrice);
        SubscribeLocalEvent<StackPriceComponent, PriceCalculationEvent>(CalculateStackPrice);
        SubscribeLocalEvent<MobPriceComponent, PriceCalculationEvent>(CalculateMobPrice);
        SubscribeLocalEvent<SolutionContainerManagerComponent, PriceCalculationEvent>(CalculateSolutionPrice);

        _consoleHost.RegisterCommand("appraisegrid",
            "Calculates the total value of the given grids.",
            "appraisegrid <grid Ids>", AppraiseGridCommand);
    }

    [AdminCommand(AdminFlags.Debug)]
    private void AppraiseGridCommand(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError("Not enough arguments.");
            return;
        }

        foreach (var gid in args)
        {
            if (!EntityUid.TryParse(gid, out var gridId) || !gridId.IsValid())
            {
                shell.WriteError($"Invalid grid ID \"{gid}\".");
                continue;
            }

            if (!_mapManager.TryGetGrid(gridId, out var mapGrid))
            {
                shell.WriteError($"Grid \"{gridId}\" doesn't exist.");
                continue;
            }

            List<(double, EntityUid)> mostValuable = new();

            var value = AppraiseGrid(mapGrid.Owner, null, (uid, price) =>
            {
                mostValuable.Add((price, uid));
                mostValuable.Sort((i1, i2) => i2.Item1.CompareTo(i1.Item1));
                if (mostValuable.Count > 5)
                    mostValuable.Pop();
            });

            shell.WriteLine($"Grid {gid} appraised to {value} spacebucks.");
            shell.WriteLine($"The top most valuable items were:");
            foreach (var (price, ent) in mostValuable)
            {
                shell.WriteLine($"- {ToPrettyString(ent)} @ {price} spacebucks");
            }
        }
    }

    private void CalculateMobPrice(EntityUid uid, MobPriceComponent component, ref PriceCalculationEvent args)
    {
        if (!TryComp<BodyComponent>(uid, out var body) || !TryComp<MobStateComponent>(uid, out var state))
        {
            Logger.ErrorS("pricing", $"Tried to get the mob price of {ToPrettyString(uid)}, which has no {nameof(BodyComponent)} and no {nameof(MobStateComponent)}.");
            return;
        }

        var partList = _bodySystem.GetBodyAllSlots(uid, body).ToList();
        var totalPartsPresent = partList.Sum(x => x.Child != null ? 1 : 0);
        var totalParts = partList.Count;

        var partRatio = totalPartsPresent / (double) totalParts;
        var partPenalty = component.Price * (1 - partRatio) * component.MissingBodyPartPenalty;

        args.Price += (component.Price - partPenalty) * (_mobStateSystem.IsAlive(uid, state) ? 1.0 : component.DeathPenalty);
    }

    private void CalculateStackPrice(EntityUid uid, StackPriceComponent component, ref PriceCalculationEvent args)
    {
        if (!TryComp<StackComponent>(uid, out var stack))
        {
            Logger.ErrorS("pricing", $"Tried to get the stack price of {ToPrettyString(uid)}, which has no {nameof(StackComponent)}.");
            return;
        }

        args.Price += stack.Count * component.Price;
    }

    private void CalculateSolutionPrice(EntityUid uid, SolutionContainerManagerComponent component, ref PriceCalculationEvent args)
    {
        var price = 0f;

        foreach (var solution in component.Solutions.Values)
        {
            foreach (var reagent in solution.Contents)
            {
                if (!_prototypeManager.TryIndex<ReagentPrototype>(reagent.ReagentId, out var reagentProto))
                    continue;
                price += (float) reagent.Quantity * reagentProto.PricePerUnit;
            }
        }
        args.Price += price;
    }

    private void CalculateStaticPrice(EntityUid uid, StaticPriceComponent component, ref PriceCalculationEvent args)
    {
        args.Price += component.Price;
    }

    /// <summary>
    /// Get a rough price for an entityprototype. Does not consider contained entities.
    /// </summary>
    public double GetEstimatedPrice(EntityPrototype prototype, IComponentFactory? factory = null)
    {
        IoCManager.Resolve(ref factory);
        var price = 0.0;

        if (prototype.Components.TryGetValue(factory.GetComponentName(typeof(StaticPriceComponent)),
                out var staticPriceProto))
        {
            var staticComp = (StaticPriceComponent) staticPriceProto.Component;

            price += staticComp.Price;
        }

        if (prototype.Components.TryGetValue(factory.GetComponentName(typeof(StackPriceComponent)), out var stackpriceProto) &&
            prototype.Components.TryGetValue(factory.GetComponentName(typeof(StackComponent)), out var stackProto))
        {
            var stackPrice = (StackPriceComponent) stackpriceProto.Component;
            var stack = (StackComponent) stackProto.Component;
            price += stack.Count * stackPrice.Price;
        }

        return price;
    }

    public double GetMaterialPrice(MaterialComponent component)
    {
        double price = 0;
        foreach (var (id, quantity) in component.Materials)
        {
            price += _prototypeManager.Index<MaterialPrototype>(id).Price * quantity;
        }
        return price;
    }

    /// <summary>
    /// Appraises an entity, returning it's price.
    /// </summary>
    /// <param name="uid">The entity to appraise.</param>
    /// <returns>The price of the entity.</returns>
    /// <remarks>
    /// This fires off an event to calculate the price.
    /// Calculating the price of an entity that somehow contains itself will likely hang.
    /// </remarks>
    public double GetPrice(EntityUid uid)
    {
        var ev = new PriceCalculationEvent();
        RaiseLocalEvent(uid, ref ev);

        //TODO: Add an OpaqueToAppraisal component or similar for blocking the recursive descent into containers, or preventing material pricing.

        if (TryComp<MaterialComponent>(uid, out var material) && !HasComp<StackPriceComponent>(uid))
        {
            var matPrice = GetMaterialPrice(material);
            if (TryComp<StackComponent>(uid, out var stack))
                matPrice *= stack.Count;

            ev.Price += matPrice;
        }

        if (TryComp<ContainerManagerComponent>(uid, out var containers))
        {
            foreach (var container in containers.Containers)
            {
                foreach (var ent in container.Value.ContainedEntities)
                {
                    ev.Price += GetPrice(ent);
                }
            }
        }

        return ev.Price;
    }

    /// <summary>
    /// Appraises a grid, this is mainly meant to be used by yarrs.
    /// </summary>
    /// <param name="grid">The grid to appraise.</param>
    /// <param name="predicate">An optional predicate that controls whether or not the entity is counted toward the total.</param>
    /// <param name="afterPredicate">An optional predicate to run after the price has been calculated. Useful for high scores or similar.</param>
    /// <returns>The total value of the grid.</returns>
    public double AppraiseGrid(EntityUid grid, Func<EntityUid, bool>? predicate = null, Action<EntityUid, double>? afterPredicate = null)
    {
        var xform = Transform(grid);
        var price = 0.0;

        foreach (var child in xform.ChildEntities)
        {
            if (predicate is null || predicate(child))
            {
                var subPrice = GetPrice(child);
                price += subPrice;
                afterPredicate?.Invoke(child, subPrice);
            }
        }

        return price;
    }
}

/// <summary>
/// A directed by-ref event fired on an entity when something needs to know it's price. This value is not cached.
/// </summary>
[ByRefEvent]
public struct PriceCalculationEvent
{
    /// <summary>
    /// The total price of the entity.
    /// </summary>
    public double Price = 0;

    public PriceCalculationEvent() { }
}
