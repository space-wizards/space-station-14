using Content.Server.Fluids;
using Content.Server.Fluids.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Slippery;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;
using PuddleSystem = Content.Server.Fluids.EntitySystems.PuddleSystem;

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

            var puddle = EntitySystem.Get<PuddleSystem>()
                .SpillAt(tile, new Solution(reagent.ID, reactVolume), "PuddleSmear", _overflow, false);

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
