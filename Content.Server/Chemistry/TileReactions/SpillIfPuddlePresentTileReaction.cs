using Content.Server.Fluids;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;
using PuddleSystem = Content.Server.Fluids.EntitySystems.PuddleSystem;

namespace Content.Server.Chemistry.TileReactions
{
    [UsedImplicitly]
    [DataDefinition]
    public class SpillIfPuddlePresentTileReaction : ITileReaction
    {
        public ReagentUnit TileReact(TileRef tile, ReagentPrototype reagent, ReagentUnit reactVolume)
        {
            var puddleSystem = EntitySystem.Get<PuddleSystem>();

            if (reactVolume < 5 || !puddleSystem.TryGetPuddle(tile, null, out _))
                return ReagentUnit.Zero;

            return puddleSystem.SpillAt(tile, new Solution(reagent.ID, reactVolume), "PuddleSmear", true, false) != null
                ? reactVolume
                : ReagentUnit.Zero;
        }
    }
}
