using Content.Shared.Inventory.Events;
using Robust.Client.Animus;

namespace Content.Client.Animus;

/// <summary>
/// A wrapper for Content events. This allows listening and triggering the AnimusSystem on events that are not known
/// by the engine itself.
/// </summary>
public sealed partial class AnimusEventWrapperSystem : EntitySystem
{
    [Dependency] private AnimusSystem _animusSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AnimusComponent, DidEquipEvent>(OnDidEquip);
    }

    private void OnDidEquip(Entity<AnimusComponent> ent, ref DidEquipEvent args)
    {
        _animusSystem.TriggerFor<DidEquipEvent>(ent);
    }
}
