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
        [DataField("launchForwardsMultiplier")] private float _launchForwardsMultiplier = 1;
        [DataField("requiredSlipSpeed")] private float _requiredSlipSpeed = 6;
        [DataField("paralyzeTime")] private float _paralyzeTime = 1;

        /// <summary>
        /// <see cref="SlipperyComponent.SuperSlippery"/>
        /// </summary>
        [DataField("superSlippery")] private bool _superSlippery;

        public FixedPoint2 TileReact(TileRef tile, ReagentPrototype reagent, FixedPoint2 reactVolume)
        {
            if (reactVolume < 5)
                return FixedPoint2.Zero;

            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (entityManager.EntitySysManager.GetEntitySystem<PuddleSystem>()
                .TrySpillAt(tile, new Solution(reagent.ID, reactVolume), out var puddleUid, false, false))
            {
                var slippery = entityManager.EnsureComponent<SlipperyComponent>(puddleUid);
                slippery.LaunchForwardsMultiplier = _launchForwardsMultiplier;
                slippery.ParalyzeTime = _paralyzeTime;
                slippery.SuperSlippery = _superSlippery;
                entityManager.Dirty(puddleUid, slippery);

                var step = entityManager.EnsureComponent<StepTriggerComponent>(puddleUid);
                entityManager.EntitySysManager.GetEntitySystem<StepTriggerSystem>().SetRequiredTriggerSpeed(puddleUid, _requiredSlipSpeed, step);

                var slow = entityManager.EnsureComponent<SlowContactsComponent>(puddleUid);
                var speedModifier = 1 - reagent.Viscosity;
                entityManager.EntitySysManager.GetEntitySystem<SlowContactsSystem>().ChangeModifiers(puddleUid, speedModifier, slow);

                return reactVolume;
            }

            return FixedPoint2.Zero;
        }
    }
}
