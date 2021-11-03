using Content.Server.Fluids.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
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

        public FixedPoint2 TileReact(TileRef tile, ReagentPrototype reagent, FixedPoint2 reactVolume)
        {
            if (reactVolume < 5) return FixedPoint2.Zero;

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

            return FixedPoint2.Zero;
        }
    }
}
