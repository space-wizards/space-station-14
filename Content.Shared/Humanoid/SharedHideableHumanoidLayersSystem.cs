using System.Numerics;
using Content.Shared.Inventory;
using Robust.Shared.Utility;

namespace Content.Shared.Humanoid;

public abstract partial class SharedHideableHumanoidLayersSystem : EntitySystem
{
    /// <summary>
    ///     Toggles a humanoid's sprite layer visibility.
    /// </summary>
    /// <param name="ent">Humanoid entity</param>
    /// <param name="layer">Layer to toggle visibility for</param>
    /// <param name="hidden">Whether to hide (true) or show (false) the layer. If more than once piece of clothing is hiding the layer, it may remain hidden.</param>
    /// <param name="slot">Equipment slot that has the clothing that is (or was) hiding the layer.</param>
    public virtual void SetLayerOcclusion(
        Entity<HideableHumanoidLayersComponent?> ent,
        HumanoidVisualLayers layer,
        bool hidden,
        SlotFlags slot)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

#if DEBUG
        DebugTools.AssertNotEqual(slot, SlotFlags.NONE);
        // Check that only a single bit in the bitflag is set
        var powerOfTwo = BitOperations.RoundUpToPowerOf2((uint)slot);
        DebugTools.AssertEqual((uint)slot, powerOfTwo);
#endif

        var dirty = false;
        if (hidden)
        {
            var oldSlots = ent.Comp.HiddenLayers.GetValueOrDefault(layer);
            ent.Comp.HiddenLayers[layer] = slot | oldSlots;
            dirty |= (oldSlots & slot) != slot;
        }
        else if (ent.Comp.HiddenLayers.TryGetValue(layer, out var oldSlots))
        {
            // This layer might be getting hidden by more than one piece of equipped clothing.
            // remove slot flag from the set of slots hiding this layer, then check if there are any left.
            ent.Comp.HiddenLayers[layer] = ~slot & oldSlots;
            if (ent.Comp.HiddenLayers[layer] == SlotFlags.NONE)
                ent.Comp.HiddenLayers.Remove(layer);

            dirty |= (oldSlots & slot) != 0;
        }

        if (!dirty)
            return;

        Dirty(ent);

        var evt = new HumanoidLayerVisibilityChangedEvent(layer, ent.Comp.HiddenLayers.ContainsKey(layer));
        RaiseLocalEvent(ent, ref evt);
    }
}
