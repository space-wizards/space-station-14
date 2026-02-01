using Content.Shared.Lathe;
using Content.Shared.Stacks;
using Content.Shared.Materials;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;

namespace Content.Shared.TicketPrinter;

public abstract class SharedTicketPrinterSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TicketPrinterComponent, LatheFinishPrintingEvent>(OnPrint);
        SubscribeLocalEvent<TicketPrinterComponent, ReclaimFinishedEvent>(OnReclaimed);
    }

    /// <summary>
    /// Print tickets when lathe prints entities with a ticket value
    /// </summary>
    /// <param name="ent">The lathe</param>
    /// <param name="args">event containing recipe</param>
    private void OnPrint(Entity<TicketPrinterComponent> ent, ref LatheFinishPrintingEvent args)
    {
        if (args.Recipe.Result is not { } resultProto) //is the result empty, if not set as resultProto
            return;

        var entProto = _proto.Index(resultProto);
        if (!entProto.TryGetComponent<TicketValueComponent>(out var ticketComp, EntityManager.ComponentFactory)) //does component exist from EntityProtoId of recipe result
            return;

        if (entProto.TryGetComponent<StackComponent>(out var stackComp, EntityManager.ComponentFactory))//if a stack, produce tickets for each item in the stack
            PrintTickets(ent, ticketComp.TicketValue * stackComp.Count);
        else
            PrintTickets(ent, ticketComp.TicketValue);
    }

    /// <summary>
    /// print tickets when reclaimer reclaims entities with a ticket value
    /// </summary>
    /// <param name="ent">the reclaimer</param>
    /// <param name="args">reclaim event containing reclaimed item</param>
    private void OnReclaimed(Entity<TicketPrinterComponent> ent, ref ReclaimFinishedEvent args)
    {
        if (_whitelistSystem.IsWhitelistFail(ent.Comp.Whitelist, args.Item)) //plenty of things are reclaimable but only some salvagable, should only give tickets for salvage scrap.
            return;

        if (!TryComp<PhysicalCompositionComponent>(args.Item, out var physComp))
            return;

        foreach (var (material, amount) in physComp.MaterialComposition) //for each material making up the reclaimed item's physical composition
        {
            if (amount <= 0 || !_proto.TryIndex<MaterialPrototype>(material, out var materialProto) || materialProto.StackEntity == null) //get that material's Material Prototype
                continue;
            var entProto = _proto.Index<EntityPrototype>(materialProto.StackEntity); //use that to get its entity prototype

            if (!entProto.TryGetComponent<TicketValueComponent>(out var ticketComp, EntityManager.ComponentFactory) ||
                !entProto.TryGetComponent<PhysicalCompositionComponent>(out var matphysComp, EntityManager.ComponentFactory)) //use that to get TicketValue and PhysicalComposition Components
                continue; //theoretically an entity may have some materials that do and some materials that don't have ticket values so we have to check them all.

            PrintTickets(ent, ticketComp.TicketValue * amount / matphysComp.MaterialComposition[materialProto.ID]);
        }
    }

    /// <summary>
    /// spawn appropriate amount of tickets
    /// considerations of stack amounts should occur before this point
    /// Ticketprinter modifer applied in this function
    /// </summary>
    /// <param name="ent">the entity printing the tickets</param>
    /// <param name="amount">amount of tickets </param>
    protected virtual void PrintTickets(Entity<TicketPrinterComponent> ent, float amount)
    {
    }
}
