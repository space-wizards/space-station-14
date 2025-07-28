using Content.Shared.Clothing.Components;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Shared.Clothing.EntitySystems;

public sealed class ExaminableClothingSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExaminableClothingComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ExaminableClothingComponent, InventoryRelayedEvent<ExaminedEvent>>(OnExaminedWorn);
    }

    private string ExamineText(Entity<ExaminableClothingComponent> ent, EntityUid wearer)
    {
        if (ent.Comp.ExamineText is { } examineText)
        {
            return Loc.GetString("examinable-clothing-examine", ("wearer", wearer), ("item", Loc.GetString(examineText, ("wearer", wearer))));
        }
        else
        {
            return Loc.GetString("examinable-clothing-examine", ("wearer", wearer), ("item", FormattedMessage.EscapeText(Identity.Name(ent, EntityManager))));
        }
    }

    private void OnExamined(Entity<ExaminableClothingComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("examinable-clothing-when-worn", ("message", ExamineText(ent, args.Examiner))));
    }

    private void OnExaminedWorn(Entity<ExaminableClothingComponent> ent, ref InventoryRelayedEvent<ExaminedEvent> args)
    {
        if (!_inventory.TryGetContainingSlot(ent.Owner, out var slot) || (slot.SlotFlags & ent.Comp.AllowedSlots) == SlotFlags.NONE)
            return;

        if (!_container.TryGetContainingContainer((ent.Owner, null, null), out var container))
            return;

        args.Args.PushMarkup(ExamineText(ent, container.Owner));
    }
}
