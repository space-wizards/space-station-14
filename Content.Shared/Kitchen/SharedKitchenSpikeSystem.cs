using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Kitchen.Components;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Kitchen;

/// <summary>
///
/// </summary>
public abstract class SharedKitchenSpikeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KitchenSpikeComponent, CanDropTargetEvent>(OnCanDrop);
    }

    private void OnCanDrop(Entity<KitchenSpikeComponent> ent, ref CanDropTargetEvent args)
    {

    }
}

/// <summary>
///
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SpikeDoAfterEvent : SimpleDoAfterEvent;
