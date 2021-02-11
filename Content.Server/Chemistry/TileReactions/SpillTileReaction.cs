using Content.Server.GameObjects.Components.Fluids;
using Content.Server.GameObjects.Components.Movement;
using Content.Shared.Chemistry;
using Content.Shared.Interfaces.Chemistry;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Server.Chemistry.TileReactions
{
    [UsedImplicitly]
    public class SpillTileReaction : ITileReaction
    {
        private float _launchForwardsMultiplier = 1f;
        private float _requiredSlipSpeed = 6f;
        private float _paralyzeTime = 1f;
        private bool _overflow;

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            // If you want to modify more puddle/slippery values, add them here.
            serializer.DataField(ref _paralyzeTime, "paralyzeTime", 1f);
            serializer.DataField(ref _launchForwardsMultiplier, "launchForwardsMultiplier", 1f);
            serializer.DataField(ref _requiredSlipSpeed, "requiredSlipSpeed", 6f);
            serializer.DataField(ref _overflow, "overflow", false);
        }

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
