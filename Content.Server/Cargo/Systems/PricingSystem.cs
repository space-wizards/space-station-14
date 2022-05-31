using System.Linq;
using Content.Server.Administration;
using Content.Server.Body.Components;
using Content.Server.Cargo.Components;
using Content.Server.Inventory;
using Content.Server.Materials;
using Content.Server.Stack;
using Content.Server.Storage.Components;
using Content.Shared.Administration;
using Content.Shared.Inventory;
using Content.Shared.MobState.Components;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Server.Cargo.Systems;

/// <summary>
/// This handles calculating the price of items, and implements two basic methods of pricing materials.
/// </summary>
public sealed class PricingSystem : EntitySystem
{
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;


    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<StaticPriceComponent, PriceCalculationEvent>(CalculateStaticPrice);
        SubscribeLocalEvent<StackPriceComponent, PriceCalculationEvent>(CalculateStackPrice);
        SubscribeLocalEvent<MaterialPriceComponent, PriceCalculationEvent>(CalculateMaterialPrice);
        SubscribeLocalEvent<ContentsPriceComponent, PriceCalculationEvent>(CalculateContainerPrice);
        SubscribeLocalEvent<MobPriceComponent, PriceCalculationEvent>(CalculateMobPrice);

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
            if (!int.TryParse(gid, out var i) || i <= 0)
            {
                shell.WriteError($"Invalid grid ID \"{gid}\".");
                continue;
            }

            var gridId = new GridId(i);

            if (!_mapManager.TryGetGrid(gridId, out var mapGrid))
            {
                shell.WriteError($"Grid \"{i}\" doesn't exist.");
                continue;
            }

            shell.WriteLine($"Grid {gid} appraised to {AppraiseGrid(mapGrid.GridEntityId)} credits.");
        }
    }

    private void CalculateMobPrice(EntityUid uid, MobPriceComponent component, ref PriceCalculationEvent args)
    {
        if (!TryComp<BodyComponent>(uid, out var body) || !TryComp<MobStateComponent>(uid, out var state))
            throw new Exception("Tried to get the mob price of an object that isn't a mob with a body and state!");

        var partList = body.Slots.ToList();
        var totalPartsPresent = partList.Sum(x => x.Part != null ? 1 : 0);
        var totalParts = partList.Count;

        var partRatio = totalPartsPresent / (double) totalParts;
        var partPenalty = component.Price * (1 - partRatio) * component.MissingBodyPartPenalty;

        args.Price += (component.Price - partPenalty) * (state.IsAlive() ? 1.0 : component.DeathPenalty);
    }

    private void CalculateMaterialPrice(EntityUid uid, MaterialPriceComponent component, ref PriceCalculationEvent args)
    {
        if (!TryComp<MaterialComponent>(uid, out var material))
            throw new Exception("Tried to get the stack price of an object that isn't a stack w/ material!");

        if (TryComp<StackComponent>(uid, out var stack))
            args.Price += stack.Count * material.Materials.Sum(x => x.Price * material._materials[x.ID]);
        else
            args.Price += material.Materials.Sum(x => x.Price);
    }

    private void CalculateContainerPrice(EntityUid uid, ContentsPriceComponent component, ref PriceCalculationEvent args)
    {
        if (TryComp<EntityStorageComponent>(uid, out var entStorage))
        {
            foreach (var contained in entStorage.Contents.ContainedEntities)
            {
                args.Price += GetPrice(contained);
            }
        }

        if (TryComp<ServerStorageComponent>(uid, out var storage) && storage.Storage is not null)
        {
            foreach (var contained in storage.Storage.ContainedEntities)
            {
                args.Price += GetPrice(contained);
            }
        }

        if (TryComp<ServerInventoryComponent>(uid, out var inventory))
        {
            foreach (var slot in _inventorySystem.GetSlots(uid, inventory))
            {
                if (!_inventorySystem.TryGetSlotEntity(uid, slot.Name, out var obj, inventory))
                    continue;

                args.Price += GetPrice(obj.Value);
            }
        }
    }

    private void CalculateStackPrice(EntityUid uid, StackPriceComponent component, ref PriceCalculationEvent args)
    {
        if (!TryComp<StackComponent>(uid, out var stack))
            throw new Exception("Tried to get the stack price of an object that isn't a stack!");

        args.Price += stack.Count;
    }

    private void CalculateStaticPrice(EntityUid uid, StaticPriceComponent component, ref PriceCalculationEvent args)
    {
        args.Price += component.Price;
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
        return ev.Price;
    }

    /// <summary>
    /// Appraises a grid, this is mainly meant to be used by yarrs.
    /// </summary>
    /// <param name="grid">The grid to appraise.</param>
    /// <param name="predicate">An optional predicate that controls whether or not the entity is counted toward the total.</param>
    /// <returns>The total value of the grid.</returns>
    public double AppraiseGrid(EntityUid grid, Func<EntityUid, bool>? predicate = null)
    {
        var xform = Transform(grid);
        var price = 0.0;

        foreach (var childXform in xform.Children)
        {
            if (predicate is null || predicate(childXform.Owner))
                price += GetPrice(childXform.Owner);
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
