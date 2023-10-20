using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Kitchen.Components;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Kitchen;

public abstract class SharedKitchenSpikeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<KitchenSpikeComponent, CanDropTargetEvent>(OnCanDrop);
    }

    private void OnCanDrop(EntityUid uid, KitchenSpikeComponent component, ref CanDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!HasComp<ButcherableComponent>(args.Dragged))
        {
            args.CanDrop = false;
            return;
        }

        // TODO: Once we get silicons need to check organic
        args.CanDrop = true;
    }
}

[Serializable, NetSerializable]
public sealed partial class SpikeDoAfterEvent : SimpleDoAfterEvent
{
}
