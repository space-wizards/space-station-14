using System.Linq;
using Content.Server.Cargo.Components;
using Content.Server.Materials;
using Content.Server.Stack;
using Content.Server.Storage.Components;

namespace Content.Server.Cargo.Systems;

/// <summary>
/// This handles calculating the price of items, and implements two basic methods of pricing materials.
/// </summary>
public sealed class PricingSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<StaticPriceComponent, PriceCalculationEvent>(CalculateStaticPrice);
        SubscribeLocalEvent<StackPriceComponent, PriceCalculationEvent>(CalculateStackPrice);
        SubscribeLocalEvent<MaterialPriceComponent, PriceCalculationEvent>(CalculateMaterialPrice);
        SubscribeLocalEvent<ContentsPriceComponent, PriceCalculationEvent>(CalculateContainerPrice);
    }

    private void CalculateMaterialPrice(EntityUid uid, MaterialPriceComponent component, PriceCalculationEvent args)
    {
        if (!TryComp<MaterialComponent>(uid, out var material))
            throw new Exception("Tried to get the stack price of an object that isn't a stack w/ material!");

        if (TryComp<StackComponent>(uid, out var stack))
            args.Price += stack.Count * material.Materials.Sum(x => x.Price);
        else
            args.Price += material.Materials.Sum(x => x.Price);
    }

    private void CalculateContainerPrice(EntityUid uid, ContentsPriceComponent component, PriceCalculationEvent args)
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
    }

    private void CalculateStackPrice(EntityUid uid, StackPriceComponent component, PriceCalculationEvent args)
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
}

/// <summary>
/// A directed by-ref event fired on an entity when something needs to know it's price. This value is not cached.
/// </summary>
public struct PriceCalculationEvent
{
    /// <summary>
    /// The total price of the entity.
    /// </summary>
    public double Price = 0;

    public PriceCalculationEvent() { }
}
