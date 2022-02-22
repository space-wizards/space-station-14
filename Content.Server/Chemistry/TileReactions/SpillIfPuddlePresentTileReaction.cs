using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.TileReactions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class SpillIfPuddlePresentTileReaction : ITileReaction
    {
        public FixedPoint2 TileReact(TileRef tile, ReagentPrototype reagent, FixedPoint2 reactVolume)
        {
            var spillSystem = EntitySystem.Get<SpillableSystem>();
            if (reactVolume < 5 || !spillSystem.TryGetPuddle(tile, out _)) return FixedPoint2.Zero;

            return spillSystem.SpillAt(tile,new Solution(reagent.ID, reactVolume), "PuddleSmear", true, false, true) != null
                ? reactVolume
                : FixedPoint2.Zero;
        }
    }
}
