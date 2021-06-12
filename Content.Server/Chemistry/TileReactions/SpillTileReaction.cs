using Content.Server.Fluids.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Solution;
using Content.Shared.Slippery;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.TileReactions
{
    [UsedImplicitly]
    [DataDefinition]
    public class SpillTileReaction : ITileReaction
    {
        [DataField("launchForwardsMultiplier")] private float _launchForwardsMultiplier = 1;
        [DataField("requiredSlipSpeed")] private float _requiredSlipSpeed = 6;
        [DataField("paralyzeTime")] private float _paralyzeTime = 1;
        [DataField("overflow")] private bool _overflow;

        public ReagentUnit TileReact(TileRef tile, ReagentPrototype reagent, ReagentUnit reactVolume)
        {
            if (reactVolume < 5) return ReagentUnit.Zero;

            // TODO Make this not puddle smear.
            var puddle = tile.SpillAt(new Solution(reagent.ID, reactVolume), "PuddleSmear", _overflow, false);

            if (puddle != null)
            {
                var slippery = puddle.Owner.GetComponent<SlipperyComponent>();
                slippery.LaunchForwardsMultiplier = _launchForwardsMultiplier;
                slippery.RequiredSlipSpeed = _requiredSlipSpeed;
                slippery.ParalyzeTime = _paralyzeTime;

                return reactVolume;
            }

            return ReagentUnit.Zero;
        }
    }
}
