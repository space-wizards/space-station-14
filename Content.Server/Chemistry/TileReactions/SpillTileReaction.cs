using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Slippery;
using Content.Shared.StepTrigger;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Server.Chemistry.TileReactions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class SpillTileReaction : ITileReaction
    {
        [DataField("launchForwardsMultiplier")] private float _launchForwardsMultiplier = 1;
        [DataField("requiredSlipSpeed")] private float _requiredSlipSpeed = 6;
        [DataField("paralyzeTime")] private float _paralyzeTime = 1;
        [DataField("overflow")] private bool _overflow;

        public FixedPoint2 TileReact(TileRef tile, ReagentPrototype reagent, FixedPoint2 reactVolume)
        {
            if (reactVolume < 5) return FixedPoint2.Zero;

            // TODO Make this not puddle smear.
            var puddle = EntitySystem.Get<SpillableSystem>()
                .SpillAt(tile, new Solution(reagent.ID, reactVolume), "PuddleSmear", _overflow, false, true);

            if (puddle != null)
            {
                var entityManager = IoCManager.Resolve<IEntityManager>();
                var slippery = entityManager.GetComponent<SlipperyComponent>(puddle.Owner);
                slippery.LaunchForwardsMultiplier = _launchForwardsMultiplier;
                EntitySystem.Get<StepTriggerSystem>().SetRequiredTriggerSpeed(puddle.Owner, _requiredSlipSpeed);
                slippery.ParalyzeTime = _paralyzeTime;

                return reactVolume;
            }

            return FixedPoint2.Zero;
        }
    }
}
