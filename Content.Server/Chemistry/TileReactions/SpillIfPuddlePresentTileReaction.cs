using Content.Server.Fluids.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Solution;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.TileReactions
{
    [UsedImplicitly]
    [DataDefinition]
    public class SpillIfPuddlePresentTileReaction : ITileReaction
    {
        public ReagentUnit TileReact(TileRef tile, ReagentPrototype reagent, ReagentUnit reactVolume)
        {
            if (reactVolume < 5 || !tile.TryGetPuddle(null, out _)) return ReagentUnit.Zero;

            return tile.SpillAt(new Solution(reagent.ID, reactVolume), "PuddleSmear", true, false) != null ? reactVolume : ReagentUnit.Zero;
        }
    }
}
