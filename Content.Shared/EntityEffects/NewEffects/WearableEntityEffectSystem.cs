using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.NewEffects;

public sealed partial class CreateWearableEntityEffectSystem : EntityEffectSystem<InventoryComponent, CreateWearable>
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    protected override void Effect(Entity<InventoryComponent> entity, ref EntityEffectEvent<CreateWearable> args)
    {
        _inventory.SpawnItemInSlot(entity, args.Effect.Slot, args.Effect.PrototypeId);

        // TODO: Reactive needs to handle deleting reagents for this.
    }
}

public sealed class CreateWearable : EntityEffectBase<CreateWearable>
{
    /// <summary>
    /// Name of the slot we're spawning the item into.
    /// </summary>
    [DataField(required: true)]
    public string Slot = string.Empty; // Rider is drunk and keeps yelling at me to fill this out or make required: true but, it is required true so it's just being an asshole.

    /// <summary>
    /// Prototype ID of item to spawn.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId PrototypeId;
}
