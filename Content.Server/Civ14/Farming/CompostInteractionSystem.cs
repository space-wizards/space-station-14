using Content.Shared.Interaction;
using Content.Shared.DoAfter;
using Content.Server.DoAfter;
using Content.Server.Botany.Components;
using Content.Shared.Farming;
using Content.Shared.Popups;

namespace Content.Server.Farming;

public sealed partial class CompostInteractionSystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CompostableFieldComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<CompostableFieldComponent, CompostDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteractUsing(Entity<CompostableFieldComponent> field, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        var usedItem = args.Used;
        if (!HasComp<CompostComponent>(usedItem))
            return;

        var comp = field.Comp;
        var user = args.User;

        var netUsedItem = GetNetEntity(usedItem);
        var doAfterArgs = new DoAfterArgs(EntityManager, user, comp.CompostTime,
            new CompostDoAfterEvent(netUsedItem), field, usedItem)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true
        };

        if (_doAfter.TryStartDoAfter(doAfterArgs))
        {
            _popup.PopupEntity("You begin applying compost to the field.", field, user);
            args.Handled = true;
        }
    }
    private void OnDoAfter(Entity<CompostableFieldComponent> field, ref CompostDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!TryComp<PlantHolderComponent>(field, out var plantHolder))
            return;

        var usedEntity = GetEntity(args.Used);
        if (TryComp<CompostComponent>(usedEntity, out var compostComp))
        {
            plantHolder.NutritionLevel += compostComp.NutritionValue;
        }

        QueueDel(usedEntity);

        _popup.PopupEntity("You finish applying compost to the field.", field, args.User);
        args.Handled = true;
    }
}