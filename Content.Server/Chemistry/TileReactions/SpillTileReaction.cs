using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Slippery;
using Content.Shared.StepTrigger.Components;
using Content.Shared.StepTrigger.Systems;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Server.Chemistry.TileReactions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class SpillTileReaction : ITileReaction
    {
        [DataField("launchForwardsMultiplier")] public float LaunchForwardsMultiplier = 1;
        [DataField("requiredSlipSpeed")] public float RequiredSlipSpeed = 6;
        [DataField("paralyzeTime")] public float ParalyzeTime = 1;

        /// <summary>
        /// <see cref="SlipperyComponent.SuperSlippery"/>
        /// </summary>
        [DataField("superSlippery")] public bool SuperSlippery;

        public FixedPoint2 TileReact(TileRef tile,
            ReagentPrototype reagent,
            FixedPoint2 reactVolume,
            IEntityManager entityManager,
            List<ReagentData>? data)
        {
            if (reactVolume < 5)
                return FixedPoint2.Zero;

            if (entityManager.EntitySysManager.GetEntitySystem<PuddleSystem>()
                .TrySpillAt(tile, new Solution(reagent.ID, reactVolume, data), out var puddleUid, false, false))
            {
                var slippery = entityManager.EnsureComponent<SlipperyComponent>(puddleUid);
                slippery.LaunchForwardsMultiplier = LaunchForwardsMultiplier;
                slippery.ParalyzeTime = ParalyzeTime;
                slippery.SuperSlippery = SuperSlippery;
                entityManager.Dirty(puddleUid, slippery);

                var step = entityManager.EnsureComponent<StepTriggerComponent>(puddleUid);
                entityManager.EntitySysManager.GetEntitySystem<StepTriggerSystem>().SetRequiredTriggerSpeed(puddleUid, RequiredSlipSpeed, step);

                var slow = entityManager.EnsureComponent<SpeedModifierContactsComponent>(puddleUid);
                var speedModifier = 1 - reagent.Viscosity;
                entityManager.EntitySysManager.GetEntitySystem<SpeedModifierContactsSystem>().ChangeModifiers(puddleUid, speedModifier, slow);

                return reactVolume;
            }

            return FixedPoint2.Zero;
        }
    }
}
